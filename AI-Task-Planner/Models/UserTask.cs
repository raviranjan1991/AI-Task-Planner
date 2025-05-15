using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Task_Planner.Models
{
    public class UserTask
    {
        public UserTask()
        {
            TimeLogs = new HashSet<TaskTimeLog>();
        }
        
        [Key]
        public int TaskId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Task Title")]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }
        
        [Display(Name = "Priority")]
        public int Priority { get; set; } // 1-High, 2-Medium, 3-Low
        
        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; }
        
        [Display(Name = "Created On")]
        public DateTime CreatedOn { get; set; }
        
        [Display(Name = "Modified On")]
        public DateTime? ModifiedOn { get; set; }
          // Foreign Key
        public int? CategoryId { get; set; }
        
        // Navigation property
        public virtual TaskCategory? Category { get; set; }
          // Time tracking
        [Display(Name = "Total Time Spent (minutes)")]
        [NotMapped]
        public int TotalTimeSpent { get; set; }
        
        // Navigation collection for time logs
        public virtual ICollection<TaskTimeLog> TimeLogs { get; set; }
        
        // Task assignment properties
        [Display(Name = "Assigned To")]
        public string? AssignedToUserId { get; set; }
        
        [Display(Name = "Assigned By")]
        public string? AssignedByUserId { get; set; }
        
        [Display(Name = "Assigned On")]
        public DateTime? AssignedOn { get; set; }
        
        // Navigation properties for task assignment
        [ForeignKey("AssignedToUserId")]
        public virtual ApplicationUser? AssignedToUser { get; set; }
        
        [ForeignKey("AssignedByUserId")]
        public virtual ApplicationUser? AssignedByUser { get; set; }
    }
}
