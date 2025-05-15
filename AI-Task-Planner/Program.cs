using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AI_Task_Planner.Data;
using AI_Task_Planner.Models;
using AI_Task_Planner.Services;
using System;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register application services
builder.Services.AddScoped<NaturalLanguageTaskService>();

// Configure authentication cookie settings
builder.Services.ConfigureApplicationCookie(options => 
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Login/Logout";
    options.AccessDeniedPath = "/Login/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Cookie expires after 1 hour
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure Identity endpoints
app.MapRazorPages(); // Required for Identity pages

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Seed roles and users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        SeedData(userManager, roleManager).Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles and users.");
    }
}

app.Run();

// Define the seed data method
static async Task SeedData(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
{
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

static async Task CreateUserIfNotExists(UserManager<ApplicationUser> userManager, ApplicationUser user, string password, string role)
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
