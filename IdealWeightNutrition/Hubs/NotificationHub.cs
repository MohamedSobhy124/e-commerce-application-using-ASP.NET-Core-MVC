using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotificationToAdmins(string title, string message, int orderId)
        {
            await Clients.Group("Admins").SendAsync("ReceiveNotification", title, message, orderId);
        }

        public async Task SendNotificationToUser(string userId, string title, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        }
    }
}

