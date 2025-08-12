using Microsoft.EntityFrameworkCore;
using TaskPro.Models;
using Task = System.Threading.Tasks.Task;

namespace TaskPro.Controllers
{
    public interface INotificationService
    {
        Task AddNotificationAsync(string userId, string message);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, bool onlyUnread = true);
    }

    public class NotificationService : INotificationService
    {
        private readonly TaskProDbContext _db;

        public NotificationService(TaskProDbContext db)
        {
            _db = db;
        }

        public async Task AddNotificationAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, bool onlyUnread = true)
        {
            var query = _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            if (onlyUnread)
                query = (IOrderedQueryable<Notification>)query.Where(n => n.IsRead.HasValue && !n.IsRead.Value);

            return await query.ToListAsync();
        }
    }
}