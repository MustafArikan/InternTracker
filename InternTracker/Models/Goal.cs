using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public enum GoalStatus { Active, Completed, Reflected }

    public class Goal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
        public string GoalTitle { get; set; }
        public DateTime CreatedDate { get; set; }
        public GoalStatus Status { get; set; }
        public string ReflectionText { get; set; }
    }
}
