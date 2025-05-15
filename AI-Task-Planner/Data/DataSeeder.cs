using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AI_Task_Planner.Models;
using System;
using System.Threading.Tasks;

namespace AI_Task_Planner.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                
                // Define roles
                string[] roleNames = { "Manager", "Lead", "User" };
                
                foreach (var roleName in roleNames)
                {
                    // Check if the role already exists
                    var roleExists = await roleManager.RoleExistsAsync(roleName);
                    if (!roleExists)
                    {
                        // Create the role
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Create Manager user
                var managerUser = new ApplicationUser
                {
                    UserName = "manager@taskplanner.com",
                    Email = "manager@taskplanner.com",
                    FirstName = "Admin",
                    LastName = "Manager",
                    EmailConfirmed = true,
                    Created = DateTime.Now
                };

                await CreateUserIfNotExists(userManager, managerUser, "Manager123!", "Manager");

                // Create Lead user
                var leadUser = new ApplicationUser
                {
                    UserName = "lead@taskplanner.com",
                    Email = "lead@taskplanner.com",
                    FirstName = "Team",
                    LastName = "Lead",
                    EmailConfirmed = true,
                    Created = DateTime.Now
                };

                await CreateUserIfNotExists(userManager, leadUser, "Lead123!", "Lead");

                // Create Normal user
                var normalUser = new ApplicationUser
                {
                    UserName = "user@taskplanner.com",
                    Email = "user@taskplanner.com",
                    FirstName = "Normal",
                    LastName = "User",
                    EmailConfirmed = true,
                    Created = DateTime.Now
                };

                await CreateUserIfNotExists(userManager, normalUser, "User123!", "User");
            }
        }

        private static async Task CreateUserIfNotExists(UserManager<ApplicationUser> userManager, ApplicationUser user, string password, string role)
        {
            var existingUser = await userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}
