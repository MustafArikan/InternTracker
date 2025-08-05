using System.ComponentModel.DataAnnotations;

namespace InternTracker.Models
{
    public class AdminCreateUserViewModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Required]
        public UserRole Role { get; set; }
    }
}