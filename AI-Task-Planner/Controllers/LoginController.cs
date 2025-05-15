using System;
using System.Threading.Tasks;
using System.Linq;
using AI_Task_Planner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AI_Task_Planner.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        
        // GET: /Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        
        // POST: /Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await _signInManager.SignOutAsync();
            
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    // Get the user to determine their role
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        // Update last active timestamp
                        user.LastActive = DateTime.Now;
                        await _userManager.UpdateAsync(user);
                        
                        // Add user role claim to the cookie
                        var roles = await _userManager.GetRolesAsync(user);
                        ViewBag.UserRole = roles.FirstOrDefault() ?? "User"; // Default to "User" if no role assigned
                    }
                    
                    // If returnUrl is null or empty, redirect to home page
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToAction(nameof(HomeController.Index), "Home");
                    }
                    return RedirectToLocal(returnUrl);
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            return View(model);
        }
        
        // POST: /Login/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Index), "Login");
        }

        // GET: /Login/AccessDenied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}
