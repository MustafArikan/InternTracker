using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public enum UserRole { Admin, Mentor, Intern }

    public class AppUser
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Username { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(255)] 
        public string PasswordHash { get; set; }
        [Required]
        public UserRole Role { get; set; }
        public string ProfilePicturePath { get; set; } 
        public DateTime RegistrationDate { get; set; }
    }
}
