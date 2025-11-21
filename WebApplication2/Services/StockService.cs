using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Hubs;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace BulkyBook.Services
{
    public class StockService : IStockService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public StockService(
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

        public async Task ProcessOrderStockDeduction(int orderId)
        {
            try
            {
                // Get order details (include FlashSaleItem to check if from flash sale)
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    o => o.OrderHeaderId == orderId,
                    includeProperties: "Product,FlashSaleItem"
                ).ToList();

                foreach (var detail in orderDetails)
                {
                    // 🔥 FLASH SALE DEDUCTION: Deduct from flash sale quantity first
                    if (detail.FlashSaleItemId.HasValue && detail.FlashSaleItem != null)
                    {
                        await DeductFlashSaleQuantity(detail.FlashSaleItemId.Value, detail.Count);
                    }

                    // Decrease product stock (regular stock)
                    bool stockDecreased = await DecreaseStock(detail.ProductId, detail.Count);

                    if (stockDecreased)
                    {
                        // Check if we need to send alerts
                        await CheckAndNotifyStockLevels(detail.ProductId);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing stock deduction for order {orderId}: {ex.Message}");
                // Don't throw - we don't want stock errors to break order confirmation
            }
        }

        // 🔥 NEW METHOD: Deduct Flash Sale Quantity
        private async Task<bool> DeductFlashSaleQuantity(int flashSaleItemId, int quantity)
        {
            try
            {
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(
                    f => f.Id == flashSaleItemId,
                    includeProperties: "Product,FlashSale"
                );

                if (flashSaleItem == null)
                {
                    Console.WriteLine($"Flash sale item {flashSaleItemId} not found");
                    return false;
                }

                // Check if we have enough flash sale quantity
                if (flashSaleItem.FlashSaleQuantity < quantity)
                {
                    Console.WriteLine($"⚠️ Insufficient flash sale quantity for item {flashSaleItemId}. Available: {flashSaleItem.FlashSaleQuantity}, Requested: {quantity}");
                    // Still decrease to 0 to prevent negative
                    flashSaleItem.FlashSaleQuantity = 0;
                }
                else
                {
                    flashSaleItem.FlashSaleQuantity -= quantity;
                }

                _unitOfWork.FlashSaleItem.Update(flashSaleItem);
                _unitOfWork.save();

                Console.WriteLine($"🔥 Flash sale quantity deducted for item {flashSaleItemId}: {quantity} units. Remaining: {flashSaleItem.FlashSaleQuantity}");
                
                // Log if flash sale item is now sold out
                if (flashSaleItem.FlashSaleQuantity == 0)
                {
                    Console.WriteLine($"🔥💥 Flash sale item {flashSaleItemId} ({flashSaleItem.Product?.Title}) is now SOLD OUT!");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deducting flash sale quantity for item {flashSaleItemId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DecreaseStock(int productId, int quantity)
        {
            try
            {
                var product = _unitOfWork.product.Get(p => p.Id == productId);
                
                if (product == null)
                {
                    Console.WriteLine($"Product {productId} not found");
                    return false;
                }

                // Check if we have enough stock
                if (product.StockQuantity < quantity)
                {
                    Console.WriteLine($"Insufficient stock for product {productId}. Available: {product.StockQuantity}, Requested: {quantity}");
                    // Still decrease to 0 to prevent negative stock
                    product.StockQuantity = 0;
                }
                else
                {
                    product.StockQuantity -= quantity;
                }

                _unitOfWork.product.update(product);
                _unitOfWork.save();

                Console.WriteLine($"Stock decreased for product {productId}: {quantity} units. Remaining: {product.StockQuantity}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decreasing stock for product {productId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IncreaseStock(int productId, int quantity)
        {
            try
            {
                var product = _unitOfWork.product.Get(p => p.Id == productId);
                
                if (product == null)
                {
                    Console.WriteLine($"Product {productId} not found");
                    return false;
                }

                product.StockQuantity += quantity;
                _unitOfWork.product.update(product);
                _unitOfWork.save();

                Console.WriteLine($"Stock increased for product {productId}: {quantity} units. New total: {product.StockQuantity}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error increasing stock for product {productId}: {ex.Message}");
                return false;
            }
        }

        public async Task CheckAndNotifyStockLevels(int productId)
        {
            try
            {
                var product = _unitOfWork.product.Get(p => p.Id == productId);
                
                if (product == null)
                {
                    return;
                }

                // Check stock status
                bool isOutOfStock = product.StockQuantity == 0;
                bool isLowStock = product.StockQuantity > 0 && product.StockQuantity <= product.MinimumStockAlert;

                // Only notify if stock is low or out
                if (isOutOfStock || isLowStock)
                {
                    await SendStockAlertToAdmins(product, isOutOfStock);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking stock levels for product {productId}: {ex.Message}");
            }
        }

        private async Task SendStockAlertToAdmins(Product product, bool isOutOfStock)
        {
            try
            {
                // Get admin email from configuration (fallback to all admins)
                var adminNotificationEmail = _configuration["StockAlerts:AdminEmail"];
                var adminUser = await _userManager.FindByEmailAsync(adminNotificationEmail??string.Empty);
                // Prepare notification details
                string title = isOutOfStock ? "⚠️ Product Out of Stock" : "📉 Low Stock Alert";
                string message = isOutOfStock
                    ? $"Product '{product.Title}' is now OUT OF STOCK!"
                    : $"Product '{product.Title}' stock is low! Only {product.StockQuantity} units remaining (Alert level: {product.MinimumStockAlert})";

                string urgency = isOutOfStock ? "URGENT" : "WARNING";
                 
                 
                    if (!string.IsNullOrEmpty(adminUser?.Id))
                    {
                        // Save notification to database
                        await LogStockNotification(adminUser.Id, product, isOutOfStock);

                        // Send email
                        var emailBody = GenerateStockAlertEmailTemplate(product, isOutOfStock);
                        await _emailSender.SendEmailAsync(
                            adminNotificationEmail,
                            $"[{urgency}] Stock Alert: {product.Title}",
                            emailBody
                        );

                        Console.WriteLine($"Stock alert email sent to admin: {adminNotificationEmail}");
                    }
                

                // Send real-time push notification to all admins
                await _hubContext.Clients.Group("Admins").SendAsync(
                    "ReceiveStockAlert",
                    new
                    {
                        title = title,
                        message = message,
                        productId = product.Id,
                        productName = product.Title,
                        stockQuantity = product.StockQuantity,
                        minimumAlert = product.MinimumStockAlert,
                        isOutOfStock = isOutOfStock,
                        urgency = urgency,
                        timestamp = DateTime.Now
                    }
                );

                Console.WriteLine($"Stock alert push notification sent for product: {product.Title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending stock alert: {ex.Message}");
            }
        }

        private async Task LogStockNotification(string adminUserId, Product product, bool isOutOfStock)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = adminUserId,
                    Title = isOutOfStock ? "Product Out of Stock" : "Low Stock Alert",
                    Message = isOutOfStock
                        ? $"'{product.Title}' is now OUT OF STOCK and cannot be ordered."
                        : $"'{product.Title}' has only {product.StockQuantity} units left (Alert threshold: {product.MinimumStockAlert})",
                    Type = "StockAlert",
                    RelatedId = product.Id,
                    IsRead = false,
                    Link = string.Empty,
                    Icon =string.Empty,
                    CreatedAt = DateTime.Now
                };

                _unitOfWork.notification.Add(notification);
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging stock notification: {ex.Message}");
            }
        }

        private string GenerateStockAlertEmailTemplate(Product product, bool isOutOfStock)
        {
            string statusColor = isOutOfStock ? "#ef4444" : "#f59e0b";
            string statusText = isOutOfStock ? "OUT OF STOCK" : "LOW STOCK";
            string statusIcon = isOutOfStock ? "❌" : "⚠️";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background: linear-gradient(135deg, {statusColor}, {(isOutOfStock ? "#dc2626" : "#ea580c")});
            color: white;
            padding: 30px;
            text-align: center;
            border-radius: 10px 10px 0 0;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
        }}
        .header .icon {{
            font-size: 48px;
            margin-bottom: 10px;
        }}
        .content {{
            background: #f9fafb;
            padding: 30px;
            border: 1px solid #e5e7eb;
        }}
        .alert-box {{
            background: white;
            border-left: 4px solid {statusColor};
            padding: 20px;
            margin: 20px 0;
            border-radius: 4px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .product-info {{
            background: white;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        .product-info h2 {{
            color: #1976D2;
            margin-top: 0;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            padding: 10px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .label {{
            font-weight: 600;
            color: #6b7280;
        }}
        .value {{
            font-weight: 700;
            color: #1f2937;
        }}
        .action-section {{
            background: #eff6ff;
            padding: 20px;
            margin: 20px 0;
            border-radius: 8px;
            border: 1px solid #bfdbfe;
        }}
        .action-section h3 {{
            color: #1976D2;
            margin-top: 0;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 30px;
            background: linear-gradient(135deg, #3B9DD5, #1976D2);
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 600;
            margin-top: 10px;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            color: #6b7280;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">{statusIcon}</div>
        <h1>{statusText} ALERT</h1>
        <p style=""margin: 10px 0 0; font-size: 16px;"">Immediate Action Required</p>
    </div>
    
    <div class=""content"">
        <div class=""alert-box"">
            <h3 style=""color: {statusColor}; margin-top: 0;"">{statusIcon} Stock Alert Notification</h3>
            <p style=""font-size: 16px; margin: 0;"">
                {(isOutOfStock
                    ? $"The product <strong>'{product.Title}'</strong> is now completely OUT OF STOCK and cannot be ordered by customers."
                    : $"The product <strong>'{product.Title}'</strong> has reached the low stock threshold with only <strong>{product.StockQuantity} units</strong> remaining.")}
            </p>
        </div>

        <div class=""product-info"">
            <h2>📦 Product Details</h2>
            <div class=""info-row"">
                <span class=""label"">Product Name:</span>
                <span class=""value"">{product.Title}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Product ID:</span>
                <span class=""value"">#{product.Id}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Current Stock:</span>
                <span class=""value"" style=""color: {statusColor};"">{product.StockQuantity} units</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Alert Threshold:</span>
                <span class=""value"">{product.MinimumStockAlert} units</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Status:</span>
                <span class=""value"" style=""color: {statusColor};"">{statusText}</span>
            </div>
        </div>

        <div class=""action-section"">
            <h3>📋 Recommended Actions</h3>
            <ul style=""margin: 10px 0; padding-left: 20px;"">
                {(isOutOfStock
                    ? @"<li>Restock this product immediately</li>
                       <li>Contact suppliers for urgent delivery</li>
                       <li>Update product page with expected restock date</li>
                       <li>Notify customers on waitlist (if applicable)</li>"
                    : @"<li>Review sales velocity and order more stock</li>
                       <li>Check with suppliers for availability</li>
                       <li>Consider adjusting alert threshold if needed</li>
                       <li>Monitor stock levels daily</li>")}
            </ul>
            <a href=""https://yourwebsite.com/Admin/Product/Upsert/{product.Id}"" class=""btn"">
                Update Stock Now →
            </a>
        </div>

        <div style=""background: #fef3c7; padding: 15px; border-radius: 6px; border-left: 4px solid #f59e0b; margin: 20px 0;"">
            <strong>💡 Pro Tip:</strong> You can adjust the minimum stock alert level for each product in the admin panel to receive notifications earlier.
        </div>
    </div>
    
    <div class=""footer"">
        <p>This is an automated stock alert from Ideal Weight Inventory Management System</p>
        <p style=""margin-top: 10px;"">Generated on {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
        <p style=""color: #9ca3af; font-size: 12px; margin-top: 15px;"">
            To configure stock alert settings, update your appsettings.json file.
        </p>
    </div>
</body>
</html>";
        }
    }
}

