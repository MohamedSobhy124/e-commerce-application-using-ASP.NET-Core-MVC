using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Hubs;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace IdealWeightNutrition.Services
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
                // Get order details (include FlashSaleItem and ComboOffer to check if from flash sale or combo)
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    o => o.OrderHeaderId == orderId,
                    includeProperties: "Product,FlashSaleItem,ComboOffer,ProductVariant"
                ).ToList();

                foreach (var detail in orderDetails)
                {
                    // 🔥 COMBO OFFER DEDUCTION: Handle combo offers separately
                    if (detail.IsFromComboOffer)
                    {
                        await DeductComboOfferStock(detail.ComboOfferId.Value, detail.Count);
                        continue; // Skip regular deduction for combo items
                    }

                    // 🔥 FLASH SALE DEDUCTION: Deduct from flash sale quantity first
                    if (detail.IsFromFlashSale)
                    {
                        await DeductFlashSaleQuantity(detail.FlashSaleItemId.Value, detail.Count);
                    } 
                    if (detail.ProductVariantId.HasValue && detail.ProductVariant != null)
                    {
                        await DeductVariantQuantity(detail.ProductVariantId.Value, detail.Count);
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
            }
        }

        // 🔥 NEW METHOD: Deduct Combo Offer Stock
        private async Task<bool> DeductComboOfferStock(int comboOfferId, int comboQuantity)
        {
            try
            {
                // Get combo offer with all items
                var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(comboOfferId);
                
                if (comboOffer == null)
                {
                    return false;
                }

                if (comboOffer.ComboOfferItems == null || !comboOffer.ComboOfferItems.Any())
                {
                    return false;
                }


                // Process each item in the combo
                foreach (var comboItem in comboOffer.ComboOfferItems.Where(i => !i.IsDeleted))
                {
                    // Calculate total quantity needed: combo item quantity * number of combos purchased
                    int totalQuantityNeeded = comboItem.Quantity * comboQuantity;

                    // Handle variant products
                    if (comboItem.ProductVariantId.HasValue && comboItem.ProductVariant != null)
                    {
                        var variant = comboItem.ProductVariant;
                        
                        // Reload variant to get latest stock (in case it was modified)
                        variant = _unitOfWork.ProductVariant.Get(v => v.Id == variant.Id && !v.IsDeleted);
                        if (variant != null)
                        {
                            // Deduct from variant stock
                            if (variant.StockQuantity < totalQuantityNeeded)
                            {
                                variant.StockQuantity = 0; // Prevent negative
                            }
                            else
                            {
                                variant.StockQuantity -= totalQuantityNeeded;
                            }

                            _unitOfWork.ProductVariant.Update(variant);
                            _unitOfWork.save();


                            // Check for stock alerts
                            bool isOutOfStock = variant.StockQuantity == 0;
                            bool isLowStock = variant.StockQuantity > 0 && variant.StockQuantity <= variant.MinimumStockAlert;
                            
                            if (isOutOfStock || isLowStock)
                            {
                                await SendStockVariantAlertToAdmins(variant, isOutOfStock);
                            }
                        }
                    }
                    else if (comboItem.Product != null)
                    {
                        // Handle regular products - reload to get latest stock
                        var product = _unitOfWork.product.Get(p => p.Id == comboItem.ProductId && !p.IsDeleted);
                        if (product != null)
                        {
                            // Deduct from product stock
                            bool stockDecreased = await DecreaseStock(product.Id, totalQuantityNeeded);

                            if (stockDecreased)
                            {
                                await CheckAndNotifyStockLevels(product.Id);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
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
                    return false;
                }

                // Check if we have enough flash sale quantity
                if (flashSaleItem.FlashSaleQuantity < quantity)
                {
                    flashSaleItem.FlashSaleQuantity = 0;
                }
                else
                {
                    flashSaleItem.FlashSaleQuantity -= quantity;
                }

                _unitOfWork.FlashSaleItem.Update(flashSaleItem);
                _unitOfWork.save();


                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
         private async Task<bool> DeductVariantQuantity(int variantItemId, int quantity)
        {
            try
            {
                var variantItem = _unitOfWork.ProductVariant.Get( f => f.Id == variantItemId, includeProperties: "Product");

                if (variantItem == null)
                {
                    return false;
                }

                // Check if we have enough flash sale quantity
                if (variantItem.StockQuantity < quantity)
                {
                    variantItem.StockQuantity = 0;
                }
                else
                {
                    variantItem.StockQuantity -= quantity;
                }

                _unitOfWork.ProductVariant.Update(variantItem);
                _unitOfWork.save();

                bool isOutOfStock = variantItem.StockQuantity == 0;
                bool isLowStock = variantItem.StockQuantity > 0 && variantItem.StockQuantity <= variantItem.MinimumStockAlert;
                 
                if (isOutOfStock || isLowStock)
                {
                    await SendStockVariantAlertToAdmins(variantItem, isOutOfStock);
                }


                return true;
            }
            catch (Exception ex)
            {
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

                return true;
            }
            catch (Exception ex)
            {
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
                    return false;
                }

                product.StockQuantity += quantity;
                _unitOfWork.product.update(product);
                _unitOfWork.save();

                return true;
            }
            catch (Exception ex)
            {
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
                        timestamp = IdealWeightNutrition.Utility.DateTimeHelper.Now
                    }
                );

            }
            catch (Exception ex)
            {
            }
        }
          
        private async Task SendStockVariantAlertToAdmins(ProductVariant productVariant, bool isOutOfStock)
        {
            try
            {
                // Get admin email from configuration (fallback to all admins)
                var adminNotificationEmail = _configuration["StockAlerts:AdminEmail"];
                var adminUser = await _userManager.FindByEmailAsync(adminNotificationEmail??string.Empty);
                // Prepare notification details
                string title = isOutOfStock ? "⚠️ Product Variant Out of Stock" : "📉 Variant Low Stock Alert";
                string message = isOutOfStock
                    ? $"Product '{productVariant.Product.Title}' , Variant '{productVariant.VariantName}' is now OUT OF STOCK!"
                    : $"Product '{productVariant.Product.Title}' , Variant '{productVariant.VariantName}' stock is low! Only {productVariant.StockQuantity} units remaining (Alert level: {productVariant.MinimumStockAlert})";

                string urgency = isOutOfStock ? "URGENT" : "WARNING";
                 
                 
                    if (!string.IsNullOrEmpty(adminUser?.Id))
                    {
                        // Save notification to database
                        await LogVariantStockNotification(adminUser.Id, productVariant, isOutOfStock);

                        // Send email
                        var emailBody = GenerateVariantStockAlertEmailTemplate(productVariant, isOutOfStock);
                        await _emailSender.SendEmailAsync(
                            adminNotificationEmail?? string.Empty,
                            $"[{urgency}] Stock Alert: {productVariant.Product.Title} , Variant : {productVariant.VariantName}",
                            emailBody
                        );

                    }
                

                // Send real-time push notification to all admins
                await _hubContext.Clients.Group("Admins").SendAsync(
                    "ReceiveStockAlert",
                    new
                    {
                        title = title,
                        message = message,
                        productId = productVariant.ProductId,
                        productName = productVariant.Product.Title,
                        stockQuantity = productVariant.StockQuantity,
                        minimumAlert = productVariant.MinimumStockAlert,
                        isOutOfStock = isOutOfStock,
                        urgency = urgency,
                        timestamp = IdealWeightNutrition.Utility.DateTimeHelper.Now
                    }
                );

            }
            catch (Exception ex)
            {
            }
        }

        private Task LogStockNotification(string adminUserId, Product product, bool isOutOfStock)
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
                    CreatedAt = IdealWeightNutrition.Utility.DateTimeHelper.Now
                };

                _unitOfWork.notification.Add(notification);
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
        }       
        
        private Task LogVariantStockNotification(string adminUserId, ProductVariant productVariant, bool isOutOfStock)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = adminUserId,
                    Title = isOutOfStock ? "Product Variant Out of Stock" : "Variant Low Stock Alert",
                    Message = isOutOfStock
                        ? $"'{productVariant.Product.Title}', Variant : {productVariant.VariantName} is now OUT OF STOCK and cannot be ordered."
                        : $"'{productVariant.Product.Title}', Variant : {productVariant.VariantName} has only {productVariant.StockQuantity} units left (Alert threshold: {productVariant.MinimumStockAlert})",
                    Type = "StockAlert",
                    RelatedId = productVariant.Id,
                    IsRead = false,
                    Link = string.Empty,
                    Icon =string.Empty,
                    CreatedAt = IdealWeightNutrition.Utility.DateTimeHelper.Now
                };

                _unitOfWork.notification.Add(notification);
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
            }

            return Task.CompletedTask;
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
            <a href=""https://idealweightnutrition.ae/Admin/Product/Upsert/{product.Id}"" class=""btn"">
                Update Stock Now →
            </a>
        </div>

        <div style=""background: #fef3c7; padding: 15px; border-radius: 6px; border-left: 4px solid #f59e0b; margin: 20px 0;"">
            <strong>💡 Pro Tip:</strong> You can adjust the minimum stock alert level for each product in the admin panel to receive notifications earlier.
        </div>
    </div>
    
    <div class=""footer"">
        <p>This is an automated stock alert from Ideal Weight Inventory Management System</p>
        <p style=""margin-top: 10px;"">Generated on {IdealWeightNutrition.Utility.DateTimeHelper.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
        <p style=""color: #9ca3af; font-size: 12px; margin-top: 15px;"">
            To configure stock alert settings, update your appsettings.json file.
        </p>
    </div>
</body>
</html>";
        }
      private string GenerateVariantStockAlertEmailTemplate(ProductVariant productVariant, bool isOutOfStock)
        {
            string statusColor = isOutOfStock ? "#ef4444" : "#f59e0b";
            string statusText = isOutOfStock ? "Variant OUT OF STOCK" : "Variant LOW STOCK";
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
                    ? $"The product <strong>'{productVariant.Product.Title}' , Variant : {productVariant.VariantName} </strong> is now completely OUT OF STOCK and cannot be ordered by customers."
                    : $"The product <strong>'{productVariant.Product.Title}', Variant : {productVariant.VariantName}</strong>  has reached the low stock threshold with only <strong>{productVariant.StockQuantity} units</strong> remaining.")}
            </p>
        </div>

        <div class=""product-info"">
            <h2>📦 Product Details</h2>
            <div class=""info-row"">
                <span class=""label"">Product Name:</span>
                <span class=""value"">{productVariant.Product.Title}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Product ID:</span>
                <span class=""value"">#{productVariant.Id}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Current Stock:</span>
                <span class=""value"" style=""color: {statusColor};"">{productVariant.StockQuantity} units</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Alert Threshold:</span>
                <span class=""value"">{productVariant.MinimumStockAlert} units</span>
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
            <a href=""https://idealweightnutrition.ae/Admin/Product/Upsert/{productVariant.ProductId}"" class=""btn"">
                Update Stock Now →
            </a>
        </div>

        <div style=""background: #fef3c7; padding: 15px; border-radius: 6px; border-left: 4px solid #f59e0b; margin: 20px 0;"">
            <strong>💡 Pro Tip:</strong> You can adjust the minimum stock alert level for each product in the admin panel to receive notifications earlier.
        </div>
    </div>
    
    <div class=""footer"">
        <p>This is an automated stock alert from Ideal Weight Inventory Management System</p>
        <p style=""margin-top: 10px;"">Generated on {IdealWeightNutrition.Utility.DateTimeHelper.Now:MMMM dd, yyyy 'at' hh:mm tt}</p>
        <p style=""color: #9ca3af; font-size: 12px; margin-top: 15px;"">
            To configure stock alert settings, update your appsettings.json file.
        </p>
    </div>
