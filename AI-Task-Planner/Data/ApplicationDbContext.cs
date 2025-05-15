using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AI_Task_Planner.Models;

namespace AI_Task_Planner.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }          public DbSet<UserTask> Tasks { get; set; }
        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TaskTimeLog> TaskTimeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure UserTask entity
            modelBuilder.Entity<UserTask>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Tasks)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Seed some categories
            modelBuilder.Entity<TaskCategory>().HasData(
                new TaskCategory { CategoryId = 1, Name = "Work", Description = "Work-related tasks" },
                new TaskCategory { CategoryId = 2, Name = "Personal", Description = "Personal tasks" },
                new TaskCategory { CategoryId = 3, Name = "Shopping", Description = "Shopping lists" },
                new TaskCategory { CategoryId = 4, Name = "Health", Description = "Health and fitness activities" }
            );
        }
    }
}
