using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual AppUser User { get; set; } 

        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Please write something in your journal entry.")]
        [Display(Name = "Your Entry")] 
        public string EntryText { get; set; }
    }
}