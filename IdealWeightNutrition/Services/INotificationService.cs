using IdealWeightNutrition.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Services
{
    public interface INotificationService
    {
        Task SendOrderNotificationToAdmins(OrderHeader orderHeader);
        Task SendOrderConfirmationToCustomer(OrderHeader orderHeader, ApplicationUser customer);
        Task SendOrderConfirmationToCustomerGuest(OrderHeader orderHeader);
        Task SendOrderDeliveredNotification(OrderHeader orderHeader);
        Task SendReturnRequestNotificationToAdmins(ReturnRequest returnRequest);
        Task SendReturnRequestStatusUpdateToCustomer(ReturnRequest returnRequest);
        Task LogNotification(string userId, string title, string message, string type, int? orderId = null, int? returnRequestId = null);
        Task<IEnumerable<Notification>> GetUserNotifications(string userId);
        Task MarkAsRead(int notificationId);
    }
}

