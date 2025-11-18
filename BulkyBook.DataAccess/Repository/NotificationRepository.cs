using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        private ApplicationDBContext _db;

        public NotificationRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Notification notification)
        {
            _db.Notifications.Update(notification);
        }

        public IEnumerable<Notification> GetUnreadNotifications(string userId)
        {
            return _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _db.Notifications.Find(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _db.Notifications.Update(notification);
            }
        }

        public void MarkAllAsRead(string userId)
        {
            var notifications = _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            _db.Notifications.UpdateRange(notifications);
        }
    }
}

