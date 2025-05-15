using System;

namespace AI_Task_Planner.Models
{
    /// <summary>
    /// Represents the result of parsing natural language input into a task
    /// </summary>
    public class TaskParsingResult
    {
        /// <summary>
        /// Whether the parsing was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Any error message if parsing failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// The parsed task if parsing was successful
        /// </summary>
        public UserTask? Task { get; set; }
    }
}
