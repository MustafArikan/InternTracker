namespace InternTracker.Models
{
    public class SystemReportsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalInterns { get; set; }
        public int TotalMentors { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalMentorUploads { get; set; }
        public int TotalAdminUploads { get; set; }
        public List<string> RegistrationDates { get; set; }
        public List<int> RegistrationCounts { get; set; }
    }
}