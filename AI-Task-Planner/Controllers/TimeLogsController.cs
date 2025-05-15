using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace AI_Task_Planner.Controllers
{
    [Authorize]
    public class TimeLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimeLogsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TimeLogs/ForTask/5
        public async Task<IActionResult> ForTask(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.TimeLogs)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(m => m.TaskId == id);

            if (task == null)
            {
                return NotFound();
            }

            ViewBag.TaskId = id;
            ViewBag.TaskTitle = task.Title;
            
            return View(task.TimeLogs.OrderByDescending(l => l.StartTime).ToList());
        }

        // GET: TimeLogs/Create/5
        public IActionResult Create(int? taskId)
        {
            if (taskId == null)
            {
                return NotFound();
            }

            ViewBag.TaskId = taskId;
            
            var timeLog = new TaskTimeLog
            {
                TaskId = taskId.Value,
                StartTime = DateTime.Now
            };
            
            return View(timeLog);
        }

        // POST: TimeLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TaskId,StartTime,EndTime,Description")] TaskTimeLog timeLog)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    timeLog.UserId = user.Id;
                    
                    _context.Add(timeLog);
                    await _context.SaveChangesAsync();
                    
                    return RedirectToAction("ForTask", new { id = timeLog.TaskId });
                }
                else
                {
                    return Forbid();
                }
            }
            
            ViewBag.TaskId = timeLog.TaskId;
            return View(timeLog);
        }

        // GET: TimeLogs/StartTimer/5
        public async Task<IActionResult> StartTimer(int? taskId)
        {
            if (taskId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            
            // Check if there's already an active timer
            var activeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(l => l.TaskId == taskId && l.UserId == user.Id && l.EndTime == null);
                
            if (activeLog != null)
            {
                // Timer already running
                return RedirectToAction("ForTask", new { id = taskId });
            }
            
            var timeLog = new TaskTimeLog
            {
                TaskId = taskId.Value,
                UserId = user.Id,
                StartTime = DateTime.Now
            };
            
            _context.Add(timeLog);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ForTask", new { id = taskId });
        }        // GET: TimeLogs/StopTimer/5
        public async Task<IActionResult> StopTimer(int? taskId, string? description = null)
        {
            if (taskId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            
            // Find the active timer
            var activeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(l => l.TaskId == taskId && l.UserId == user.Id && l.EndTime == null);
                
            if (activeLog == null)
            {
                // No active timer found
                return RedirectToAction("ForTask", new { id = taskId });
            }
            
            // If the timer was paused, calculate total paused time
            if (activeLog.IsPaused && activeLog.PauseTime.HasValue)
            {
                // No need to add more paused time since it's already being tracked
                activeLog.IsPaused = false;
                activeLog.PauseTime = null;
            }
            
            // Stop the timer
            activeLog.EndTime = DateTime.Now;
            
            // Add description if provided
            if (!string.IsNullOrEmpty(description))
            {
                activeLog.Description = description;
            }
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ForTask", new { id = taskId });
        }
        
        // GET: TimeLogs/PauseTimer/5
        public async Task<IActionResult> PauseTimer(int? taskId)
        {
            if (taskId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            
            // Find the active timer
            var activeLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(l => l.TaskId == taskId && l.UserId == user.Id && l.EndTime == null && !l.IsPaused);
                
            if (activeLog == null)
            {
                // No active timer found
                return RedirectToAction("ForTask", new { id = taskId });
            }
            
            // Pause the timer
            activeLog.IsPaused = true;
            activeLog.PauseTime = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ForTask", new { id = taskId });
        }
        
        // GET: TimeLogs/ResumeTimer/5
        public async Task<IActionResult> ResumeTimer(int? taskId)
        {
            if (taskId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            
            // Find the paused timer
            var pausedLog = await _context.TaskTimeLogs
                .FirstOrDefaultAsync(l => l.TaskId == taskId && l.UserId == user.Id && l.EndTime == null && l.IsPaused);
                
            if (pausedLog == null)
            {
                // No paused timer found
                return RedirectToAction("ForTask", new { id = taskId });
            }
            
            // Calculate paused time and add to total
            if (pausedLog.PauseTime.HasValue)
            {
                int pausedMinutes = (int)(DateTime.Now - pausedLog.PauseTime.Value).TotalMinutes;
                pausedLog.TotalPausedMinutes += pausedMinutes;
            }
            
            // Resume the timer
            pausedLog.IsPaused = false;
            pausedLog.PauseTime = null;
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ForTask", new { id = taskId });
        }

        // GET: TimeLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeLog = await _context.TaskTimeLogs.FindAsync(id);
            
            if (timeLog == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            
            // Only allow editing if the user is the owner of the log or is a manager
            if (timeLog.UserId != user.Id && !roles.Contains("Manager"))
            {
                return Forbid();
            }
            
            ViewBag.TaskId = timeLog.TaskId;
            return View(timeLog);
        }

        // POST: TimeLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LogId,TaskId,StartTime,EndTime,Description,IsPaused,TotalPausedMinutes")] TaskTimeLog timeLog)
        {
            if (id != timeLog.LogId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalLog = await _context.TaskTimeLogs.AsNoTracking().FirstOrDefaultAsync(l => l.LogId == id);
                    if (originalLog == null)
                    {
                        return NotFound();
                    }
                      // Preserve user ID and paused time information
                    timeLog.UserId = originalLog.UserId;
                    timeLog.IsPaused = originalLog.IsPaused;
                    timeLog.PauseTime = originalLog.PauseTime;
                    timeLog.TotalPausedMinutes = originalLog.TotalPausedMinutes;
                    
                    _context.Update(timeLog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeLogExists(timeLog.LogId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                
                return RedirectToAction("ForTask", new { id = timeLog.TaskId });
            }
            
            ViewBag.TaskId = timeLog.TaskId;
            return View(timeLog);
        }

        // GET: TimeLogs/Delete/5
        [Authorize(Roles = "Manager,Lead")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeLog = await _context.TaskTimeLogs
                .Include(l => l.Task)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.LogId == id);
                
            if (timeLog == null)
            {
                return NotFound();
            }

            return View(timeLog);
        }

        // POST: TimeLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Lead")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeLog = await _context.TaskTimeLogs.FindAsync(id);
            
            if (timeLog == null)
            {
                return NotFound();
            }
            
            int taskId = timeLog.TaskId;
            
            _context.TaskTimeLogs.Remove(timeLog);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("ForTask", new { id = taskId });
        }

        // GET: TimeLogs/Report
        [Authorize(Roles = "Manager,Lead")]
        public async Task<IActionResult> Report(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today.AddDays(1);
            
            var logs = await _context.TaskTimeLogs
                .Include(l => l.Task)
                .Include(l => l.User)
                .Where(l => l.StartTime >= fromDate && l.StartTime < toDate)
                .OrderByDescending(l => l.StartTime)
                .ToListAsync();
                
            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            
            return View(logs);
        }

        // GET: TimeLogs/AddDescription/5
        public async Task<IActionResult> AddDescription(int id)
        {
            var timeLog = await _context.TaskTimeLogs
                .Include(l => l.Task)
                .FirstOrDefaultAsync(l => l.LogId == id);
                
            if (timeLog == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null || timeLog.UserId != user.Id)
            {
                return Forbid();
            }
            
            // Only allow adding description to recently stopped timers
            if (timeLog.EndTime == null || (DateTime.Now - timeLog.EndTime.Value).TotalMinutes > 30)
            {
                return RedirectToAction("ForTask", new { id = timeLog.TaskId });
            }
            
            ViewBag.TaskId = timeLog.TaskId;
            ViewBag.TaskTitle = timeLog.Task.Title;
            ViewBag.Duration = timeLog.DurationMinutes;
            
            return View(timeLog);
        }
        
        // POST: TimeLogs/AddDescription/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDescription(int id, [Bind("LogId,Description")] TaskTimeLog timeLog)
        {
            var existingLog = await _context.TaskTimeLogs.FindAsync(id);
            if (existingLog == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null || existingLog.UserId != user.Id)
            {
                return Forbid();
            }
            
            if (ModelState.IsValid)
            {
                existingLog.Description = timeLog.Description;
                await _context.SaveChangesAsync();
                
                return RedirectToAction("ForTask", new { id = existingLog.TaskId });
            }
            
            ViewBag.TaskId = existingLog.TaskId;
            return View(existingLog);
        }

        private bool TimeLogExists(int id)
        {
            return _context.TaskTimeLogs.Any(e => e.LogId == id);
        }
    }
}
