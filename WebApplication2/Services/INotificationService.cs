using BulkyBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BulkyBook.Services
{
    public interface INotificationService
    {
        Task SendOrderNotificationToAdmins(OrderHeader orderHeader);
        Task SendOrderConfirmationToCustomer(OrderHeader orderHeader, ApplicationUser customer);
        Task SendOrderConfirmationToCustomerGuest(OrderHeader orderHeader);
        Task LogNotification(string userId, string title, string message, string type, int? orderId = null);
        Task<IEnumerable<Notification>> GetUserNotifications(string userId);
        Task MarkAsRead(int notificationId);
    }
}

