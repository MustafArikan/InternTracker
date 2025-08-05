using System.ComponentModel.DataAnnotations.Schema;

namespace InternTracker.Models
{
    public class ResourceFile
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FilePath { get; set; }
        public int UploadedByUserId { get; set; }
        [ForeignKey("UploadedByUserId")]
        public virtual AppUser UploadedByUser { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
