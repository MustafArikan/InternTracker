using System.Collections.Generic;

namespace InternTracker.Models
{
    public class InternDetailsViewModel
    {
        public AppUser Intern { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public List<JournalEntry> JournalEntries { get; set; }
        public List<Goal> Goals { get; set; }
        public List<Report> Reports { get; set; }
        public List<WorkSession> WorkSessions { get; set; }
        public List<Notification> Notifications { get; set; }
    }
}