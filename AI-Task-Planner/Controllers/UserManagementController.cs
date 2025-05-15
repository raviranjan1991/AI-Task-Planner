using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace AI_Task_Planner.Controllers
{
    [Authorize(Roles = "Manager")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        
        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        
        // GET: UserManagement
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? "None",
                    Created = user.Created,
                    LastActive = user.LastActive
                });
            }
            
            return View(userViewModels);
        }
        
        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToListAsync();
            return View();
        }
        
        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    Created = DateTime.Now
                };
                
                var result = await _userManager.CreateAsync(user, model.Password);
                
                if (result.Succeeded)
                {
                    // Assign role if selected
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            ViewBag.Roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToListAsync();
            return View(model);
        }
        
        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            
            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "None"
            };
            
            ViewBag.Roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToListAsync();
            return View(model);
        }
        
        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                
                user.Email = model.Email;
                user.UserName = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                
                var result = await _userManager.UpdateAsync(user);
                
                if (result.Succeeded)
                {
                    // Handle role changes
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    
                    // Remove existing roles
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }
                    
                    // Add new role if selected
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            ViewBag.Roles = await _roleManager.Roles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToListAsync();
            return View(model);
        }
        
        // GET: UserManagement/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            
            var viewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "None",
                Created = user.Created,
                LastActive = user.LastActive
            };
            
            return View(viewModel);
        }        // POST: UserManagement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow deletion of your own account
            if (User.Identity != null && User.Identity.Name == user.Email)
            {
                ModelState.AddModelError(string.Empty, "You cannot delete your own account.");
                
                var roles = await _userManager.GetRolesAsync(user);
                
                var viewModel = new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = roles.FirstOrDefault() ?? "None",
                    Created = user.Created,
                    LastActive = user.LastActive
                };
                
                return View(viewModel);
            }
            
            try
            {
                // Get a reference to the DbContext
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                
                // Find all tasks assigned to this user
                var assignedToUserTasks = await context.Tasks
                    .Where(t => t.AssignedToUserId == id)
                    .ToListAsync();
                
                // Find all tasks assigned by this user
                var assignedByUserTasks = await context.Tasks
                    .Where(t => t.AssignedByUserId == id)
                    .ToListAsync();
                
                // Update tasks assigned to this user (set AssignedToUserId to null)
                foreach (var task in assignedToUserTasks)
                {
                    task.AssignedToUserId = null;
                }
                
                // Update tasks assigned by this user (set AssignedByUserId to null)
                foreach (var task in assignedByUserTasks)
                {
                    task.AssignedByUserId = null;
                }
                
                // Save changes to the database
                await context.SaveChangesAsync();
                
                // Check for time logs associated with this user and delete them
                var timeLogs = await context.TaskTimeLogs
                    .Where(tl => tl.UserId == id)
                    .ToListAsync();
                
                if (timeLogs.Any())
                {
                    context.TaskTimeLogs.RemoveRange(timeLogs);
                    await context.SaveChangesAsync();
                }
                
                // Now delete the user
                var result = await _userManager.DeleteAsync(user);
                
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error deleting user: {ex.Message}");
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
