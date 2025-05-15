using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using Microsoft.AspNetCore.Authorization;

namespace AI_Task_Planner.Controllers
{
    [Authorize(Roles = "Manager,Lead")]
    public class TaskCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TaskCategories
        public async Task<IActionResult> Index()
        {
            return View(await _context.TaskCategories.ToListAsync());
        }        // GET: TaskCategories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskCategory = await _context.TaskCategories
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (taskCategory == null)
            {
                return NotFound();
            }

            return View(taskCategory);
        }

        // GET: TaskCategories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TaskCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,Name,Description")] TaskCategory taskCategory)
        {
            if (ModelState.IsValid)
            {
                _context.Add(taskCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taskCategory);
        }

        // GET: TaskCategories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskCategory = await _context.TaskCategories.FindAsync(id);
            if (taskCategory == null)
            {
                return NotFound();
            }
            return View(taskCategory);
        }

        // POST: TaskCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Description")] TaskCategory taskCategory)
        {
            if (id != taskCategory.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(taskCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskCategoryExists(taskCategory.CategoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(taskCategory);
        }

        // GET: TaskCategories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskCategory = await _context.TaskCategories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
            if (taskCategory == null)
            {
                return NotFound();
            }

            // Check if there are any tasks using this category
            var tasksUsingCategory = await _context.Tasks
                .AnyAsync(t => t.CategoryId == id);
            
            ViewBag.HasTasks = tasksUsingCategory;

            return View(taskCategory);
        }

        // POST: TaskCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taskCategory = await _context.TaskCategories.FindAsync(id);
            
            // Check if this category has associated tasks
            var tasksCount = await _context.Tasks.CountAsync(t => t.CategoryId == id);
            
            if (tasksCount > 0)
            {
                ModelState.AddModelError(string.Empty, 
                    $"Cannot delete this category because it is used by {tasksCount} task(s). " +
                    "First reassign or delete these tasks.");
                
                return View(taskCategory);
            }

            if (taskCategory != null)
            {
                _context.TaskCategories.Remove(taskCategory);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool TaskCategoryExists(int id)
        {
            return _context.TaskCategories.Any(e => e.CategoryId == id);
        }
    }
}
