using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AI_Task_Planner.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }    
    public async Task<IActionResult> Index()
    {        // Get the current user and their roles
        var user = await _userManager.GetUserAsync(User);
        var userRole = "User"; // Default role
        
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRole = roles.FirstOrDefault() ?? "User";
        }
        
        // Set user role for the view
        ViewBag.UserRole = userRole;
        
        // Base query for tasks
        var tasksQuery = _context.Tasks.AsQueryable();
        
        // Filter tasks based on user role
        if (userRole == "Manager")
        {
            // Manager can see all tasks (no additional filtering needed)
        }
        else if (userRole == "Lead")
        {
            // Lead can see all user tasks and their own tasks
            if (user != null)
            {
                // Get user role IDs
                var userRoleId = await _context.Roles.Where(r => r.Name == "User").Select(r => r.Id).FirstOrDefaultAsync();
                
                // Get all users with regular user role
                var userIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == userRoleId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();
                    
                // Add lead's own ID to the list
                userIds.Add(user.Id);
                  
                // Filter tasks assigned to any regular user or to the lead
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId != null && 
                    (userIds.Contains(t.AssignedToUserId) || t.AssignedToUserId == user.Id));
            }
        }
        else
        {
            // Regular users can only see tasks assigned to them
            if (user != null)
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == user.Id);
            }
        }
        
        // Get statistics based on the role-filtered tasks
        ViewBag.TotalTasks = await tasksQuery.CountAsync();
        ViewBag.CompletedTasks = await tasksQuery.CountAsync(t => t.IsCompleted);
        ViewBag.CompletionRate = ViewBag.TotalTasks > 0 
            ? Math.Round((double)ViewBag.CompletedTasks / ViewBag.TotalTasks * 100, 1)
            : 0;
        ViewBag.DueTodayCount = await tasksQuery.CountAsync(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.Today && !t.IsCompleted);
        ViewBag.OverdueCount = await tasksQuery.CountAsync(t => t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.Today && !t.IsCompleted);
        
        // Add task category breakdown for managers
        if (userRole == "Manager")
        {
            ViewBag.CategoryBreakdown = await tasksQuery
                .GroupBy(t => t.Category != null ? t.Category.Name : "Uncategorized")
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
        }
          // Get upcoming/due soon tasks - filtered according to the user's role
        if (userRole == "Manager")
        {
            // For managers, show all tasks including completed ones, with most recent at top
            ViewBag.UpcomingTasks = await tasksQuery
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .OrderByDescending(t => t.IsCompleted) // Completed tasks after incomplete ones
                .ThenBy(t => t.DueDate)
                .Take(10) // Show more tasks for managers
                .ToListAsync();
        }
        else
        {
            // For leads and regular users, only show incomplete tasks
            ViewBag.UpcomingTasks = await tasksQuery
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Where(t => !t.IsCompleted)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToListAsync();
        }
            
        return View();
    }
    
    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
