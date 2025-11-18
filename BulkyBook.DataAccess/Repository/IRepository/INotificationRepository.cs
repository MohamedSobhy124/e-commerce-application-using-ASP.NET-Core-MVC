using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        void Update(Notification notification);
        IEnumerable<Notification> GetUnreadNotifications(string userId);
        void MarkAsRead(int notificationId);
        void MarkAllAsRead(string userId);
    }
}

