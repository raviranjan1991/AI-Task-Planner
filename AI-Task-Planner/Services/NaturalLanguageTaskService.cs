using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AI_Task_Planner.Models;
using AI_Task_Planner.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AI_Task_Planner.Services
{
    public class NaturalLanguageTaskService
    {        
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Chronic.Core.Parser _dateParser;

        public NaturalLanguageTaskService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _dateParser = new Chronic.Core.Parser();
        }

        public async Task<TaskParsingResult> ParseNaturalLanguageTaskAsync(string naturalLanguageInput, string userId)
        {
            // Initialize the result
            var result = new TaskParsingResult 
            { 
                Success = true,
                Task = new UserTask
                {
                    CreatedOn = DateTime.Now,
                    IsCompleted = false,
                    AssignedToUserId = userId,
                    AssignedByUserId = userId,
                    AssignedOn = DateTime.Now,
                    Priority = 2 // Default to Medium priority
                }
            };

            try
            {
                // Extract title (main task description)
                result.Task.Title = ExtractTitle(naturalLanguageInput);

                // Extract due date
                result.Task.DueDate = ExtractDueDate(naturalLanguageInput);

                // Extract priority
                result.Task.Priority = ExtractPriority(naturalLanguageInput);

                // Extract category
                result.Task.CategoryId = await ExtractCategoryIdAsync(naturalLanguageInput);

                // Extract assignee (if applicable)
                if (await TryExtractAssigneeAsync(naturalLanguageInput, userId, result))
                {
                    // Assignment was handled by the method
                }

                // Fill description with remaining details
                result.Task.Description = ExtractDescription(naturalLanguageInput, result.Task);

                return result;
            }
            catch (Exception ex)
            {
                return new TaskParsingResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to parse task: {ex.Message}",
                    Task = result.Task // Return what we have so far
                };
            }
        }

        private string ExtractTitle(string input)
        {
            // Basic implementation: Take the first sentence or part before specific markers
            var markers = new[] { " by ", " on ", " at ", " due ", " for ", " with priority " };
            var firstSentenceEnd = input.IndexOf('.');
            
            // Find the first marker position
            var firstMarkerPos = markers
                .Select(m => input.IndexOf(m, StringComparison.OrdinalIgnoreCase))
                .Where(pos => pos > 0)
                .DefaultIfEmpty(input.Length)
                .Min();
            
            // Take the shorter of first sentence or before first marker
            var endPos = (firstSentenceEnd > 0 && firstSentenceEnd < firstMarkerPos) 
                ? firstSentenceEnd 
                : firstMarkerPos;
                
            return input.Substring(0, endPos).Trim();
        }

        private DateTime? ExtractDueDate(string input)
        {
            try
            {
                // Look for due date patterns
                var dueDatePatterns = new[]
                {
                    @"(?:due|by|on)\s+(.*?)(?:\s+at\s+|$)",
                    @"for\s+(.*?)(?:\s+at\s+|$)"
                };

                foreach (var pattern in dueDatePatterns)
                {
                    var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dateText = match.Groups[1].Value;
                        var span = _dateParser.Parse(dateText);
                        if (span != null)
                        {
                            return span.Start;
                        }
                    }
                }

                // If no patterns matched, try parsing the entire input
                var span2 = _dateParser.Parse(input);
                if (span2 != null)
                {
                    return span2.Start;
                }

                // If no date was found, return null
                return null;
            }
            catch
            {
                return null; // If date parsing fails, return null
            }
        }

        private int ExtractPriority(string input)
        {
            // Check for explicit priority mentions
            if (Regex.IsMatch(input, @"(?:high|urgent|critical|important)\s+priority", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(input, @"priority\s*(?::|is|=)?\s*(?:high|urgent|critical|important)", RegexOptions.IgnoreCase))
            {
                return 1; // High priority
            }
            else if (Regex.IsMatch(input, @"(?:low|minor)\s+priority", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(input, @"priority\s*(?::|is|=)?\s*(?:low|minor)", RegexOptions.IgnoreCase))
            {
                return 3; // Low priority
            }
            
            // Check for urgency indicators
            if (Regex.IsMatch(input, @"\b(?:urgent|asap|immediately|right away|critical)\b", RegexOptions.IgnoreCase))
            {
                return 1; // High priority
            }
            else if (Regex.IsMatch(input, @"\b(?:when you have time|no rush|can wait|eventually)\b", RegexOptions.IgnoreCase))
            {
                return 3; // Low priority
            }
            
            return 2; // Default to medium priority
        }

        private async Task<int?> ExtractCategoryIdAsync(string input)
        {
            // Get all categories from the database
            var categories = await _context.TaskCategories.ToListAsync();
            
            // Check if any category name appears in the input
            foreach (var category in categories)
            {
                var categoryPattern = $@"\b{Regex.Escape(category.Name)}\b";
                if (Regex.IsMatch(input, categoryPattern, RegexOptions.IgnoreCase))
                {
                    return category.CategoryId;
                }
            }
            
            // Also check for common category indicators
            var categoryIndicators = new Dictionary<string, string>
            {
                { @"\b(?:meet|meeting|conference|call|discussion)\b", "Meetings" },
                { @"\b(?:dev|develop|code|program|implement|build)\b", "Development" },
                { @"\b(?:design|mockup|prototype|wireframe|UI|UX)\b", "Design" },
                { @"\b(?:test|QA|verify|validate|bug|fix)\b", "Testing" },
                { @"\b(?:doc|documentation|write|report)\b", "Documentation" }
            };
            
            foreach (var indicator in categoryIndicators)
            {
                if (Regex.IsMatch(input, indicator.Key, RegexOptions.IgnoreCase))
                {
                    // Look for a category that matches or contains the indicator value
                    var matchingCategory = categories.FirstOrDefault(c => 
                        c.Name.Equals(indicator.Value, StringComparison.OrdinalIgnoreCase) || 
                        c.Name.Contains(indicator.Value, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingCategory != null)
                    {
                        return matchingCategory.CategoryId;
                    }
                }
            }
            
            // If no category is found, return null (will use default or ask user)
            return null;
        }

        private async Task<bool> TryExtractAssigneeAsync(string input, string currentUserId, TaskParsingResult result)
        {
            // Check for assignment patterns
            var assignPatterns = new[]
            {
                @"(?:assign|delegate|give)\s+to\s+(.+?)(?:\s+by|\s+on|\s+due|\.|$)",
                @"for\s+(.+?)(?:\s+to\s+|$)"
            };

            foreach (var pattern in assignPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string assigneeName = match.Groups[1].Value.Trim();
                    
                    // Get all users from the database
                    var users = await _userManager.Users.ToListAsync();
                    
                    // Try to find by first name, last name, or full name
                    var assignee = users.FirstOrDefault(u => 
                        u.FirstName.Equals(assigneeName, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.Equals(assigneeName, StringComparison.OrdinalIgnoreCase) ||
                        $"{u.FirstName} {u.LastName}".Equals(assigneeName, StringComparison.OrdinalIgnoreCase));
                    
                    if (assignee != null)
                    {                        // Check if the current user can assign to this user
                        var currentUser = await _userManager.FindByIdAsync(currentUserId);
                        if (currentUser == null)
                        {
                            return false;
                        }
                        var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
                        var currentUserRole = currentUserRoles.FirstOrDefault() ?? "User";
                        
                        var assigneeRoles = await _userManager.GetRolesAsync(assignee);
                        var assigneeRole = assigneeRoles.FirstOrDefault() ?? "User";
                        
                        bool canAssign = false;
                        
                        if (currentUserRole == "Manager")
                        {
                            // Managers can assign to anyone
                            canAssign = true;
                        }
                        else if (currentUserRole == "Lead")
                        {
                            // Leads can only assign to regular users or themselves
                            canAssign = assigneeRole == "User" || assignee.Id == currentUserId;
                        }
                        else
                        {
                            // Regular users can only assign to themselves
                            canAssign = assignee.Id == currentUserId;
                        }
                        
                        if (canAssign)
                        {
                            result.Task.AssignedToUserId = assignee.Id;
                            return true;
                        }
                        else
                        {
                            result.Success = false;
                            result.ErrorMessage = $"You don't have permission to assign tasks to {assigneeName}.";
                            return true;
                        }
                    }
                }
            }
            
            // If no assignee was found or no assignment patterns matched, return false
            return false;
        }

        private string ExtractDescription(string input, UserTask task)
        {
            // Extract additional details that weren't used for other fields
            var description = input;
            
            // Remove title if it's at the beginning of the input
            if (description.StartsWith(task.Title))
            {
                description = description.Substring(task.Title.Length).Trim();
            }
            
            // Clean up the description by removing detected due date markers
            if (task.DueDate.HasValue)
            {
                var dueDatePatterns = new[]
                {
                    @"(?:due|by|on)\s+.*?(?:\s+at\s+|\s+\d{1,2}:\d{2}|\s+\d{1,2}\s*(?:am|pm)|$)",
                    @"for\s+.*?(?:\s+at\s+|\s+\d{1,2}:\d{2}|\s+\d{1,2}\s*(?:am|pm)|$)"
                };
                
                foreach (var pattern in dueDatePatterns)
                {
                    description = Regex.Replace(description, pattern, " ", RegexOptions.IgnoreCase);
                }
            }
            
            // Clean up the description by removing detected priority markers
            var priorityPatterns = new[]
            {
                @"(?:high|urgent|critical|important|low|minor|medium)\s+priority",
                @"priority\s*(?::|is|=)?\s*(?:high|urgent|critical|important|low|minor|medium)",
                @"\b(?:urgent|asap|immediately|right away|critical|when you have time|no rush|can wait|eventually)\b"
            };
            
            foreach (var pattern in priorityPatterns)
            {
                description = Regex.Replace(description, pattern, " ", RegexOptions.IgnoreCase);
            }
              // Clean up extra spaces and return the description
            description = Regex.Replace(description, @"\s+", " ").Trim();
            
            return string.IsNullOrWhiteSpace(description) ? string.Empty : description;
        }
    }    public class TaskParsingResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public UserTask Task { get; set; } = new UserTask();
    }
}
