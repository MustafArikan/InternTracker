using System.ComponentModel.DataAnnotations;

namespace InternTracker.Models
{
    public class UpdateEmailViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New Email")]
        public string NewEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
