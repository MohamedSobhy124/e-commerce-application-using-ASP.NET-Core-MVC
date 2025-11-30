using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Hubs;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        public NotificationService(
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IHubContext<NotificationHub> hubContext,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _hubContext = hubContext;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task SendOrderNotificationToAdmins(OrderHeader orderHeader)
        {
            // Get all admin users
            var adminNotificationEmail = _configuration["StockAlerts:AdminEmail"];
            //var adminUser = await _userManager.FindByEmailAsync(adminNotificationEmail ?? string.Empty);

            if (!string.IsNullOrEmpty(adminNotificationEmail))
            {
           
                // Send email to admin
                var emailBody = GenerateAdminEmailTemplate(orderHeader);
                await _emailSender.SendEmailAsync(
                    adminNotificationEmail ?? string.Empty,
                    $"New Order #{orderHeader.Id} - Ideal Weight",
                    emailBody
                );


                 
            }
            var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
            foreach (var admin in adminUsers)
            {
                // Log notification in database
                await LogNotification(
                    admin.Id,
                    "New Order Received",
                    $"New order #{orderHeader.Id} has been placed by {orderHeader.Name}. Total: {orderHeader.OrderTotal:C}",
                    "Order",
                    orderHeader.Id
                );
            }
            // Send real-time push notification to all connected admins
            await _hubContext.Clients.Group("Admins").SendAsync(
                "ReceiveOrderNotification",
                new
                {
                    title = "New Order Received",
                    message = $"Order #{orderHeader.Id} - {orderHeader.Name}",
                    orderId = orderHeader.Id,
                    total = orderHeader.OrderTotal,
                    timestamp = BulkyBook.Utility.DateTimeHelper.Now
                }
            );

        }

        public async Task SendOrderConfirmationToCustomer(OrderHeader orderHeader, ApplicationUser customer)
        {
            // Log notification in database
            await LogNotification(
                customer.Id,
                "Order Confirmed",
                $"Your order #{orderHeader.Id} has been confirmed. Total: {orderHeader.OrderTotal:C}",
                "Order",
                orderHeader.Id
            );

            // Get order details
            var orderDetails = _unitOfWork.OrderDetail.GetAll(
                o => o.OrderHeaderId == orderHeader.Id,
                includeProperties: "Product"
            ).ToList();

            // Send email to customer
            var emailBody = GenerateCustomerEmailTemplate(orderHeader, orderDetails, customer);
            await _emailSender.SendEmailAsync(
                customer.Email,
                $"Order Confirmation #{orderHeader.Id} - Ideal Weight",
                emailBody
            );

            // Send real-time notification to customer
            await _hubContext.Clients.User(customer.Id).SendAsync(
                "ReceiveOrderConfirmation",
                new
                {
                    title = "Order Confirmed",
                    message = $"Your order #{orderHeader.Id} has been confirmed",
                    orderId = orderHeader.Id,
                    total = orderHeader.OrderTotal,
                    timestamp = BulkyBook.Utility.DateTimeHelper.Now
                }
            );
        }
              
        public async Task SendOrderConfirmationToCustomerGuest(OrderHeader orderHeader)
        {
     
            // Get order details (include ComboOffer to show combo names in email)
            var orderDetails = _unitOfWork.OrderDetail.GetAll(
                o => o.OrderHeaderId == orderHeader.Id,
                includeProperties: "Product,ComboOffer"
            ).ToList();

            // Send email to customer
            var emailBody = GenerateCustomerEmailTemplate(orderHeader, orderDetails, null);
            await _emailSender.SendEmailAsync(
                orderHeader.Email??string.Empty,
                $"Order Confirmation #{orderHeader.Id} - Ideal Weight",
                emailBody
            );

        
        }

        public async Task LogNotification(string userId, string title, string message, string type, int? orderId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                OrderId = orderId,
                IsRead = false,
                CreatedAt = BulkyBook.Utility.DateTimeHelper.Now,
                Icon = type == "Order" ? "bi-cart-check" : "bi-bell",
                Link = orderId.HasValue ? $"/Admin/Order/Details/{orderId}" : null
            };

            _unitOfWork.notification.add(notification);
            _unitOfWork.save();

            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Notification>> GetUserNotifications(string userId)
        {
            return await Task.FromResult(
                _unitOfWork.notification.GetUnreadNotifications(userId)
            );
        }

        public async Task MarkAsRead(int notificationId)
        {
            _unitOfWork.notification.MarkAsRead(notificationId);
            _unitOfWork.save();
            await Task.CompletedTask;
        }

        private string GenerateAdminEmailTemplate(OrderHeader orderHeader)
        {
            var itemsHtml = new StringBuilder();
            
            try
            {
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    o => o.OrderHeaderId == orderHeader.Id,
                    includeProperties: "Product,ComboOffer"
                ).ToList();

                foreach (var item in orderDetails)
                {
                    // Check if this is a combo offer
                    bool isComboOffer = item.ComboOfferId.HasValue && item.ComboOffer != null;
                    
                    // Get display name - use combo name if it's a combo, otherwise use product name
                    var displayTitle = isComboOffer 
                        ? item.ComboOffer.Name 
                        : (item.Product?.Title ?? "Unknown Product");
                    
                    var productAuthor = item.Product?.Author ?? "";
                    var comboBadge = isComboOffer ? "<span style='background: #fef3c7; color: #92400e; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600; margin-left: 8px;'>COMBO</span>" : "";
                    
                    itemsHtml.AppendLine($@"
                        <tr>
                            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb;'>{displayTitle}{comboBadge}</td>
                            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: center;'>{item.Count}</td>
                            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{item.Price:C}</td>
                            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700;'>{(item.Price * item.Count):C}</td>
                        </tr>
                    ");
                }
            }
            catch (Exception ex)
            {
                // Log error and return basic template without order details
                // In production, you might want to log this to a logging service
                itemsHtml.Clear();
                itemsHtml.AppendLine($@"
                    <tr>
                        <td colspan='4' style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: center; color: #ef4444;'>
                            Unable to load order details. Please check the order in the admin panel.
                        </td>
                    </tr>
                ");
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; background-color: #f9fafb; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 800;'>🎉 New Order Received!</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Order #{orderHeader.Id}</p>
        </div>

        <!-- Content -->
        <div style='padding: 30px;'>
            <div style='background: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px;'>Customer Information</h2>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr>
                        <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Name:</td>
                        <td style='padding: 8px 0; color: #1f2937; font-weight: 700;'>{orderHeader.Name}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Phone:</td>
                        <td style='padding: 8px 0; color: #1f2937;'>{orderHeader.PhoneNumber}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Address:</td>
                        <td style='padding: 8px 0; color: #1f2937;'>{orderHeader.StreetAddress}, {orderHeader.City}, {orderHeader.State} {orderHeader.PostalCode}</td>
                    </tr>
                    <tr>
                        <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Order Date:</td>
                        <td style='padding: 8px 0; color: #1f2937;'>{orderHeader.OrderDate:MMM dd, yyyy hh:mm tt}</td>
                    </tr>
                </table>
            </div>

            <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px;'>Order Items</h2>
            <table style='width: 100%; border-collapse: collapse; margin-bottom: 25px;'>
                <thead>
                    <tr style='background: #f3f4f6;'>
                        <th style='padding: 12px; text-align: left; color: #374151; font-weight: 700;'>Product</th>
                        <th style='padding: 12px; text-align: center; color: #374151; font-weight: 700;'>Qty</th>
                        <th style='padding: 12px; text-align: right; color: #374151; font-weight: 700;'>Price</th>
                        <th style='padding: 12px; text-align: right; color: #374151; font-weight: 700;'>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
                <tfoot>
                    <tr style='background: #f9fafb;'>
                        <td colspan='3' style='padding: 15px; text-align: right; font-weight: 700; font-size: 18px; color: #1f2937;'>Order Total:</td>
                        <td style='padding: 15px; text-align: right; font-weight: 900; font-size: 20px; color: #059669;'>{orderHeader.OrderTotal:C}</td>
                    </tr>
                </tfoot>
            </table>

            <div style='background: #ede9fe; border-left: 4px solid #7c3aed; border-radius: 8px; padding: 15px; margin-bottom: 25px;'>
                <p style='margin: 0; color: #6d28d9; font-weight: 600;'>
                    <strong>Payment Status:</strong> {orderHeader.PaymentStatus}
                </p>
                <p style='margin: 10px 0 0 0; color: #6d28d9; font-weight: 600;'>
                    <strong>Order Status:</strong> {orderHeader.OrderStatus}
                </p>
            </div>

            <div style='text-align: center; margin-top: 30px;'>
                <a href='{GetBaseUrl()}/Admin/Order/Details/{orderHeader.Id}' style='display: inline-block; background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 8px; font-weight: 700; font-size: 16px;'>
                    View Order Details
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style='background: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb;'>
            <p style='color: #6b7280; margin: 0; font-size: 14px;'>
                © 2025 Ideal Weight. All rights reserved.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateCustomerEmailTemplate(OrderHeader orderHeader, List<OrderDetail> orderDetails, ApplicationUser? customer)
        {
            var itemsHtml = new StringBuilder();
            foreach (var item in orderDetails)
            {
                // Check if this is a combo offer
                bool isComboOffer = item.ComboOfferId.HasValue && item.ComboOffer != null;
                
                // Get display name - use combo name if it's a combo, otherwise use product name
                var displayTitle = isComboOffer 
                    ? item.ComboOffer.Name 
                    : (item.Product?.Title ?? "Unknown Product");
                
                var productAuthor = item.Product?.Author ?? "";
                var comboBadge = isComboOffer ? "<span style='background: #fef3c7; color: #92400e; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 600; margin-left: 8px;'>COMBO OFFER</span>" : "";
                
                itemsHtml.AppendLine($@"
                    <tr>
                        <td style='padding: 12px; border-bottom: 1px solid #e5e7eb;'>
                            <strong style='color: #1f2937; display: block; margin-bottom: 5px;'>{displayTitle}{comboBadge}</strong>
                            {(!string.IsNullOrEmpty(productAuthor) && !isComboOffer ? $"<small style='color: #6b7280;'>by {productAuthor}</small>" : "")}
                            {(isComboOffer ? $"<small style='color: #6b7280;'>{item.ComboOffer.TotalProducts} products included</small>" : "")}
                        </td>
                        <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: center;'>{item.Count}</td>
                        <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right;'>{item.Price:C}</td>
                        <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; font-weight: 700; color: #059669;'>{(item.Price * item.Count):C}</td>
                    </tr>
                ");
            }

            var estimatedDelivery = BulkyBook.Utility.DateTimeHelper.Now.AddDays(7).ToString("MMM dd, yyyy") + " - " + BulkyBook.Utility.DateTimeHelper.Now.AddDays(14).ToString("MMM dd, yyyy");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; background-color: #f9fafb; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); padding: 40px; text-align: center;'>
            <div style='font-size: 48px; margin-bottom: 15px;'>✅</div>
            <h1 style='color: #ffffff; margin: 0; font-size: 32px; font-weight: 800;'>Order Confirmed!</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Thank you for your purchase,  {(customer != null ? customer.Name : orderHeader.Name)?? "Our valued Customer"}</p>
        </div>

        <!-- Content -->
        <div style='padding: 30px;'>
            <div style='background: #dcfce7; border-left: 4px solid #059669; border-radius: 8px; padding: 15px; margin-bottom: 25px;'>
                <p style='margin: 0; color: #047857; font-weight: 600; font-size: 15px;'>
                    ✓ Your order has been successfully placed and confirmed!
                </p>
            </div>

            <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px; border-bottom: 2px solid #e5e7eb; padding-bottom: 10px;'>
                Order #<span style='color: #7c3aed;'>{orderHeader.Id}</span>
            </h2>

            <table style='width: 100%; margin-bottom: 25px;'>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Order Date:</td>
                    <td style='padding: 8px 0; color: #1f2937; font-weight: 700; text-align: right;'>{orderHeader.OrderDate:MMM dd, yyyy hh:mm tt}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Payment Status:</td>
                    <td style='padding: 8px 0; color: #059669; font-weight: 700; text-align: right;'>{orderHeader.PaymentStatus}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Estimated Delivery:</td>
                    <td style='padding: 8px 0; color: #ef4444; font-weight: 700; text-align: right;'>{estimatedDelivery}</td>
                </tr>
            </table>

            <h3 style='color: #1f2937; margin: 25px 0 15px 0; font-size: 18px;'>Order Items</h3>
            <table style='width: 100%; border-collapse: collapse; margin-bottom: 25px;'>
                <thead>
                    <tr style='background: #f3f4f6;'>
                        <th style='padding: 12px; text-align: left; color: #374151; font-weight: 700;'>Product</th>
                        <th style='padding: 12px; text-align: center; color: #374151; font-weight: 700;'>Qty</th>
                        <th style='padding: 12px; text-align: right; color: #374151; font-weight: 700;'>Price</th>
                        <th style='padding: 12px; text-align: right; color: #374151; font-weight: 700;'>Total</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <div style='background: linear-gradient(135deg, #1f2937 0%, #111827 100%); border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <div style='display: flex; justify-content: space-between; align-items: center;'>
                    <span style='color: rgba(255,255,255,0.85); font-size: 18px; font-weight: 600;'>Order Total:</span>
                    <span style='color: #ffffff; font-size: 28px; font-weight: 900;'>{orderHeader.OrderTotal:C}</span>
                </div>
            </div>

            <h3 style='color: #1f2937; margin: 25px 0 15px 0; font-size: 18px;'>Shipping Address</h3>
            <div style='background: #f9fafb; border-radius: 8px; padding: 20px;'>
                <p style='margin: 0 0 5px 0; color: #1f2937; font-weight: 700; font-size: 16px;'>{orderHeader.Name}</p>
                <p style='margin: 0 0 5px 0; color: #6b7280;'>{orderHeader.StreetAddress}</p>
                <p style='margin: 0 0 5px 0; color: #6b7280;'>{orderHeader.City}, {orderHeader.State} {orderHeader.PostalCode}</p>
                <p style='margin: 0; color: #6b7280;'>📞 {orderHeader.PhoneNumber}</p>
            </div>

            <div style='text-align: center; margin-top: 30px;'>
                <a href='{GetBaseUrl()}/Customer/Order/Details/{orderHeader.Id}' style='display: inline-block; background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 8px; font-weight: 700; font-size: 16px; margin-right: 10px;'>
                    Track Order
                </a>
                <a href='{GetBaseUrl()}' style='display: inline-block; background: #ffffff; color: #7c3aed; text-decoration: none; padding: 15px 40px; border-radius: 8px; font-weight: 700; font-size: 16px; border: 2px solid #7c3aed;'>
                    Continue Shopping
                </a>
            </div>

            <div style='background: #ede9fe; border-radius: 8px; padding: 20px; margin-top: 30px; text-align: center;'>
                <p style='margin: 0 0 10px 0; color: #6d28d9; font-weight: 600; font-size: 16px;'>Need help with your order?</p>
                <p style='margin: 0; color: #7c3aed; font-size: 14px;'>Contact our support team 24/7</p>
            </div>
        </div>

        <!-- Footer -->
        <div style='background: #f9fafb; padding: 25px; text-align: center; border-top: 1px solid #e5e7eb;'>
            <p style='color: #6b7280; margin: 0 0 10px 0; font-size: 14px;'>
                © 2025 Ideal Weight. All rights reserved.
            </p>
            <p style='color: #9ca3af; margin: 0; font-size: 12px;'>
                This is an automated email. Please do not reply to this message.
            </p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetBaseUrl()
        {
            // You can configure this in appsettings.json or get it from HttpContext
            return "https://msobhyapp.runasp.net";
        }
    }
}

