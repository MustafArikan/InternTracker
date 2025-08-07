using System.ComponentModel.DataAnnotations;

namespace InternTracker.Models
{
    public class ChangeUsernameViewModel
    {
        [Display(Name = "Current Username")]
        public string CurrentUsername { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 3)]
        [Display(Name = "New Username")]
        public string NewUsername { get; set; }
    }
}