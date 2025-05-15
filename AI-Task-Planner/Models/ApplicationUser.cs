using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Task_Planner.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            AssignedTasks = new HashSet<UserTask>();
            TasksAssigned = new HashSet<UserTask>();
        }
        
        [Required]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastActive { get; set; }
          // Navigation collections
        [InverseProperty("AssignedToUser")]
        public virtual ICollection<UserTask> AssignedTasks { get; set; } // Tasks assigned to this user
        
        [InverseProperty("AssignedByUser")]
        public virtual ICollection<UserTask> TasksAssigned { get; set; } // Tasks assigned by this user
    }
}
