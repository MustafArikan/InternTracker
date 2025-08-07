using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Recipient of the notification
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        // Optional: To link to the source of the notification (e.g., Task, Report, ResourceFile)
        public string NotificationType { get; set; } // e.g., "TaskAssigned", "ResourceUploaded", "ReportSubmitted"
        public int? RelatedEntityId { get; set; } // ID of the related Task, Report, or ResourceFile
    }
}