using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AI_Task_Planner.Models
{    public class TaskTimeLog
    {
        [Key]
        public int LogId { get; set; }
        
        [Required]
        public int TaskId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }
        
        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }
        
        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Display(Name = "Is Paused")]
        public bool IsPaused { get; set; } = false;
        
        [Display(Name = "Pause Time")]
        public DateTime? PauseTime { get; set; }
        
        [Display(Name = "Total Paused Time (minutes)")]
        public int TotalPausedMinutes { get; set; } = 0;
        
        [Display(Name = "Duration (minutes)")]
        [NotMapped]
        public int DurationMinutes 
        { 
            get 
            {
                if (EndTime.HasValue)
                    return (int)(EndTime.Value - StartTime).TotalMinutes - TotalPausedMinutes;
                else if (IsPaused && PauseTime.HasValue)
                    return (int)(PauseTime.Value - StartTime).TotalMinutes - TotalPausedMinutes;
                else
                    return (int)(DateTime.Now - StartTime).TotalMinutes - TotalPausedMinutes;
            } 
        }
        
        // Navigation properties
        [ForeignKey("TaskId")]
        public virtual UserTask Task { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
