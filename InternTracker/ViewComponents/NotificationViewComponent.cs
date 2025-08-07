using InternTracker.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;

namespace InternTracker.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly NotificationService _notificationService;

        public NotificationViewComponent(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int userId = 0;
            if (User.Identity.IsAuthenticated)
            {
                var userIdString = (User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdString, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            var unreadCount = await _notificationService.GetUnreadNotificationCount(userId);
            ViewBag.UnreadNotificationCount = unreadCount;
            return View();
        }
    }
}