</body>
</html>";
        }

        public async Task ProcessReturnStockRestoration(int returnRequestId)
        {
            try
            {
                // Get return request with items
                var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(returnRequestId);
                
                if (returnRequest == null || returnRequest.ReturnRequestItems == null || !returnRequest.ReturnRequestItems.Any())
                {
                    return;
                }

                foreach (var returnItem in returnRequest.ReturnRequestItems)
                {
                    if (returnItem.OrderDetail == null)
                    {
                        // Load order detail if not already loaded
                        returnItem.OrderDetail = _unitOfWork.OrderDetail.Get(
                            od => od.Id == returnItem.OrderDetailId,
                            includeProperties: "Product,FlashSaleItem,ComboOffer,ProductVariant"
                        );
                    }

                    if (returnItem.OrderDetail == null)
                    {
                        continue; // Skip if order detail not found
                    }

                    var orderDetail = returnItem.OrderDetail;
                    var returnQuantity = returnItem.Quantity;

                    // 🔥 COMBO OFFER RESTORATION: Handle combo offers separately
                    if (orderDetail.IsFromComboOffer && orderDetail.ComboOfferId.HasValue)
                    {
                        await RestoreComboOfferStock(orderDetail.ComboOfferId.Value, returnQuantity);
                        continue; // Skip regular restoration for combo items
                    }

                    // 🔥 FLASH SALE RESTORATION: Restore flash sale quantity first
                    if (orderDetail.IsFromFlashSale && orderDetail.FlashSaleItemId.HasValue)
                    {
                        await RestoreFlashSaleQuantity(orderDetail.FlashSaleItemId.Value, returnQuantity);
                    }

                    // 🔥 VARIANT RESTORATION: Restore variant stock
                    if (orderDetail.ProductVariantId.HasValue && orderDetail.ProductVariant != null)
                    {
                        await RestoreVariantQuantity(orderDetail.ProductVariantId.Value, returnQuantity);
                    }

                    // Restore product stock (regular stock)
                    bool stockIncreased = await IncreaseStock(orderDetail.ProductId, returnQuantity);

                    if (stockIncreased)
                    {
                        // Check if we need to send alerts (stock might have been restored from low/out of stock)
                        await CheckAndNotifyStockLevels(orderDetail.ProductId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - stock restoration failure shouldn't block return completion
                // In production, you might want to log this to a logging service
            }
        }

        // 🔥 NEW METHOD: Restore Combo Offer Stock
        private async Task<bool> RestoreComboOfferStock(int comboOfferId, int comboQuantity)
        {
            try
            {
                // Get combo offer with all items
                var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(comboOfferId);
                
                if (comboOffer == null)
                {
                    return false;
                }

                if (comboOffer.ComboOfferItems == null || !comboOffer.ComboOfferItems.Any())
                {
                    return false;
                }

                // Process each item in the combo
                foreach (var comboItem in comboOffer.ComboOfferItems.Where(i => !i.IsDeleted))
                {
                    // Calculate total quantity to restore: combo item quantity * number of combos returned
                    int totalQuantityToRestore = comboItem.Quantity * comboQuantity;

                    // Handle variant products
                    if (comboItem.ProductVariantId.HasValue && comboItem.ProductVariant != null)
                    {
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == comboItem.ProductVariantId.Value && !v.IsDeleted);
                        if (variant != null)
                        {
                            // Restore variant stock
                            variant.StockQuantity += totalQuantityToRestore;
                            _unitOfWork.ProductVariant.Update(variant);
                            _unitOfWork.save();

                            // Check for stock alerts (might have been restored from out of stock)
                            bool wasOutOfStock = variant.StockQuantity == totalQuantityToRestore;
                            bool isLowStock = variant.StockQuantity > 0 && variant.StockQuantity <= variant.MinimumStockAlert;
                            
                            if (wasOutOfStock || isLowStock)
                            {
                                await SendStockVariantAlertToAdmins(variant, false); // Not out of stock anymore if we just restored
                            }
                        }
                    }
                    else if (comboItem.Product != null)
                    {
                        // Handle regular products
                        var product = _unitOfWork.product.Get(p => p.Id == comboItem.ProductId && !p.IsDeleted);
                        if (product != null)
                        {
                            // Restore product stock
                            bool stockIncreased = await IncreaseStock(product.Id, totalQuantityToRestore);

                            if (stockIncreased)
                            {
                                await CheckAndNotifyStockLevels(product.Id);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // 🔥 NEW METHOD: Restore Flash Sale Quantity
        private async Task<bool> RestoreFlashSaleQuantity(int flashSaleItemId, int quantity)
        {
            try
            {
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(
                    f => f.Id == flashSaleItemId,
                    includeProperties: "Product,FlashSale"
                );

                if (flashSaleItem == null)
                {
                    return false;
                }

                // Restore flash sale quantity
                flashSaleItem.FlashSaleQuantity += quantity;

                _unitOfWork.FlashSaleItem.Update(flashSaleItem);
                _unitOfWork.save();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        // 🔥 NEW METHOD: Restore Variant Quantity
        private async Task<bool> RestoreVariantQuantity(int variantItemId, int quantity)
        {
            try
            {
                var variantItem = _unitOfWork.ProductVariant.Get(
                    v => v.Id == variantItemId,
                    includeProperties: "Product"
                );

                if (variantItem == null)
                {
                    return false;
                }

                // Restore variant stock
                variantItem.StockQuantity += quantity;

                _unitOfWork.ProductVariant.Update(variantItem);
                _unitOfWork.save();

                // Check for stock alerts (might have been restored from out of stock)
                bool wasOutOfStock = variantItem.StockQuantity == quantity;
                bool isLowStock = variantItem.StockQuantity > 0 && variantItem.StockQuantity <= variantItem.MinimumStockAlert;
                
                if (wasOutOfStock || isLowStock)
                {
                    await SendStockVariantAlertToAdmins(variantItem, false); // Not out of stock anymore if we just restored
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    
    
    }
}

