using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace InternTracker.Models
{
    public class UploadProfilePictureViewModel
    {
        [Required]
        [Display(Name = "Profile Picture")]
        public IFormFile ProfilePicture { get; set; }
    }
}
