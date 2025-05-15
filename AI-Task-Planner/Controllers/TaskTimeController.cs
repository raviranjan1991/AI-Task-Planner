using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using AI_Task_Planner.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace AI_Task_Planner.Controllers
{
    [Authorize]
    public class TaskTimeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TaskTimeController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: TaskTime/StartTimer/5
        public async Task<IActionResult> StartTimer(int? taskId, string returnUrl = null)
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
                .FirstOrDefaultAsync(l => l.UserId == user.Id && l.EndTime == null);
                
            if (activeLog != null)
            {
                // Stop existing timer before starting a new one
                if (activeLog.IsPaused)
                {
                    // Calculate total paused time
                    var pausedDuration = DateTime.Now - activeLog.PauseTime.Value;
                    activeLog.TotalPausedMinutes += (int)pausedDuration.TotalMinutes;
                }
                
                activeLog.EndTime = DateTime.Now;
                _context.Update(activeLog);
            }
            
            // Create new timer
            var timeLog = new TaskTimeLog
            {
                TaskId = taskId.Value,
                UserId = user.Id,
                StartTime = DateTime.Now
            };
            
            _context.Add(timeLog);
            await _context.SaveChangesAsync();
            
            // Redirect back to the referring page, or to the task details
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        // GET: TaskTime/StopTimer/5
        public async Task<IActionResult> StopTimer(int? taskId, string returnUrl = null)
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
                TempData["ErrorMessage"] = "No active timer found to stop.";
            }
            else
            {
                // If timer was paused, calculate total paused time
                if (activeLog.IsPaused)
                {
                    var pausedDuration = DateTime.Now - activeLog.PauseTime.Value;
                    activeLog.TotalPausedMinutes += (int)pausedDuration.TotalMinutes;
                    activeLog.IsPaused = false;
                }
                
                // Stop the timer
                activeLog.EndTime = DateTime.Now;
                _context.Update(activeLog);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Timer stopped successfully.";
            }
            
            // Redirect back to the referring page, or to the task details
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        // GET: TaskTime/PauseTimer/5
        public async Task<IActionResult> PauseTimer(int? taskId, string returnUrl = null)
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
                TempData["ErrorMessage"] = "No active timer found to pause.";
            }
            else
            {
                // Pause the timer
                activeLog.IsPaused = true;
                activeLog.PauseTime = DateTime.Now;
                
                _context.Update(activeLog);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Timer paused successfully.";
            }
            
            // Redirect back to the referring page, or to the task details
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        // GET: TaskTime/ResumeTimer/5
        public async Task<IActionResult> ResumeTimer(int? taskId, string returnUrl = null)
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
                TempData["ErrorMessage"] = "No paused timer found to resume.";
            }
            else
            {
                // Calculate paused time
                var pausedDuration = DateTime.Now - pausedLog.PauseTime.Value;
                pausedLog.TotalPausedMinutes += (int)pausedDuration.TotalMinutes;
                
                // Resume the timer
                pausedLog.IsPaused = false;
                pausedLog.PauseTime = null;
                
                _context.Update(pausedLog);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Timer resumed successfully.";
            }
            
            // Redirect back to the referring page, or to the task details
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Details", "Tasks", new { id = taskId });
        }

        // GET: TaskTime/ForTask/5
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
            
            // Get active time logs
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.ActiveTimeLogs = await _context.TaskTimeLogs
                    .Where(l => l.UserId == user.Id && l.EndTime == null)
                    .Include(l => l.Task)
                    .ToListAsync();
            }
            
            return View("~/Views/TimeLogs/ForTask.cshtml", task.TimeLogs.OrderByDescending(l => l.StartTime).ToList());
        }

        // Helper method to get active timers for the current user
        [NonAction]
        public async Task<List<TaskTimeLog>> GetActiveTimers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new List<TaskTimeLog>();
            }
            
            return await _context.TaskTimeLogs
                .Include(l => l.Task)
                .Where(l => l.UserId == user.Id && l.EndTime == null)
                .ToListAsync();
        }
    }
}
