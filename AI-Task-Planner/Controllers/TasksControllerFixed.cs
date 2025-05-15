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
    public class TasksControllerFixed : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NaturalLanguageTaskService _nlpService;

        public TasksControllerFixed(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            NaturalLanguageTaskService nlpService)
        {
            _context = context;
            _userManager = userManager;
            _nlpService = nlpService;
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Category)
                .Include(t => t.AssignedToUser)
                .Include(t => t.AssignedByUser)
                .FirstOrDefaultAsync(t => t.TaskId == id);
                
            if (task == null)
            {
                return NotFound();
            }
            
            // Get categories for dropdown
            ViewBag.Categories = new SelectList(_context.TaskCategories.OrderBy(c => c.Name), "CategoryId", "Name", task.CategoryId);
            
            // Get priorities for dropdown
            ViewBag.Priorities = new SelectList(new[]
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" },
            }, "Value", "Text", task.Priority);
            
            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TaskId,Title,Description,DueDate,Priority,CategoryId,AssignedToUserId,IsCompleted,CreatedOn,AssignedByUserId,AssignedOn")] UserTask task)
        {
            if (id != task.TaskId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            // If validation failed, redisplay the form
            ViewBag.Categories = new SelectList(_context.TaskCategories.OrderBy(c => c.Name), "CategoryId", "Name", task.CategoryId);
            ViewBag.Priorities = new SelectList(new[]
            {
                new { Value = 1, Text = "High" },
                new { Value = 2, Text = "Medium" },
                new { Value = 3, Text = "Low" },
            }, "Value", "Text", task.Priority);
            
            return View(task);
        }
        
        // Helper method to check if a task exists
        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.TaskId == id);
        }

        // Index method for linking
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Tasks");
        }
    }
}
