using Microsoft.EntityFrameworkCore;
using InternTracker.Models;

namespace InternTracker.Data
{
    public class InternTrackerContext : DbContext
    {
        
        public InternTrackerContext(DbContextOptions<InternTrackerContext> options) : base(options) { }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Report> Reports { get; set; } 
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ResourceFile> ResourceFiles { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<WorkSession> WorkSessions { get; set; }

    }
}
