using InternTracker.Data;
using InternTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InternTracker.Services
{
    public class NotificationService
    {
        private readonly InternTrackerContext _context;

        public NotificationService(InternTrackerContext context)
        {
            _context = context;
        }

        public async Task CreateNotification(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadNotifications(int userId)
        {
            return await _context.Notifications
                                 .Where(n => n.UserId == userId && !n.IsRead)
                                 .OrderByDescending(n => n.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCount(int userId)
        {
            return await _context.Notifications
                                 .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task NotifyInternOfAssignedTask(int internId, string taskTitle)
        {
            var message = $"You have been assigned a new task: '{taskTitle}'.";
            await CreateNotification(internId, message);
        }

        public async Task NotifyInternsOfNewResource(string resourceTitle)
        {
            var interns = await _context.AppUsers.Where(u => u.Role == UserRole.Intern).ToListAsync();
            foreach (var intern in interns)
            {
                var message = $"A new resource has been uploaded: '{resourceTitle}'.";
                await CreateNotification(intern.Id, message);
            }
        }

        public async Task NotifyMentorOfSubmittedReport(int mentorId, string internUsername)
        {
            var message = $"Intern '{internUsername}' has submitted a new report.";
            await CreateNotification(mentorId, message);
        }
    }
}
