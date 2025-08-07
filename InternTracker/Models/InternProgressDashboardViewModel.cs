using System.Collections.Generic;
using InternTracker.Models;

namespace InternTracker.Models
{
    public class InternProgressDashboardViewModel
    {
        public AppUser Intern { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public List<JournalEntry> JournalEntries { get; set; }
        public List<Goal> Goals { get; set; }
        public List<Report> Reports { get; set; }
        public List<WorkSession> WorkSessions { get; set; }
        public List<Notification> Notifications { get; set; } // Potentially useful for activity logs

        // Data for Charts (pre-processed for easier Chart.js consumption)
        public List<string> TaskStatusLabels { get; set; }
        public List<int> TaskStatusCounts { get; set; }

        public List<string> GoalStatusLabels { get; set; }
        public List<int> GoalStatusCounts { get; set; }

        public List<string> WorkSessionDates { get; set; }
        public List<int> WorkSessionMinutes { get; set; }

        public List<string> JournalEntryDates { get; set; }
        public List<int> JournalEntryCounts { get; set; }

        public List<string> ReportSubmissionDates { get; set; }
        public List<int> ReportSubmissionCounts { get; set; }

        // For Activity Log
        public List<string> ActivityLogs { get; set; }

        public List<string> IndividualWorkSessionLabels { get; set; }
        public List<int> IndividualWorkSessionMinutes { get; set; }
    }
}