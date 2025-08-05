using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public class Report
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string FilePath { get; set; }
        public string Feedback { get; set; }
        public string MentorFeedback { get; set; }
    }
}
