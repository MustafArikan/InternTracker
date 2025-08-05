using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace InternTracker.Models
{
    public enum TaskStatus { NotStarted, InProgress, Completed }

    public class TaskItem
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime AssignedDate { get; set; }
        public TaskStatus Status { get; set; }
        public int AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        [ValidateNever]
        public virtual AppUser AssignedToUser { get; set; }
    }
}