using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using IdealWeightNutrition.Services;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ServiceSubscriptionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly GeideaSettings _geideaSettings;
        private readonly TamaraSettings _tamaraSettings;
        private readonly TappySettings _tappySettings;
        private readonly ILogger<ServiceSubscriptionController> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _memoryCache;
        private readonly InvoiceService _invoiceService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceSubscriptionController(
            IUnitOfWork unitOfWork,
            IStringLocalizer<SharedResources> localizer,
            INotificationService notificationService,
            IConfiguration configuration,
            IOptions<GeideaSettings> geideaSettings,
            IOptions<TamaraSettings> tamaraSettings,
            IOptions<TappySettings> tappySettings,
            ILogger<ServiceSubscriptionController> logger,
            ILoggerFactory loggerFactory,
            IEmailSender emailSender,
            IMemoryCache memoryCache,
            InvoiceService invoiceService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _notificationService = notificationService;
            _configuration = configuration;
            _geideaSettings = geideaSettings.Value;
            _tamaraSettings = tamaraSettings.Value;
            _tappySettings = tappySettings.Value;
            _logger = logger;
            _loggerFactory = loggerFactory;
            _emailSender = emailSender;
            _memoryCache = memoryCache;
            _invoiceService = invoiceService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // GET: ServiceSubscription/Index
        public IActionResult Index()
        {
            var services = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive, includeProperties: "ServiceImages,ServiceOffers")
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.CreatedDate)
                .ToList();

            // Get all active offers for all services at once
            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            var serviceIds = services.Select(s => s.Id).ToList();
            var allActiveOffers = _unitOfWork.ServiceOffers.GetAll(
                o => serviceIds.Contains(o.ServiceSubscriptionId) && 
                     o.IsActive && 
                     o.StartDate <= now && 
                     o.EndDate >= now
            ).ToList();

            ViewBag.Offers = allActiveOffers;

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

            // Set ViewData title based on current language
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            ViewData["Title"] = currentCulture == "ar" && !string.IsNullOrEmpty(service.TitleAr) 
                ? service.TitleAr 
                : service.Title;

            // Get active offers
            var activeOffers = _unitOfWork.ServiceOffers.GetAll(
                o => o.ServiceSubscriptionId == service.Id && 
                     o.IsActive && 
                     o.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now && 
                     o.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now
            ).ToList();

            ViewBag.ActiveOffers = activeOffers;
            ViewBag.PromoCode = _unitOfWork.PromoCode.GetAll().Where(p => p.IsActive).ToList();

            return View(service);
        }

        // GET: ServiceSubscription/ServiceSummary
        public async Task<IActionResult> ServiceSummary(int? id, int? offerId, string? promoCode)
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

            bool isGuest = !User.Identity.IsAuthenticated;
            string? userId = null;
            ApplicationUser? user = null;

            if (!isGuest)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                user = _unitOfWork.applicationUser.Get(u => u.Id == userId);
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
                    appliedOffer.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now &&
                    appliedOffer.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
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
                    promo.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now &&
                    promo.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
                {
                    // Check if service is excluded from this promo code
                    if (IsServiceExcludedFromPromoCode(promo, service.Id))
                    {
                        // Service is excluded, don't apply promo code
                        promo = null;
                    }
                    else
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
            }

            // Calculate minimum amount for offline services
            decimal minAmountRequired = 0;
            if (service.ServiceType == ServiceType.Offline && service.OfflinePaymentPercent.HasValue)
            {
                minAmountRequired = totalAmount * (service.OfflinePaymentPercent.Value / 100);
            }

            // Create view model
            var viewModel = new ServiceSummaryVM
            {
                Service = service,
                ServiceId = service.Id,
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                AppliedOffer = appliedOffer,
                AppliedPromoCode = promo,
                MinAmountRequired = minAmountRequired,
                IsGuest = isGuest,
                User = user,
                OfferId = offerId,
                PromoCode = promoCode
            };

            // Check if Tamara payment is available
            bool tamaraAvailable = false;
            if (_tamaraSettings.Enabled && totalAmount > 0)
            {
                try
                {
                    var tamaraLogger = _loggerFactory?.CreateLogger<TamaraHelper>();
                    var tamaraHelper = new TamaraHelper(_tamaraSettings, tamaraLogger);
                    tamaraAvailable = await tamaraHelper.IsPaymentAvailableAsync(totalAmount, _tamaraSettings.CountryCode ?? "AE");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking Tamara payment availability for service");
                    tamaraAvailable = false;
                }
            }

            ViewBag.TamaraAvailable = tamaraAvailable;
            ViewBag.TamaraPublicKey = _tamaraSettings.PublicKey;
            ViewBag.TamaraOrderTotal = totalAmount;
            ViewBag.TabbyAvailable = _tappySettings.Enabled && totalAmount > 0;
            ViewBag.OfferId = offerId;
            ViewBag.PromoCode = promoCode;

            return View(viewModel);
        }

        // POST: ServiceSubscription/ServiceSummary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServiceSummary(ServiceSummaryVM model)
        {
            _logger.LogInformation($"ServiceSummary POST called - ServiceId: {model.ServiceId}, PaymentMethod: {model.PaymentMethod}, OfferId: {model.OfferId}, PromoCode: {model.PromoCode}, CustomAmount: {model.CustomAmount}");
            
            if (model.ServiceId == 0)
            {
                _logger.LogWarning("ServiceSummary POST - ServiceId is 0");
                TempData["error"] = "Service not found";
                return RedirectToAction(nameof(Index));
            }

            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == model.ServiceId);

            if (service == null || !service.IsActive)
            {
                _logger.LogWarning($"ServiceSummary POST - Service {model.ServiceId} not found or inactive");
                TempData["error"] = "Service not found or inactive";
                return RedirectToAction(nameof(Index));
            }
            
            _logger.LogInformation($"ServiceSummary POST - Service found: {service.Title}, Price: {service.Price}, ServiceType: {service.ServiceType}");

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
                if (string.IsNullOrWhiteSpace(model.GuestName) || string.IsNullOrWhiteSpace(model.GuestEmail))
                {
                    TempData["error"] = "Please provide your name and email";
                    return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                }
            }

            // Recalculate price (same logic as GET)
            decimal totalAmount = service.Price;
            decimal discountAmount = 0;
            ServiceOffer? appliedOffer = null;

            if (model.OfferId.HasValue)
            {
                appliedOffer = _unitOfWork.ServiceOffers.Get(o => o.Id == model.OfferId.Value);
                if (appliedOffer != null && appliedOffer.IsActive &&
                    appliedOffer.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now &&
                    appliedOffer.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
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

            PromoCode? promo = null;
            if (!string.IsNullOrWhiteSpace(model.PromoCode))
            {
                promo = _unitOfWork.PromoCode.GetByCode(model.PromoCode.Trim());
                if (promo != null && promo.IsActive &&
                    promo.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now &&
                    promo.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
                {
                    // Check if service is excluded from this promo code
                    if (IsServiceExcludedFromPromoCode(promo, service.Id))
                    {
                        // Service is excluded, don't apply promo code
                        promo = null;
                    }
                    else
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
            }

            // Check if total amount is 0 (100% discount) - allow free subscription
            bool isFreeSubscription = totalAmount <= 0.01m; // Allow for rounding differences
            
            // Calculate amount to pay
            decimal amountToPay = totalAmount;
            if (!isFreeSubscription && service.ServiceType == ServiceType.Offline && service.OfflinePaymentPercent.HasValue)
            {
                decimal minAmountRequired = totalAmount * (service.OfflinePaymentPercent.Value / 100);
                
                if (model.CustomAmount.HasValue)
                {
                    if (model.CustomAmount.Value < minAmountRequired)
                    {
                        TempData["error"] = $"Amount must be at least {minAmountRequired:C} ({service.OfflinePaymentPercent.Value}% of total amount)";
                        return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                    }
                    
                    if (model.CustomAmount.Value > totalAmount)
                    {
                        TempData["error"] = $"Amount cannot exceed the total amount of {totalAmount:C}";
                        return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                    }
                    
                    amountToPay = model.CustomAmount.Value;
                }
                else
                {
                    amountToPay = minAmountRequired;
                }
            }
            else if (isFreeSubscription)
            {
                amountToPay = 0;
            }

            // Validate payment method only if amount is greater than 0
            if (!isFreeSubscription && string.IsNullOrWhiteSpace(model.PaymentMethod))
            {
                TempData["error"] = "Please select a payment method";
                return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
            }

            // Create service purchase record
            var purchase = new ServicePurchase
            {
                ServiceSubscriptionId = model.ServiceId,
                ApplicationUserId = userId,
                GuestEmail = isGuest ? model.GuestEmail : null,
                GuestName = isGuest ? model.GuestName : null,
                GuestPhone = isGuest ? model.GuestPhone : null,
                TotalAmount = totalAmount,
                AmountPaid = amountToPay,
                PaymentStatus = isFreeSubscription ? "Approved" : "Pending", // Auto-approve free subscriptions
                ServiceOfferId = appliedOffer?.Id,
                DiscountAmount = discountAmount,
                PurchaseDate = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                Status = "Active",
                PaymentIntentId = isFreeSubscription ? "FREE" : model.PaymentMethod // Store payment method temporarily
            };

            _unitOfWork.ServicePurchases.Add(purchase);
            _unitOfWork.save();

            _logger.LogInformation($"ServicePurchase created - Id: {purchase.Id}, ServiceId: {purchase.ServiceSubscriptionId}, TotalAmount: {purchase.TotalAmount}, AmountPaid: {purchase.AmountPaid}, IsFree: {isFreeSubscription}");

            // If free subscription (100% discount), skip payment and redirect to success
            if (isFreeSubscription)
            {
                _logger.LogInformation($"Free subscription detected - TotalAmount: {totalAmount}, AmountPaid: {amountToPay}. Skipping payment processing.");
                
                // Send confirmation email asynchronously
                _ = Task.Run(async () =>
                {
                    await SendServicePurchaseConfirmationEmail(purchase);
                });
                
                // Check if this is an AJAX request
                bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                
                if (isAjaxRequest)
                {
                    // Return JSON response for AJAX requests
                    return Json(new 
                    { 
                        success = true, 
                        paymentMethod = "FREE",
                        purchaseId = purchase.Id,
                        redirectUrl = Url.Action(nameof(PaymentSuccess), new { purchaseId = purchase.Id })
                    });
                }
                else
                {
                    // Regular redirect for non-AJAX requests
                    TempData["success"] = _localizer["ServiceSubscriptionConfirmed"]?.Value ?? "Your service subscription has been confirmed!";
                    return RedirectToAction(nameof(PaymentSuccess), new { purchaseId = purchase.Id });
                }
            }

            // Process payment based on method
            var domain = $"{Request.Scheme}://{Request.Host}";
            _logger.LogInformation($"ServiceSummary POST - Domain: {domain}, PaymentMethod: {model.PaymentMethod ?? "Geidea (default)"}, AmountToPay: {amountToPay}, TotalAmount: {totalAmount}");
            
            // Determine base URL for callbacks - use CallbackUrlOverride if configured, otherwise use domain
            string baseUrl = domain;
            if (!string.IsNullOrEmpty(_geideaSettings.CallbackUrlOverride))
            {
                baseUrl = _geideaSettings.CallbackUrlOverride.TrimEnd('/');
                _logger.LogInformation($"Using Geidea CallbackUrlOverride: {baseUrl} (original domain was: {domain})");
            }
            else if (domain.Contains("localhost") || domain.Contains("127.0.0.1"))
            {
                var siteBaseUrl = _configuration["SiteSettings:BaseUrl"];
                if (!string.IsNullOrEmpty(siteBaseUrl))
                {
                    baseUrl = siteBaseUrl.TrimEnd('/');
                    _logger.LogInformation($"Using SiteSettings:BaseUrl: {baseUrl} (original domain was: {domain})");
                }
            }
            
            if (model.PaymentMethod == "Geidea" || string.IsNullOrEmpty(model.PaymentMethod))
            {
                // Geidea payment
                _logger.LogInformation($"Creating Geidea payment for service purchase {purchase.Id}. Amount: {amountToPay} AED");
                
                var geideaHelper = new GeideaHelper(_geideaSettings);
                
                var geideaItems = new List<GeideaOrderItem>
                {
                    new GeideaOrderItem
                    {
                        Name = service.Title,
                        Description = service.Description ?? service.Title,
                        Quantity = 1,
                        Price = amountToPay,
                        Sku = service.Id.ToString()
                    }
                };

                var returnUrl = baseUrl + $"/customer/servicesubscription/paymentsuccess?purchaseId={purchase.Id}";
                var cancelUrl = baseUrl + $"/customer/servicesubscription/servicesummary?id={model.ServiceId}";
                
                _logger.LogInformation($"Geidea URLs - BaseUrl: {baseUrl}, ReturnUrl: {returnUrl}, CancelUrl: {cancelUrl}");
                
                // Validate callback URL for Geidea
                if ((returnUrl.Contains("localhost") || returnUrl.Contains("127.0.0.1") || !returnUrl.StartsWith("https://")) 
                    && string.IsNullOrEmpty(_geideaSettings.CallbackUrlOverride))
                {
                    _logger.LogError($"Geidea callback URL validation failed - URL: {returnUrl}");
                    TempData["error"] = "Geidea requires a public HTTPS callback URL. " +
                                        "For local testing, please configure 'CallbackUrlOverride' in appsettings.json with your ngrok URL or public domain.";
                    return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                }
                
                var geideaRequest = new GeideaPaymentRequest
                {
                    Amount = amountToPay,
                    Currency = "AED",
                    OrderId = purchase.Id.ToString(),
                    CustomerName = isGuest ? model.GuestName : user?.Name ?? "Customer",
                    CustomerEmail = isGuest ? model.GuestEmail : user?.Email ?? "",
                    CustomerPhone = isGuest ? model.GuestPhone : user?.PhoneNumber ?? "",
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    Items = geideaItems
                };

                _logger.LogInformation($"Geidea Request - OrderId: {geideaRequest.OrderId}, Amount: {geideaRequest.Amount}, Currency: {geideaRequest.Currency}, ReturnUrl: {returnUrl}, CancelUrl: {cancelUrl}, CustomerEmail: {geideaRequest.CustomerEmail}");

                try
                {
                    var geideaResponse = await geideaHelper.CreatePaymentAsync(geideaRequest);
                    
                    _logger.LogInformation($"Geidea Response - Success: {geideaResponse.Success}, Message: {geideaResponse.Message}, TransactionId: {geideaResponse.TransactionId}, PaymentUrl: {geideaResponse.PaymentUrl}");
                    
                    if (!geideaResponse.Success)
                    {
                        _logger.LogError($"Geidea payment creation failed for purchase {purchase.Id}. Error: {geideaResponse.Message}");
                        
                        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                        if (isAjax)
                        {
                            return Json(new 
                            { 
                                success = false, 
                                error = "Failed to create payment: " + geideaResponse.Message
                            });
                        }
                        
                        TempData["error"] = "Failed to create payment: " + geideaResponse.Message;
                        return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                    }

                    purchase.SessionId = geideaResponse.TransactionId;
                    purchase.PaymentIntentId = geideaResponse.TransactionId;
                    _unitOfWork.ServicePurchases.Update(purchase);
                    _unitOfWork.save();

                    _logger.LogInformation($"Geidea payment created successfully for purchase {purchase.Id}. SessionId: {geideaResponse.TransactionId}");
                    
                    bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                    if (isAjaxRequest)
                    {
                        // Return JSON with sessionId for Geidea v2 HPP JavaScript integration
                        return Json(new 
                        { 
                            success = true, 
                            paymentMethod = "Geidea",
                            sessionId = geideaResponse.TransactionId, // This is the sessionId for v2 HPP
                            purchaseId = purchase.Id, // Purchase ID for redirect after payment
                            redirectUrl = geideaResponse.PaymentUrl // Keep for fallback if needed
                        });
                    }
                    else
                    {
                        // Fallback: redirect to payment URL for non-AJAX requests
                        Response.Headers.Add("Location", geideaResponse.PaymentUrl);
                        return new StatusCodeResult(303);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception creating Geidea payment for purchase {purchase.Id}");
                    
                    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                    if (isAjax)
                    {
                        return Json(new 
                        { 
                            success = false, 
                            error = "An error occurred while creating payment. Please try again."
                        });
                    }
                    
                    TempData["error"] = "An error occurred while creating payment. Please try again.";
                    return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                }
            }
            else if (model.PaymentMethod == "Tamara")
            {
                // Tamara payment
                _logger.LogInformation($"Creating Tamara payment for service purchase {purchase.Id}. Amount: {amountToPay} AED");
                
                var tamaraLogger = _loggerFactory?.CreateLogger<TamaraHelper>();
                var tamaraHelper = new TamaraHelper(_tamaraSettings, tamaraLogger);
                
                // Split name into first and last name
                string firstName = "Customer";
                string lastName = "Customer";
                if (!isGuest && user != null && !string.IsNullOrEmpty(user.Name))
                {
                    var nameParts = user.Name.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    firstName = nameParts[0];
                    lastName = nameParts.Length > 1 ? nameParts[1] : firstName; // Use first name as last name if not provided
                }
                else if (isGuest && !string.IsNullOrEmpty(model.GuestName))
                {
                    var nameParts = model.GuestName.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    firstName = nameParts[0];
                    lastName = nameParts.Length > 1 ? nameParts[1] : firstName; // Use first name as last name if not provided
                }
                
                // Ensure lastName is not empty (Tamara requirement)
                if (string.IsNullOrWhiteSpace(lastName))
                {
                    lastName = firstName;
                }
                
                // Format phone number for Tamara (NO + prefix, just digits after country code)
                string phoneNumber = isGuest ? model.GuestPhone : user?.PhoneNumber ?? "";
                string originalPhone = phoneNumber;
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    phoneNumber = phoneNumber.Trim().Replace("+", "").Replace("-", "").Replace(" ", "");
                    
                    // Remove country code if present - Tamara wants local format
                    if (phoneNumber.StartsWith("971"))
                    {
                        phoneNumber = phoneNumber.Substring(3); // Remove 971 prefix
                    }
                    // Remove leading 0 if present
                    else if (phoneNumber.StartsWith("0"))
                    {
                        phoneNumber = phoneNumber.Substring(1);
                    }
                }
                else
                {
                    phoneNumber = "500000001"; // Default phone number if not provided
                }
                
                var customerEmail = isGuest ? model.GuestEmail : user?.Email ?? "";
                _logger.LogInformation($"Tamara Customer Info - FirstName: {firstName}, LastName: {lastName}, Email: {customerEmail}, Phone (original): {originalPhone}, Phone (formatted): {phoneNumber}");
                
                var tamaraRequest = new TamaraPaymentRequest
                {
                    OrderReferenceId = purchase.Id.ToString(),
                    OrderNumber = $"SRV-{purchase.Id}",
                    TotalAmount = new TamaraAmount
                    {
                        Amount = amountToPay,
                        Currency = "AED"
                    },
                    Description = $"Service Subscription: {service.Title}",
                    CountryCode = _tamaraSettings.CountryCode ?? "AE",
                    PaymentType = "PAY_BY_INSTALMENTS",
                    Instalments = null, // Let Tamara decide based on amount
                    Locale = "en_US",
                    Platform = "ASP.NET Core MVC",
                    IsMobile = false,
                    Consumer = new TamaraConsumer
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Email = isGuest ? model.GuestEmail : user?.Email ?? "",
                        PhoneNumber = phoneNumber
                    },
                    BillingAddress = new TamaraAddress
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Line1 = "Service Subscription", // Services don't have physical addresses
                        Line2 = null,
                        City = "Dubai",
                        Region = "Dubai",
                        PostalCode = "00000",
                        CountryCode = _tamaraSettings.CountryCode ?? "AE",
                        PhoneNumber = phoneNumber
                    },
                    ShippingAddress = new TamaraAddress
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        Line1 = "Service Subscription", // Services don't have physical addresses
                        Line2 = null,
                        City = "Dubai",
                        Region = "Dubai",
                        PostalCode = "00000",
                        CountryCode = _tamaraSettings.CountryCode ?? "AE",
                        PhoneNumber = phoneNumber
                    },
                    Items = new List<TamaraItem>
                    {
                        new TamaraItem
                        {
                            ReferenceId = service.Id.ToString(),
                            Type = "Digital",
                            Name = service.Title?.Length > 200 ? service.Title.Substring(0, 200) : service.Title,
                            Sku = service.Id.ToString(),
                            Quantity = 1,
                            UnitPrice = new TamaraAmount
                            {
                                Amount = amountToPay,
                                Currency = "AED"
                            },
                            TotalAmount = new TamaraAmount
                            {
                                Amount = amountToPay,
                                Currency = "AED"
                            },
                            DiscountAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = "AED"
                            },
                            TaxAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = "AED"
                            }
                        }
                    },
                    TaxAmount = new TamaraAmount
                    {
                        Amount = 0,
                        Currency = "AED"
                    },
                    ShippingAmount = new TamaraAmount
                    {
                        Amount = 0,
                        Currency = "AED"
                    },
                    MerchantUrl = new TamaraMerchantUrl
                    {
                        Success = baseUrl + $"/customer/servicesubscription/paymentsuccess?purchaseId={purchase.Id}",
                        Failure = baseUrl + $"/customer/servicesubscription/servicesummary?id={model.ServiceId}",
                        Cancel = baseUrl + $"/customer/servicesubscription/servicesummary?id={model.ServiceId}",
                        Notification = "" // Empty notification URL as required for checkout creation
                    }
                };

                _logger.LogInformation($"Tamara Request Details - OrderReferenceId: {tamaraRequest.OrderReferenceId}, OrderNumber: {tamaraRequest.OrderNumber}, TotalAmount: {tamaraRequest.TotalAmount.Amount} {tamaraRequest.TotalAmount.Currency}, CountryCode: {tamaraRequest.CountryCode}, PaymentType: {tamaraRequest.PaymentType}, Items Count: {tamaraRequest.Items?.Count ?? 0}");
                _logger.LogInformation($"Tamara Merchant URLs - Success: {tamaraRequest.MerchantUrl.Success}, Failure: {tamaraRequest.MerchantUrl.Failure}, Cancel: {tamaraRequest.MerchantUrl.Cancel}, Notification: '{tamaraRequest.MerchantUrl.Notification}'");
                
                if (tamaraRequest.Items != null && tamaraRequest.Items.Any())
                {
                    var firstItem = tamaraRequest.Items.First();
                    _logger.LogInformation($"Tamara First Item - ReferenceId: {firstItem.ReferenceId}, Type: {firstItem.Type}, Name: {firstItem.Name}, Quantity: {firstItem.Quantity}, UnitPrice: {firstItem.UnitPrice?.Amount}, TotalAmount: {firstItem.TotalAmount?.Amount}");
                }

                try
                {
                    var tamaraResponse = await tamaraHelper.CreateCheckoutAsync(tamaraRequest);
                    
                    _logger.LogInformation($"Tamara Response - Success: {tamaraResponse.Success}, Message: {tamaraResponse.Message}, OrderId: {tamaraResponse.OrderId}, CheckoutId: {tamaraResponse.CheckoutId}, CheckoutUrl: {tamaraResponse.CheckoutUrl}, Status: {tamaraResponse.Status}");
                    
                    if (!tamaraResponse.Success)
                    {
                        _logger.LogError($"Tamara payment creation failed for purchase {purchase.Id}. Error: {tamaraResponse.Message}");
                        TempData["error"] = "Failed to create Tamara payment: " + tamaraResponse.Message;
                        return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                    }

                    purchase.SessionId = tamaraResponse.OrderId;
                    purchase.PaymentIntentId = tamaraResponse.OrderId;
                    _unitOfWork.ServicePurchases.Update(purchase);
                    _unitOfWork.save();

                    _logger.LogInformation($"Tamara payment created successfully for purchase {purchase.Id}. Redirecting to: {tamaraResponse.CheckoutUrl}");
                    Response.Headers.Add("Location", tamaraResponse.CheckoutUrl);
                    return new StatusCodeResult(303);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception creating Tamara payment for purchase {purchase.Id}. Exception details: {ex.Message}, StackTrace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner Exception: {ex.InnerException.Message}, StackTrace: {ex.InnerException.StackTrace}");
                    }
                    TempData["error"] = "An error occurred while creating payment. Please try again.";
                    return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                }
            }
            else if (model.PaymentMethod == "Tabby" || model.PaymentMethod == SD.PaymentMethodTappy)
            {
                // Tabby payment logic
                if (_tappySettings.Enabled)
                {
                    _logger.LogInformation($"Creating Tabby payment for service purchase {purchase.Id}. Amount: {amountToPay} AED");
                    
                    var tappyHelper = new TappyHelper(_tappySettings);
                    
                    // Get service image URL if available
                    string serviceImageUrl = "";
                    if (service.ServiceImages != null && service.ServiceImages.Any())
                    {
                        var firstImage = service.ServiceImages.First();
                        serviceImageUrl = firstImage.ImageUrl ?? "";
                        if (!string.IsNullOrEmpty(serviceImageUrl) && !serviceImageUrl.StartsWith("http"))
                        {
                            serviceImageUrl = domain.TrimEnd('/') + serviceImageUrl;
                        }
                    }
                    
                    var serviceUrl = domain + $"/customer/servicesubscription/details?id={service.Id}";
                    
                    var tabbyItems = new List<TabbyOrderItem>
                    {
                        new TabbyOrderItem
                        {
                            ReferenceId = service.Id.ToString(),
                            Title = service.Title ?? "Service Subscription",
                            Description = service.Description?.Length > 500 ? service.Description.Substring(0, 500) : service.Description,
                            Quantity = 1,
                            UnitPrice = amountToPay,
                            DiscountAmount = discountAmount > 0 ? (decimal?)discountAmount : null,
                            ImageUrl = !string.IsNullOrEmpty(serviceImageUrl) ? serviceImageUrl : null,
                            ProductUrl = serviceUrl,
                            Category = "Service"
                        }
                    };
                    
                    var tappyRequest = new TappyPaymentRequest
                    {
                        MerchantId = _tappySettings.MerchantId,
                        Amount = amountToPay,
                        Currency = "AED",
                        OrderId = purchase.Id.ToString(),
                        CustomerName = isGuest ? model.GuestName : user?.Name ?? "Customer",
                        CustomerEmail = isGuest ? model.GuestEmail : user?.Email ?? "",
                        CustomerPhone = isGuest ? model.GuestPhone : user?.PhoneNumber ?? "",
                        ReturnUrl = baseUrl + $"/customer/servicesubscription/paymentsuccess?purchaseId={purchase.Id}",
                        CancelUrl = baseUrl + $"/customer/servicesubscription/servicesummary?id={model.ServiceId}",
                        Description = $"Service Subscription: {service.Title}",
                        ShippingCity = "Dubai",
                        ShippingAddress = "Service Subscription",
                        ShippingPostalCode = "00000",
                        TaxAmount = 0,
                        ShippingAmount = 0,
                        DiscountAmount = discountAmount > 0 ? (decimal?)discountAmount : null,
                        Language = "en",
                        Items = tabbyItems
                    };

                    _logger.LogInformation($"Tabby Request - OrderId: {tappyRequest.OrderId}, Amount: {tappyRequest.Amount}, Currency: {tappyRequest.Currency}, CustomerEmail: {tappyRequest.CustomerEmail}, ReturnUrl: {tappyRequest.ReturnUrl}");

                    try
                    {
                        var tappyResponse = await tappyHelper.CreatePaymentAsync(tappyRequest);
                        
                        _logger.LogInformation($"Tabby Response - Success: {tappyResponse.Success}, Message: {tappyResponse.Message}, TransactionId: {tappyResponse.TransactionId}, PaymentUrl: {tappyResponse.PaymentUrl}");
                        
                        if (!tappyResponse.Success)
                        {
                            _logger.LogError($"Tabby payment creation failed for purchase {purchase.Id}. Error: {tappyResponse.Message}");
                            
                            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                            if (isAjax)
                            {
                                return Json(new 
                                { 
                                    success = false, 
                                    error = "Failed to create Tabby payment: " + tappyResponse.Message
                                });
                            }
                            
                            TempData["error"] = "Failed to create Tabby payment: " + tappyResponse.Message;
                            return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                        }

                        purchase.SessionId = tappyResponse.TransactionId;
                        purchase.PaymentIntentId = tappyResponse.TransactionId;
                        _unitOfWork.ServicePurchases.Update(purchase);
                        _unitOfWork.save();

                        _logger.LogInformation($"Tabby payment created successfully for purchase {purchase.Id}. Redirecting to: {tappyResponse.PaymentUrl}");
                        
                        bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                        if (isAjaxRequest)
                        {
                            // Return JSON with redirect URL for Tabby (Tabby redirects, doesn't use modal)
                            return Json(new 
                            { 
                                success = true, 
                                paymentMethod = "Tabby",
                                redirectUrl = tappyResponse.PaymentUrl
                            });
                        }
                        else
                        {
                            // Fallback: redirect to payment URL for non-AJAX requests
                            Response.Headers.Add("Location", tappyResponse.PaymentUrl);
                            return new StatusCodeResult(303);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Exception creating Tabby payment for purchase {purchase.Id}");
                        
                        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                        if (isAjax)
                        {
                            return Json(new 
                            { 
                                success = false, 
                                error = "An error occurred while creating Tabby payment. Please try again."
                            });
                        }
                        
                        TempData["error"] = "An error occurred while creating Tabby payment. Please try again.";
                        return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                    }
                }
                else
                {
                    bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
                    if (isAjax)
                    {
                        return Json(new 
                        { 
                            success = false, 
                            error = "Tabby payment is currently unavailable"
                        });
                    }
                    
                    TempData["error"] = "Tabby payment is currently unavailable";
                    return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
                }
            }
            else
            {
                TempData["error"] = "Invalid payment method";
                return RedirectToAction(nameof(ServiceSummary), new { id = model.ServiceId, offerId = model.OfferId, promoCode = model.PromoCode });
            }
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
                    appliedOffer.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now && 
                    appliedOffer.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
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
                    promo.StartDate <= IdealWeightNutrition.Utility.DateTimeHelper.Now && 
                    promo.EndDate >= IdealWeightNutrition.Utility.DateTimeHelper.Now)
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
                PurchaseDate = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                Status = "Active"
            };

            _unitOfWork.ServicePurchases.Add(purchase);
            _unitOfWork.save();

            // Create Geidea payment session
            var domain = $"{Request.Scheme}://{Request.Host}";
            var geideaHelper = new GeideaHelper(_geideaSettings);
            
            var geideaItems = new List<GeideaOrderItem>
            {
                new GeideaOrderItem
                {
                    Name = service.Title,
                    Description = service.Description ?? service.Title,
                    Quantity = 1,
                    Price = amountToPay,
                    Sku = service.Id.ToString()
                }
            };

            var geideaRequest = new GeideaPaymentRequest
            {
                Amount = amountToPay,
                Currency = "AED",
                OrderId = purchase.Id.ToString(),
                CustomerName = isGuest ? guestName : user?.Name ?? "Customer",
                CustomerEmail = isGuest ? guestEmail : user?.Email ?? "",
                CustomerPhone = isGuest ? guestPhone : user?.PhoneNumber ?? "",
                ReturnUrl = domain + $"/customer/servicesubscription/paymentsuccess?purchaseId={purchase.Id}",
                CancelUrl = domain + $"/customer/servicesubscription/details/{serviceId}",
                Items = geideaItems
            };

            var geideaResponse = await geideaHelper.CreatePaymentAsync(geideaRequest);
            
            if (!geideaResponse.Success)
            {
                TempData["error"] = "Failed to create Geidea payment: " + geideaResponse.Message;
                return RedirectToAction(nameof(Details), new { id = serviceId });
            }

            // Update purchase with session ID
            purchase.SessionId = geideaResponse.TransactionId;
            purchase.PaymentIntentId = geideaResponse.TransactionId;
            _unitOfWork.ServicePurchases.Update(purchase);
            _unitOfWork.save();

            Response.Headers.Add("Location", geideaResponse.PaymentUrl);
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

			// Verify payment with Geidea
			if (!string.IsNullOrEmpty(purchase.SessionId))
			{
				var geideaHelper = new GeideaHelper(_geideaSettings);
				// Use purchase ID (merchant reference ID) for verification, not session ID
				var verificationResponse = await geideaHelper.VerifyPaymentAsync(purchase.Id.ToString());

                if (verificationResponse.Success && verificationResponse.IsPaid)
                {
                    purchase.PaymentStatus = "Approved";
                    purchase.PaymentIntentId = purchase.SessionId;
                    
                    // AmountPaid is already set when creating the purchase
                    // If it's still 0, use the stored amountToPay value as fallback
                    if (purchase.AmountPaid == 0)
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
                    _ = Task.Run(async () =>
                    {
                        // Send notifications to all admins
                        await SendServicePurchaseNotifications(purchase);

                    // Send confirmation email to customer with invoice
                    await SendServicePurchaseConfirmationEmail(purchase);
                    });
                    TempData["success"] = "Payment successful! Your service subscription has been confirmed.";
                }
                else
                {
                    TempData["error"] = "Payment verification failed. Please contact support.";
                }
            }
            else if (purchase.PaymentStatus == "Approved" || purchase.PaymentStatus == "Paid")
            {
                _ = Task.Run(async () =>
                {
                    await SendServicePurchaseConfirmationEmail(purchase);
                });
            }

            return View(purchase);
        }

        private async Task SendServicePurchaseConfirmationEmail(ServicePurchase purchase)
        {
            try
            {
                // Get customer information using a new scope to avoid disposed context issues
                ApplicationUser? customer = null;
                string customerEmail = "";
                string customerName = "";

                if (purchase.ApplicationUserId != null)
                {
                    // Use a new scope to access the database
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        customer = unitOfWork.applicationUser.Get(u => u.Id == purchase.ApplicationUserId);
                        customerEmail = customer?.Email ?? "";
                        customerName = customer?.Name ?? "";
                    }
                }
                else
                {
                    // Guest user
                    customerEmail = purchase.GuestEmail ?? "";
                    customerName = purchase.GuestName ?? "";
                }

                if (string.IsNullOrWhiteSpace(customerEmail))
                {
                    _logger.LogWarning($"Cannot send confirmation email for service purchase {purchase.Id}: No email address available");
                    return;
                }

                // Generate PDF invoice
                byte[] invoicePdf = null;
                try
                {
                    invoicePdf = _invoiceService.GenerateServicePurchaseInvoicePdf(purchase, customer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error generating invoice PDF for service purchase {purchase.Id}");
                    // Continue with email sending even if invoice generation fails
                }

                // Generate email body
                var emailBody = GenerateServicePurchaseCustomerEmailTemplate(purchase, customer);

                // Send email with PDF attachment if available
                if (invoicePdf != null && _emailSender is EmailSender customEmailSender)
                {
                    await customEmailSender.SendEmailWithAttachmentAsync(
                        customerEmail,
                        $"Service Subscription Confirmation #{purchase.Id} - Ideal Weight",
                        emailBody,
                        invoicePdf,
                        $"Invoice-SVC-{purchase.Id}.pdf"
                    );
                    _logger.LogInformation($"Service purchase confirmation email sent with invoice to {customerEmail} for purchase {purchase.Id}");
                }
                else
                {
                    // Fallback: send email without attachment
                    await _emailSender.SendEmailAsync(
                        customerEmail,
                        $"Service Subscription Confirmation #{purchase.Id} - Ideal Weight",
                        emailBody
                    );
                    _logger.LogInformation($"Service purchase confirmation email sent (without invoice) to {customerEmail} for purchase {purchase.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending confirmation email for service purchase {purchase.Id}");
                // Don't throw - email failure shouldn't break the payment success flow
            }
        }

        private string GenerateServicePurchaseCustomerEmailTemplate(ServicePurchase purchase, ApplicationUser? customer)
        {
            var customerName = customer?.Name ?? purchase.GuestName ?? "Our valued Customer";
            var serviceTitle = purchase.ServiceSubscription?.Title ?? "Service Subscription";
            var serviceTypeText = purchase.ServiceSubscription?.ServiceType == ServiceType.Online
                ? "Online (Full Payment)"
                : $"Offline (Partial Payment - {purchase.ServiceSubscription?.OfflinePaymentPercent}% of total)";

            // Format currency function
            Func<decimal, string> formatCurrency = (amount) =>
            {
                return $"AED {amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ",")}";
            };

            var discountInfo = "";
            if (purchase.DiscountAmount > 0)
            {
                discountInfo = $@"
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Discount:</td>
                    <td style='padding: 8px 0; color: #059669; font-weight: 700; text-align: right;'>- {formatCurrency(purchase.DiscountAmount)}</td>
                </tr>";
            }

            var remainingAmountInfo = "";
            if (purchase.ServiceSubscription?.ServiceType == ServiceType.Offline)
            {
                var remainingAmount = purchase.TotalAmount - purchase.AmountPaid;
                if (remainingAmount > 0)
                {
                    remainingAmountInfo = $@"
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Remaining Amount:</td>
                    <td style='padding: 8px 0; color: #ef4444; font-weight: 700; text-align: right;'>{formatCurrency(remainingAmount)}</td>
                </tr>";
                }
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
        <div style='background: linear-gradient(135deg, #7c3aed 0%, #6d28d9 100%); padding: 40px; text-align: center;'>
            <div style='font-size: 48px; margin-bottom: 15px;'>✅</div>
            <h1 style='color: #ffffff; margin: 0; font-size: 32px; font-weight: 800;'>Service Subscription Confirmed!</h1>
            <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Thank you for your subscription, {customerName}</p>
        </div>

        <!-- Content -->
        <div style='padding: 30px;'>
            <div style='background: #dcfce7; border-left: 4px solid #059669; border-radius: 8px; padding: 15px; margin-bottom: 25px;'>
                <p style='margin: 0; color: #047857; font-weight: 600; font-size: 15px;'>
                    ✓ Your service subscription has been successfully confirmed!
                </p>
            </div>

            <h2 style='color: #1f2937; margin: 0 0 15px 0; font-size: 20px; border-bottom: 2px solid #e5e7eb; padding-bottom: 10px;'>
                Subscription #<span style='color: #7c3aed;'>{purchase.Id}</span>
            </h2>

            <table style='width: 100%; margin-bottom: 25px;'>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Service:</td>
                    <td style='padding: 8px 0; color: #1f2937; font-weight: 700; text-align: right;'>{serviceTitle}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Type:</td>
                    <td style='padding: 8px 0; color: #1f2937; font-weight: 700; text-align: right;'>{serviceTypeText}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Purchase Date:</td>
                    <td style='padding: 8px 0; color: #1f2937; font-weight: 700; text-align: right;'>{purchase.PurchaseDate:MMM dd, yyyy hh:mm tt}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Payment Status:</td>
                    <td style='padding: 8px 0; color: #059669; font-weight: 700; text-align: right;'>{purchase.PaymentStatus}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Total Amount:</td>
                    <td style='padding: 8px 0; color: #1f2937; font-weight: 700; text-align: right;'>{formatCurrency(purchase.TotalAmount)}</td>
                </tr>
                {discountInfo}
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-weight: 600;'>Amount Paid:</td>
                    <td style='padding: 8px 0; color: #059669; font-weight: 700; text-align: right;'>{formatCurrency(purchase.AmountPaid)}</td>
                </tr>
                {remainingAmountInfo}
            </table>

            <div style='background: linear-gradient(135deg, #1f2937 0%, #111827 100%); border-radius: 8px; padding: 20px; margin-bottom: 25px;'>
                <div style='display: flex; justify-content: space-between; align-items: center;'>
                    <span style='color: rgba(255,255,255,0.85); font-size: 18px; font-weight: 600;'>Total Amount:</span>
                    <span style='color: #ffffff; font-size: 28px; font-weight: 900;'>{formatCurrency(purchase.TotalAmount)}</span>
                </div>
            </div>

            <div style='background: #ede9fe; border-radius: 8px; padding: 20px; margin-top: 30px; text-align: center;'>
                <p style='margin: 0 0 10px 0; color: #6d28d9; font-weight: 600; font-size: 16px;'>Need help with your subscription?</p>
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

        // POST: ServiceSubscription/ValidatePromoCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ValidatePromoCode(string promoCode, int serviceId)
        {
            if (string.IsNullOrWhiteSpace(promoCode))
            {
                return Json(new { success = false, message = _localizer["PleaseEnterPromoCode"]?.Value ?? "Please enter a promo code" });
            }

            var promo = _unitOfWork.PromoCode.GetByCode(promoCode.Trim());
            
            if (promo == null)
            {
                return Json(new { success = false, message = _localizer["InvalidPromoCode"]?.Value ?? "Invalid promo code" });
            }

            // Load excluded services if not already loaded
            if (promo.ExcludedServiceSubscriptions == null)
            {
                promo = _unitOfWork.PromoCode.Get(
                    p => p.Id == promo.Id,
                    includeProperties: "ExcludedServiceSubscriptions"
                );
            }

            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            
            // Check if promo code is active
            if (!promo.IsActive)
            {
                return Json(new { success = false, message = _localizer["PromoCodeNoLongerActive"]?.Value ?? "This promo code is no longer active" });
            }

            // Check validity period
            if (now < promo.StartDate)
            {
                return Json(new { success = false, message = _localizer["PromoCodeNotYetValid"]?.Value ?? "This promo code is not yet valid" });
            }

            if (now > promo.EndDate)
            {
                return Json(new { success = false, message = _localizer["PromoCodeExpired"]?.Value ?? "This promo code has expired" });
            }

            // Check usage limit
            if (promo.UsageLimit.HasValue && promo.TimesUsed >= promo.UsageLimit.Value)
            {
                return Json(new { success = false, message = _localizer["PromoCodeUsageLimitReached"]?.Value ?? "This promo code has reached its usage limit" });
            }

            // Check per-user usage limit (only for authenticated users)
            if (User.Identity.IsAuthenticated && promo.UsageLimitPerUser.HasValue)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId) && !_unitOfWork.PromoCode.CanUserUsePromoCode(promo.Id, userId))
                {
                    return Json(new { success = false, message = _localizer["PromoCodeUsageLimitReached"]?.Value ?? "You have reached the usage limit for this promo code" });
                }
            }

            // Get service to calculate discount
            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == serviceId);
            if (service == null)
            {
                return Json(new { success = false, message = _localizer["ServiceNotFound"]?.Value ?? "Service not found" });
            }

            // Check if service is excluded from this promo code
            if (IsServiceExcludedFromPromoCode(promo, serviceId))
            {
                return Json(new { success = false, message = _localizer["PromoCodeNotApplicableToService"]?.Value ?? "This promo code is not applicable to this service" });
            }

            // Calculate base amount (service price)
            decimal baseAmount = service.Price;
            
            // Check minimum order amount
            if (promo.MinimumOrderAmount.HasValue && baseAmount < promo.MinimumOrderAmount.Value)
            {
                return Json(new { 
                    success = false, 
                    message = string.Format(_localizer["MinimumOrderAmountRequired"]?.Value ?? "Minimum order amount of {0} is required", 
                        baseAmount.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace("$", "AED ")) 
                });
            }

            // Calculate discount
            decimal discountAmount = 0;
            if (promo.DiscountType == DiscountType.Percentage)
            {
                discountAmount = baseAmount * (promo.DiscountValue / 100);
                if (promo.MaximumDiscountAmount.HasValue && discountAmount > promo.MaximumDiscountAmount.Value)
                {
                    discountAmount = promo.MaximumDiscountAmount.Value;
                }
            }
            else
            {
                discountAmount = promo.DiscountValue;
            }

            // Ensure discount doesn't exceed base amount
            if (discountAmount > baseAmount)
            {
                discountAmount = baseAmount;
            }

            decimal finalAmount = baseAmount - discountAmount;

            // Format discount text based on discount type
            string discountText;
            if (promo.DiscountType == DiscountType.Percentage)
            {
                discountText = string.Format(_localizer["PercentOff"]?.Value ?? "{0}% off", promo.DiscountValue);
            }
            else
            {
                var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
                var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
                var currencySymbol = IdealWeightNutrition.Utility.CurrencyHelper.GetCurrencySymbol(currentCulture);
                discountText = string.Format(_localizer["AmountOff"]?.Value ?? "{0} off", 
                    $"{currencySymbol} {promo.DiscountValue.ToString("N2", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ",")}");
            }

            return Json(new 
            { 
                success = true, 
                message = _localizer["PromoCodeAppliedSuccessfully"]?.Value ?? "Promo code applied successfully",
                promoCodeId = promo.Id,
                promoCode = promo.Code,
                discountAmount = discountAmount,
                baseAmount = baseAmount,
                finalAmount = finalAmount,
                discountText = discountText
            });
        }

        /// <summary>
        /// Checks if a service is excluded from a promo code
        /// </summary>
        private bool IsServiceExcludedFromPromoCode(PromoCode promoCode, int serviceId)
        {
            // If ExcludeAllServices is true, all services are excluded
            if (promoCode.ExcludeAllServices)
            {
                return true;
            }

            // If ExcludeAllServices is false, check if this specific service is in the excluded list
            // Load excluded services if not already loaded
            if (promoCode.ExcludedServiceSubscriptions == null)
            {
                // Load the promo code with excluded services from database
                var promoWithExclusions = _unitOfWork.PromoCode.Get(
                    p => p.Id == promoCode.Id,
                    includeProperties: "ExcludedServiceSubscriptions"
                );
                
                if (promoWithExclusions?.ExcludedServiceSubscriptions == null)
                {
                    return false;
                }
                
                return promoWithExclusions.ExcludedServiceSubscriptions
                    .Any(ess => ess.ServiceSubscriptionId == serviceId);
            }
            else
            {
                // Already loaded, check the collection
                return promoCode.ExcludedServiceSubscriptions
                    .Any(ess => ess.ServiceSubscriptionId == serviceId);
            }
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
                var hubContext = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<IdealWeightNutrition.Hubs.NotificationHub>>();
                await hubContext.Clients.Group("Admins").SendAsync(
                    "ReceiveServiceSubscriptionNotification",
                    new
                    {
                        title = "New Service Subscription",
                        message = $"Service subscription #{purchase.Id} - {purchase.ServiceSubscription?.Title}",
                        purchaseId = purchase.Id,
                        amount = purchase.AmountPaid,
                        timestamp = IdealWeightNutrition.Utility.DateTimeHelper.Now
                    }
                );
            }
            catch (Exception ex)
            {
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

        [HttpPost]
        public async Task<IActionResult> SendOtp(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = _localizer["EmailIsRequired"]?.Value ?? "Email is required" });
            }

            email = email.Trim().ToLowerInvariant();

            // Validate email format first
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!emailRegex.IsMatch(email))
            {
                return Json(new { success = false, message = _localizer["InvalidEmailFormat"]?.Value ?? "Please enter a valid email address" });
            }

            try
            {
                var otpHelper = new OtpHelper(_memoryCache);
                var otp = otpHelper.GenerateOtp();
                otpHelper.StoreOtp(email, otp);

                // Send OTP email
                var emailSubject = _localizer["EmailVerificationOTP"]?.Value ?? "Email Verification Code - Ideal Weight";
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9fafb;'>
                        <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #059669; margin-top: 0;'>Email Verification</h2>
                            <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                Thank you for subscribing to a service with Ideal Weight Nutrition. To complete your subscription, please verify your email address using the code below:
                            </p>
                            <div style='background: linear-gradient(135deg, #059669 0%, #047857 100%); color: white; padding: 20px; border-radius: 8px; text-align: center; margin: 30px 0;'>
                                <div style='font-size: 36px; font-weight: bold; letter-spacing: 8px; font-family: monospace;'>{otp}</div>
                            </div>
                            <p style='color: #6b7280; font-size: 14px; margin-top: 20px;'>
                                <strong>Important:</strong> This code will expire in 10 minutes. If you didn't request this code, please ignore this email.
                            </p>
                            <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;' />
                            <p style='color: #9ca3af; font-size: 12px; text-align: center; margin: 0;'>
                                © {DateTime.Now.Year} Ideal Weight Nutrition. All rights reserved.
                            </p>
                        </div>
                    </div>";

                await _emailSender.SendEmailAsync(email, emailSubject, emailBody);

                return Json(new 
                { 
                    success = true, 
                    message = _localizer["OTPSentSuccessfully"]?.Value ?? "Verification code sent to your email. Please check your inbox." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {Email}", email);
                return Json(new { success = false, message = _localizer["ErrorSendingOTP"]?.Value ?? "Error sending verification code. Please try again." });
            }
        }

        [HttpPost]
        public IActionResult VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return Json(new { success = false, message = _localizer["EmailAndOTPRequired"]?.Value ?? "Email and OTP are required" });
            }

            email = email.Trim().ToLowerInvariant();
            otp = otp.Trim();

            try
            {
                var otpHelper = new OtpHelper(_memoryCache);
                var result = otpHelper.VerifyOtp(email, otp);

                if (result.IsValid)
                {
                    return Json(new { success = true, message = _localizer["EmailVerifiedSuccessfully"]?.Value ?? "Email verified successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", email);
                return Json(new { success = false, message = _localizer["ErrorVerifyingOTP"]?.Value ?? "Error verifying code. Please try again." });
            }
        }

        [HttpPost]
        public IActionResult CheckEmailVerified(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { verified = false });
            }

            email = email.Trim().ToLowerInvariant();

            try
            {
                var otpHelper = new OtpHelper(_memoryCache);
                var verified = otpHelper.IsEmailVerified(email);
                return Json(new { verified = verified });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email verification status for {Email}", email);
                return Json(new { verified = false });
            }
        }

        private string GetBaseUrl()
        {
            return _configuration["SiteSettings:BaseUrl"]??string.Empty;
        }
    }
}

