using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Services;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ServiceSubscriptionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public ServiceSubscriptionController(
            IUnitOfWork unitOfWork,
            IStringLocalizer<SharedResources> localizer,
            INotificationService notificationService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        // GET: ServiceSubscription/Index
        public IActionResult Index()
        {
            var services = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive, includeProperties: "ServiceImages")
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.CreatedDate)
                .ToList();

            // Get active offers for each service
            foreach (var service in services)
            {
                var activeOffers = _unitOfWork.ServiceOffers.GetAll(
                    o => o.ServiceSubscriptionId == service.Id && 
                         o.IsActive && 
                         o.StartDate <= BulkyBook.Utility.DateTimeHelper.Now && 
                         o.EndDate >= BulkyBook.Utility.DateTimeHelper.Now
                ).ToList();
                ViewBag.Offers = activeOffers;
            }

            return View(services);
        }

        // GET: ServiceSubscription/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var service = _unitOfWork.ServiceSubscriptions.Get(
                s => s.Id == id,
                includeProperties: "ServiceOffers,ServiceImages"
            );

            if (service == null || !service.IsActive)
            {
                return NotFound();
            }

            // Get active offers
            var activeOffers = _unitOfWork.ServiceOffers.GetAll(
                o => o.ServiceSubscriptionId == service.Id && 
                     o.IsActive && 
                     o.StartDate <= BulkyBook.Utility.DateTimeHelper.Now && 
                     o.EndDate >= BulkyBook.Utility.DateTimeHelper.Now
            ).ToList();

            ViewBag.ActiveOffers = activeOffers;
            ViewBag.PromoCode = _unitOfWork.PromoCode.GetAll().Where(p => p.IsActive).ToList();

            return View(service);
        }

        // POST: ServiceSubscription/Subscribe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int serviceId, int? offerId, string? promoCode, 
            string? guestName, string? guestEmail, string? guestPhone, decimal? customAmount = null)
        {
            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == serviceId);
            
            if (service == null || !service.IsActive)
            {
                TempData["error"] = "Service not found or inactive";
                return RedirectToAction(nameof(Index));
            }

            bool isGuest = !User.Identity.IsAuthenticated;
            string? userId = null;
            ApplicationUser? user = null;

            if (!isGuest)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                user = _unitOfWork.applicationUser.Get(u => u.Id == userId);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(guestEmail))
                {
                    TempData["error"] = "Please provide your name and email";
                    return RedirectToAction(nameof(Details), new { id = serviceId });
                }
            }

            // Calculate price
            decimal totalAmount = service.Price;
            decimal discountAmount = 0;
            ServiceOffer? appliedOffer = null;

            // Apply offer if provided
            if (offerId.HasValue)
            {
                appliedOffer = _unitOfWork.ServiceOffers.Get(o => o.Id == offerId.Value);
                if (appliedOffer != null && appliedOffer.IsActive && 
                    appliedOffer.StartDate <= BulkyBook.Utility.DateTimeHelper.Now && 
                    appliedOffer.EndDate >= BulkyBook.Utility.DateTimeHelper.Now)
                {
                    if (appliedOffer.DiscountType == DiscountType.Percentage)
                    {
                        discountAmount = totalAmount * (appliedOffer.DiscountValue / 100);
                    }
                    else
                    {
                        discountAmount = appliedOffer.DiscountValue;
                    }
                    totalAmount -= discountAmount;
                }
            }

            // Apply promo code if provided
            PromoCode? promo = null;
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                promo = _unitOfWork.PromoCode.GetByCode(promoCode.Trim());
                if (promo != null && promo.IsActive && 
                    promo.StartDate <= BulkyBook.Utility.DateTimeHelper.Now && 
                    promo.EndDate >= BulkyBook.Utility.DateTimeHelper.Now)
                {
                    decimal promoDiscount = 0;
                    if (promo.DiscountType == DiscountType.Percentage)
                    {
                        promoDiscount = totalAmount * (promo.DiscountValue / 100);
                        if (promo.MaximumDiscountAmount.HasValue && promoDiscount > promo.MaximumDiscountAmount.Value)
                        {
                            promoDiscount = promo.MaximumDiscountAmount.Value;
                        }
                    }
                    else
                    {
                        promoDiscount = promo.DiscountValue;
                    }

                    if (promoDiscount > totalAmount)
                    {
                        promoDiscount = totalAmount;
                    }

                    discountAmount += promoDiscount;
                    totalAmount -= promoDiscount;
                }
            }

            // Calculate amount to pay based on service type
            decimal amountToPay = totalAmount;
            decimal minAmountRequired = 0;
            
            if (service.ServiceType == ServiceType.Offline && service.OfflinePaymentPercent.HasValue)
            {
                minAmountRequired = totalAmount * (service.OfflinePaymentPercent.Value / 100);
                
                // Validate custom amount for offline services
                if (!customAmount.HasValue)
                {
                    TempData["error"] = $"Please enter the amount you want to pay. Minimum required: {minAmountRequired:C}";
                    return RedirectToAction(nameof(Details), new { id = serviceId });
                }
                
                if (customAmount.Value < minAmountRequired)
                {
                    TempData["error"] = $"Amount must be at least {minAmountRequired:C} ({service.OfflinePaymentPercent.Value}% of total amount)";
                    return RedirectToAction(nameof(Details), new { id = serviceId });
                }
                
                if (customAmount.Value > totalAmount)
                {
                    TempData["error"] = $"Amount cannot exceed the total amount of {totalAmount:C}";
                    return RedirectToAction(nameof(Details), new { id = serviceId });
                }
                
                amountToPay = customAmount.Value;
            }

            // Create service purchase record
            var purchase = new ServicePurchase
            {
                ServiceSubscriptionId = serviceId,
                ApplicationUserId = userId,
                GuestEmail = isGuest ? guestEmail : null,
                GuestName = isGuest ? guestName : null,
                GuestPhone = isGuest ? guestPhone : null,
                TotalAmount = totalAmount,
                AmountPaid = amountToPay, // Set the amount to pay (custom amount for offline, full amount for online)
                PaymentStatus = "Pending",
                ServiceOfferId = appliedOffer?.Id,
                DiscountAmount = discountAmount,
                PurchaseDate = BulkyBook.Utility.DateTimeHelper.Now,
                Status = "Active"
            };

            _unitOfWork.ServicePurchases.Add(purchase);
            _unitOfWork.save();

            // Create Stripe checkout session
            var domain = $"{Request.Scheme}://{Request.Host}";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(amountToPay * 100), // Convert to cents
                            Currency = "aed",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = service.Title,
                                Description = service.Description
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = domain + $"/Customer/ServiceSubscription/PaymentSuccess?purchaseId={purchase.Id}",
                CancelUrl = domain + $"/Customer/ServiceSubscription/Details/{serviceId}",
                CustomerEmail = isGuest ? guestEmail : user?.Email
            };

            var serviceStripe = new Stripe.Checkout.SessionService();
            Session session = serviceStripe.Create(options);

            // Update purchase with session ID
            purchase.SessionId = session.Id;
            _unitOfWork.ServicePurchases.Update(purchase);
            _unitOfWork.save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: ServiceSubscription/PaymentSuccess
        public async Task<IActionResult> PaymentSuccess(int purchaseId)
        {
            var purchase = _unitOfWork.ServicePurchases.Get(
                p => p.Id == purchaseId,
                includeProperties: "ServiceSubscription,ApplicationUser,ServiceOffer"
            );

            if (purchase == null)
            {
                return NotFound();
            }

            // Verify payment with Stripe
            if (!string.IsNullOrEmpty(purchase.SessionId))
            {
                var serviceStripe = new Stripe.Checkout.SessionService();
                var session = serviceStripe.Get(purchase.SessionId);

                if (session.PaymentStatus == "paid")
                {
                    purchase.PaymentStatus = "Approved";
                    purchase.PaymentIntentId = session.PaymentIntentId;
                    
                    // Get the actual amount paid from Stripe session
                    // The amount_total is in cents, so divide by 100
                    if (session.AmountTotal.HasValue)
                    {
                        purchase.AmountPaid = (decimal)session.AmountTotal.Value / 100;
                    }
                    // If AmountPaid is still 0, use the stored amountToPay value
                    else if (purchase.AmountPaid == 0)
                    {
                        // This shouldn't happen, but as a fallback, calculate from service type
                        if (purchase.ServiceSubscription?.ServiceType == ServiceType.Online)
                        {
                            purchase.AmountPaid = purchase.TotalAmount;
                        }
                        else if (purchase.ServiceSubscription?.ServiceType == ServiceType.Offline && 
                                 purchase.ServiceSubscription.OfflinePaymentPercent.HasValue)
                        {
                            purchase.AmountPaid = purchase.TotalAmount * (purchase.ServiceSubscription.OfflinePaymentPercent.Value / 100);
                        }
                    }

                    _unitOfWork.ServicePurchases.Update(purchase);
                    _unitOfWork.save();

                    // Send notifications to all admins
                    await SendServicePurchaseNotifications(purchase);

                    TempData["success"] = "Payment successful! Your service subscription has been confirmed.";
                }
            }

            return View(purchase);
        }

        private async Task SendServicePurchaseNotifications(ServicePurchase purchase)
        {
            try
            {
                // Get admin email from configuration
                var adminEmail = _configuration["StockAlerts:AdminEmail"];
                
                // Send email notification
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    var emailBody = GenerateAdminEmailTemplate(purchase);
                    var emailSender = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
                    await emailSender.SendEmailAsync(
                        adminEmail,
                        $"New Service Subscription Purchase #{purchase.Id} - Ideal Weight",
                        emailBody
                    );
                }

                // Send push notifications to all admins
                var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                var adminUsers = await userManager.GetUsersInRoleAsync(SD.Role_Admin);
                
                foreach (var admin in adminUsers)
                {
                    await _notificationService.LogNotification(
                        admin.Id,
                        "New Service Subscription",
                        $"New service subscription #{purchase.Id} - {purchase.ServiceSubscription?.Title}. Amount: {purchase.AmountPaid:C}",
                        "ServiceSubscription",
                        purchase.Id
                    );
                }

                // Send real-time notification via SignalR
                var hubContext = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<BulkyBook.Hubs.NotificationHub>>();
                await hubContext.Clients.Group("Admins").SendAsync(
                    "ReceiveServiceSubscriptionNotification",
                    new
                    {
                        title = "New Service Subscription",
                        message = $"Service subscription #{purchase.Id} - {purchase.ServiceSubscription?.Title}",
                        purchaseId = purchase.Id,
                        amount = purchase.AmountPaid,
                        timestamp = BulkyBook.Utility.DateTimeHelper.Now
                    }
                );
            }
            catch (Exception ex)
            {
                // Log error but don't fail the purchase
                // In production, use proper logging
                Console.WriteLine($"Error sending notifications: {ex.Message}");
            }
        }

        private string GenerateAdminEmailTemplate(ServicePurchase purchase)
        {
            var customerInfo = purchase.ApplicationUser != null
                ? $"{purchase.ApplicationUser.Name}<br/>Email: {purchase.ApplicationUser.Email}<br/>Phone: {purchase.ApplicationUser.PhoneNumber}"
                : $"{purchase.GuestName}<br/>Email: {purchase.GuestEmail}<br/>Phone: {purchase.GuestPhone ?? "N/A"}";

            var serviceTypeText = purchase.ServiceSubscription?.ServiceType == ServiceType.Online 
                ? "Online (Full Payment)" 
                : $"Offline (Partial Payment - {purchase.ServiceSubscription?.OfflinePaymentPercent}%)";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; background-color: #f9fafb; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px; font-weight: 800;'>🎉 New Service Subscription!</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Purchase #{purchase.Id}</p>
        </div>
        <div style='padding: 30px;'>
            <div style='background: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px;'>Customer Information</h2>
                <div style='color: #1f2937;'>{customerInfo}</div>
            </div>
            <div style='background: #f9fafb; border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px;'>Service Details</h2>
                <p style='margin: 5px 0; color: #1f2937;'><strong>Service:</strong> {purchase.ServiceSubscription?.Title}</p>
                <p style='margin: 5px 0; color: #1f2937;'><strong>Type:</strong> {serviceTypeText}</p>
                <p style='margin: 5px 0; color: #1f2937;'><strong>Total Amount:</strong> {purchase.TotalAmount:C}</p>
                <p style='margin: 5px 0; color: #1f2937;'><strong>Amount Paid:</strong> {purchase.AmountPaid:C}</p>
                <p style='margin: 5px 0; color: #1f2937;'><strong>Purchase Date:</strong> {purchase.PurchaseDate:MMM dd, yyyy hh:mm tt}</p>
            </div>
            <div style='text-align: center; margin-top: 30px;'>
                <a href='{GetBaseUrl()}/Admin/ServiceSubscription/Details/{purchase.ServiceSubscriptionId}' style='display: inline-block; background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); color: #ffffff; text-decoration: none; padding: 15px 40px; border-radius: 8px; font-weight: 700; font-size: 16px;'>
                    View Service Details
                </a>
            </div>
        </div>
        <div style='background: #f9fafb; padding: 20px; text-align: center; border-top: 1px solid #e5e7eb;'>
            <p style='color: #6b7280; margin: 0; font-size: 14px;'>© 2025 Ideal Weight. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetBaseUrl()
        {
            return _configuration["SiteSettings:BaseUrl"]??string.Empty;
        }
    }
}

