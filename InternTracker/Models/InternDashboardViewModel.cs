using System.Collections.Generic;
using InternTracker.Models;

namespace InternTracker.Models
{
    public class InternDashboardViewModel
    {
        public AppUser Intern { get; set; }
        public List<TaskItem> Tasks { get; set; }
        public List<WorkSession> WorkSessions { get; set; }
        public List<Goal> Goals { get; set; }
        public List<Report> Reports { get; set; }

        // Data for Charts
        public List<string> TaskStatusLabels { get; set; }
        public List<int> TaskStatusCounts { get; set; }

        public List<string> TimeSpentWeeklyLabels { get; set; }
        public List<double> TimeSpentWeeklyData { get; set; }

        public List<string> GoalStatusLabels { get; set; }
        public List<int> GoalStatusCounts { get; set; }

        public List<string> ReportsSubmittedLabels { get; set; }
        public List<int> ReportsSubmittedCounts { get; set; }
    }
}