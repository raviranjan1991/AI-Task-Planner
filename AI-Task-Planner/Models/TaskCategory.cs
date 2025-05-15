using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AI_Task_Planner.Models
{
    public class TaskCategory
    {
        public TaskCategory()
        {
            Tasks = new List<UserTask>();
        }

        [Key]
        public int CategoryId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        // Navigation property
        public virtual ICollection<UserTask> Tasks { get; set; }
    }
}