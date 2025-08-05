using System.ComponentModel.DataAnnotations;

namespace InternTracker.Models
{
    public class LoginViewModel
    {
        [Required]
[EmailAddress]
public string Email { get; set; }

        [DataType(DataType.Password)]
        [Required]
        public string Password { get; set; }

    }
}
