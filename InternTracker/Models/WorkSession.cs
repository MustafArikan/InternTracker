using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public class WorkSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalMinutes { get; set; }
        public int? TaskId { get; set; }
        [ForeignKey("TaskId")]
        public virtual TaskItem? TaskItem { get; set; }
    }
}
