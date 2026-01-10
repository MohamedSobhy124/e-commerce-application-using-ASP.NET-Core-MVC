using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using IdealWeightNutrition.Utility;
using IdealWeightNutrition.Services;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IStringLocalizer<IdealWeightNutrition.SharedResources> localizer,
            IUnitOfWork unitOfWork,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        // GET: Account Dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserName = user.UserName;
            ViewBag.Email = user.Email;
            ViewBag.PhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            ViewBag.EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            return View();
        }

        // GET: User Orders
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Index));
            }

            // Get all orders for the current user, excluding Pending and Cancelled orders
            var orders = _unitOfWork.OrderHeader.GetAll(
                o => o.ApplicationUserId == userId && 
                     !o.IsGuestOrder && 
                     o.OrderStatus != SD.StatusPending && 
                     o.OrderStatus != SD.StatusCancelled,
                includeProperties: "ApplicationUser"
            )
            .OrderByDescending(o => o.OrderDate)
            .ToList();

            // Load order details for each order and store in ViewBag
            var orderDetailsDict = new Dictionary<int, List<OrderDetail>>();
            foreach (var order in orders)
            {
                var details = _unitOfWork.OrderDetail.GetAll(
                    od => od.OrderHeaderId == order.Id,
                    includeProperties: "Product,Product.ProductImages,FlashSaleItem,ProductVariant"
                ).ToList();
                orderDetailsDict[order.Id] = details;
            }

            ViewBag.OrderDetails = orderDetailsDict;

            return View(orders);
        }

        // GET: Order Details
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Index));
            }

            // Get the order and verify it belongs to the current user
            var order = _unitOfWork.OrderHeader.Get(
                o => o.Id == id && o.ApplicationUserId == userId && !o.IsGuestOrder,
                includeProperties: "ApplicationUser"
            );

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"]?.Value ?? "Order not found";
                return RedirectToAction(nameof(Orders));
            }

            // Get order details
            var orderDetails = _unitOfWork.OrderDetail.GetAll(
                od => od.OrderHeaderId == order.Id,
                includeProperties: "Product,Product.ProductImages,Product.categry,FlashSaleItem,ProductVariant,ProductVariant.VariantOptionValues,ProductVariant.VariantOptionValues.OptionValue,ProductVariant.VariantOptionValues.OptionValue.ProductOption"
            ).ToList();

            ViewBag.OrderDetails = orderDetails;
            
            // Check if there's already a return request for this order
            var existingReturnRequest = _unitOfWork.ReturnRequest.GetByOrderId(order.Id).FirstOrDefault();
            ViewBag.HasReturnRequest = existingReturnRequest != null;
            ViewBag.ReturnRequest = existingReturnRequest;

            return View(order);
        }
        
        // GET: Request Return
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> RequestReturn(int orderId, string email = null)
        {
            OrderHeader order = null;
            
            // Check if user is authenticated
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Authenticated user - get order by user ID
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    order = _unitOfWork.OrderHeader.Get(
                        o => o.Id == orderId && o.ApplicationUserId == userId && !o.IsGuestOrder,
                        includeProperties: "ApplicationUser"
                    );
                }
            }
            else
            {
                // Guest order - verify using email
                if (string.IsNullOrEmpty(email))
                {
                    TempData["error"] = _localizer["EmailRequiredForGuestOrder"]?.Value ?? "Email is required to view guest order details. Please provide your email address.";
                    return RedirectToAction("TrackOrder", "Home", new { area = "Customer", orderId = orderId });
                }
                
                // Use ToLower() for case-insensitive comparison (EF Core can translate this)
                var emailLower = email.ToLower();
                order = _unitOfWork.OrderHeader.Get(
                    o => o.Id == orderId && o.Email != null && o.Email.ToLower() == emailLower && o.IsGuestOrder,
                    includeProperties: "ApplicationUser"
                );
            }

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"]?.Value ?? "Order not found or you don't have permission to view this order";
                if (user == null)
                {
                    return RedirectToAction("TrackOrder", "Home", new { area = "Customer" });
                }
                return RedirectToAction(nameof(Orders));
            }

            // Check if order is eligible for return (must be delivered or shipped)
            if (order.OrderStatus != SD.StatusDelivered && 
                order.OrderStatus != SD.StatusShipped && 
                order.OrderStatus != SD.StatusOutForDelivery)
            {
                TempData["error"] = _localizer["OrderNotEligibleForReturn"]?.Value ?? "This order is not eligible for return. Only delivered or shipped orders can be returned.";
                return RedirectToAction(nameof(OrderDetails), new { id = orderId });
            }

            // Check if there's already a pending or approved return request
            var existingReturnRequest = _unitOfWork.ReturnRequest.GetByOrderId(orderId)
                .FirstOrDefault(r => r.Status == SD.ReturnStatusPending || 
                                     r.Status == SD.ReturnStatusApproved || 
                                     r.Status == SD.ReturnStatusProcessing);
            
            if (existingReturnRequest != null)
            {
                TempData["error"] = _localizer["ReturnRequestAlreadyExists"]?.Value ?? "A return request already exists for this order.";
                return RedirectToAction(nameof(OrderDetails), new { id = orderId });
            }

            // Get order details
            var orderDetails = _unitOfWork.OrderDetail.GetAll(
                od => od.OrderHeaderId == order.Id,
                includeProperties: "Product,Product.ProductImages,ProductVariant,ProductVariant.VariantOptionValues,ProductVariant.VariantOptionValues.OptionValue,ProductVariant.VariantOptionValues.OptionValue.ProductOption"
            ).ToList();

            var returnRequestVM = new ReturnRequestVM
            {
                OrderHeaderId = order.Id,
                OrderHeader = order,
                Email = order.Email, // Pre-fill email for guest orders
                IsGuestOrder = order.IsGuestOrder,
                Items = orderDetails.Select(od => new ReturnRequestItemVM
                {
                    OrderDetailId = od.Id,
                    OrderDetail = od,
                    Quantity = od.Count, // Default to full quantity
                    ItemCondition = "New" // Default condition
                }).ToList()
            };

            ViewBag.OrderDetails = orderDetails;
            ViewBag.IsGuestView = order.IsGuestOrder;
            return View(returnRequestVM);
        }

        // POST: Request Return
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(ReturnRequestVM returnRequestVM)
        {
            OrderHeader orderHeader = null;
            string? userId = null;
            bool isGuestOrder = false;
            
            // Check if user is authenticated
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Authenticated user - get order by user ID
                    orderHeader = _unitOfWork.OrderHeader.Get(
                        o => o.Id == returnRequestVM.OrderHeaderId && o.ApplicationUserId == userId && !o.IsGuestOrder,
                        includeProperties: "ApplicationUser"
                    );
                }
            }
            else
            {
                // Guest order - verify using email
                if (string.IsNullOrEmpty(returnRequestVM.Email))
                {
                    TempData["error"] = _localizer["EmailRequiredForGuestReturn"]?.Value ?? "Email is required for guest return requests. Please provide your email address.";
                    return RedirectToAction("RequestReturn", new { orderId = returnRequestVM.OrderHeaderId });
                }
                
                // Use ToLower() for case-insensitive comparison (EF Core can translate this)
                var emailLower = returnRequestVM.Email?.ToLower();
                orderHeader = _unitOfWork.OrderHeader.Get(
                    o => o.Id == returnRequestVM.OrderHeaderId && 
                         o.Email != null && 
                         o.Email.ToLower() == emailLower && 
                         o.IsGuestOrder,
                    includeProperties: "ApplicationUser"
                );
                
                if (orderHeader == null)
                {
                    TempData["error"] = _localizer["OrderNotFoundOrEmailMismatch"]?.Value ?? "Order not found or email does not match. Please verify your email address.";
                    return RedirectToAction("RequestReturn", new { orderId = returnRequestVM.OrderHeaderId, email = returnRequestVM.Email });
                }
                
                isGuestOrder = true;
                returnRequestVM.IsGuestOrder = true;
            }

            if (orderHeader == null)
            {
                TempData["error"] = _localizer["OrderNotFound"]?.Value ?? "Order not found or you don't have permission to return this order";
                if (user == null)
                {
                    return RedirectToAction("TrackOrder", "Home", new { area = "Customer" });
                }
                return RedirectToAction(nameof(Orders));
            }

            // Remove validation errors for items with Quantity = 0 (not selected for return)
            for (int i = 0; i < returnRequestVM.Items.Count; i++)
            {
                if (returnRequestVM.Items[i].Quantity == 0)
                {
                    // Remove validation errors for unselected items
                    ModelState.Remove($"Items[{i}].Quantity");
                    ModelState.Remove($"Items[{i}].ItemReason");
                    ModelState.Remove($"Items[{i}].ItemCondition");
                }
            }

            // Validate model
            if (!ModelState.IsValid)
            {
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    od => od.OrderHeaderId == orderHeader.Id,
                    includeProperties: "Product,Product.ProductImages,ProductVariant,ProductVariant.VariantOptionValues,ProductVariant.VariantOptionValues.OptionValue,ProductVariant.VariantOptionValues.OptionValue.ProductOption"
                ).ToList();

                returnRequestVM.OrderHeader = orderHeader;
                ViewBag.OrderDetails = orderDetails;
                ViewBag.IsGuestView = isGuestOrder;
                return View(returnRequestVM);
            }

            // Check if order is eligible for return (status check)
            if (orderHeader.OrderStatus != SD.StatusDelivered && 
                orderHeader.OrderStatus != SD.StatusShipped && 
                orderHeader.OrderStatus != SD.StatusOutForDelivery)
            {
                TempData["error"] = _localizer["OrderNotEligibleForReturn"]?.Value ?? "This order is not eligible for return. Only delivered or shipped orders can be returned.";
                if (isGuestOrder)
                {
                    return RedirectToAction("OrderDetails", "Home", new { area = "Customer", id = IdealWeightNutrition.Utility.IdEncryptionHelper.EncryptId(orderHeader.Id), email = orderHeader.Email });
                }
                return RedirectToAction(nameof(OrderDetails), new { id = returnRequestVM.OrderHeaderId });
            }

            // Check return time limit (14 days from delivery date for delivered orders, or from shipping date)
            var returnDeadline = orderHeader.OrderStatus == SD.StatusDelivered 
                ? orderHeader.ShippingDate.AddDays(14) 
                : orderHeader.ShippingDate.AddDays(30); // More lenient for shipped orders
            
            if (IdealWeightNutrition.Utility.DateTimeHelper.Now > returnDeadline)
            {
                TempData["error"] = _localizer["ReturnTimeLimitExceeded"]?.Value ?? $"The return deadline for this order has passed. Returns must be requested within 14 days of delivery.";
                if (isGuestOrder)
                {
                    return RedirectToAction("OrderDetails", "Home", new { area = "Customer", id = IdealWeightNutrition.Utility.IdEncryptionHelper.EncryptId(orderHeader.Id), email = orderHeader.Email });
                }
                return RedirectToAction(nameof(OrderDetails), new { id = returnRequestVM.OrderHeaderId });
            }

            // Check if there's already a pending or approved return request
            var existingReturnRequest = _unitOfWork.ReturnRequest.GetByOrderId(returnRequestVM.OrderHeaderId)
                .FirstOrDefault(r => r.Status == SD.ReturnStatusPending || 
                                     r.Status == SD.ReturnStatusApproved || 
                                     r.Status == SD.ReturnStatusProcessing);
            
            if (existingReturnRequest != null)
            {
                TempData["error"] = _localizer["ReturnRequestAlreadyExists"]?.Value ?? "A return request already exists for this order.";
                return RedirectToAction(nameof(OrderDetails), new { id = returnRequestVM.OrderHeaderId });
            }

            // Validate return items
            var orderDetailsList = _unitOfWork.OrderDetail.GetAll(
                od => od.OrderHeaderId == returnRequestVM.OrderHeaderId
            ).ToList();

            decimal totalRefundAmount = 0;
            var returnRequestItems = new List<ReturnRequestItem>();

            foreach (var itemVM in returnRequestVM.Items)
            {
                if (itemVM.Quantity > 0)
                {
                    var orderDetail = orderDetailsList.FirstOrDefault(od => od.Id == itemVM.OrderDetailId);
                    if (orderDetail == null)
                    {
                        ModelState.AddModelError("", $"Order detail {itemVM.OrderDetailId} not found.");
                        var order = _unitOfWork.OrderHeader.Get(
                            o => o.Id == returnRequestVM.OrderHeaderId,
                            includeProperties: "ApplicationUser"
                        );
                        var details = _unitOfWork.OrderDetail.GetAll(
                            od => od.OrderHeaderId == order.Id,
                            includeProperties: "Product,Product.ProductImages,ProductVariant"
                        ).ToList();
                        returnRequestVM.OrderHeader = order;
                        ViewBag.OrderDetails = details;
                        return View(returnRequestVM);
                    }

                    if (itemVM.Quantity > orderDetail.Count)
                    {
                        ModelState.AddModelError("", $"Return quantity cannot exceed ordered quantity for item {orderDetail.Product?.Title ?? "Unknown"}.");
                        var order = _unitOfWork.OrderHeader.Get(
                            o => o.Id == returnRequestVM.OrderHeaderId,
                            includeProperties: "ApplicationUser"
                        );
                        var details = _unitOfWork.OrderDetail.GetAll(
                            od => od.OrderHeaderId == order.Id,
                            includeProperties: "Product,Product.ProductImages,ProductVariant"
                        ).ToList();
                        returnRequestVM.OrderHeader = order;
                        ViewBag.OrderDetails = details;
                        return View(returnRequestVM);
                    }

                    var returnItem = new ReturnRequestItem
                    {
                        OrderDetailId = itemVM.OrderDetailId,
                        Quantity = itemVM.Quantity,
                        ReturnPrice = (decimal)orderDetail.Price,
                        ItemReason = itemVM.ItemReason,
                        ItemCondition = itemVM.ItemCondition ?? "New"
                    };

                    returnRequestItems.Add(returnItem);
                    totalRefundAmount += (decimal)(orderDetail.Price * itemVM.Quantity);
                }
            }

            if (returnRequestItems.Count == 0)
            {
                ModelState.AddModelError("", _localizer["AtLeastOneItemRequiredForReturn"]?.Value ?? "At least one item must be selected for return.");
                var order = _unitOfWork.OrderHeader.Get(
                    o => o.Id == returnRequestVM.OrderHeaderId,
                    includeProperties: "ApplicationUser"
                );
                var details = _unitOfWork.OrderDetail.GetAll(
                    od => od.OrderHeaderId == order.Id,
                    includeProperties: "Product,Product.ProductImages,ProductVariant,ProductVariant.VariantOptionValues,ProductVariant.VariantOptionValues.OptionValue,ProductVariant.VariantOptionValues.OptionValue.ProductOption"
                ).ToList();
                returnRequestVM.OrderHeader = order;
                ViewBag.OrderDetails = details;
                return View(returnRequestVM);
            }

            // Create return request
            var returnRequest = new ReturnRequest
            {
                OrderHeaderId = returnRequestVM.OrderHeaderId,
                ApplicationUserId = isGuestOrder ? null : userId,
                Email = isGuestOrder ? returnRequestVM.Email : (orderHeader.Email ?? orderHeader.ApplicationUser?.Email),
                PhoneNumber = isGuestOrder ? orderHeader.PhoneNumber : null,
                Reason = returnRequestVM.Reason,
                AdditionalNotes = returnRequestVM.AdditionalNotes,
                Status = SD.ReturnStatusPending,
                RequestDate = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                RefundAmount = totalRefundAmount,
                RefundStatus = SD.RefundStatusPending
            };

            _unitOfWork.ReturnRequest.Add(returnRequest);
            _unitOfWork.save();

            // Add return request items
            foreach (var item in returnRequestItems)
            {
                item.ReturnRequestId = returnRequest.Id;
                _unitOfWork.ReturnRequestItem.Add(item);
            }

            _unitOfWork.save();

            // Update order status
            if (orderHeader.OrderStatus != SD.StatusReturnRequested)
            {
                orderHeader.OrderStatus = SD.StatusReturnRequested;
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.save();
            }

            // Send notification to admin
            await _notificationService.SendReturnRequestNotificationToAdmins(returnRequest);

            // Send confirmation email to customer (guest or authenticated)
            if (isGuestOrder && !string.IsNullOrEmpty(returnRequest.Email))
            {
                await _notificationService.SendReturnRequestStatusUpdateToCustomer(returnRequest);
            }

            TempData["success"] = _localizer["ReturnRequestSubmittedSuccessfully"]?.Value ?? "Return request submitted successfully. We will review your request and get back to you soon.";
            
            // Redirect based on user type
            if (isGuestOrder)
            {
                var encryptedOrderId = IdealWeightNutrition.Utility.IdEncryptionHelper.EncryptId(orderHeader.Id);
                TempData["returnRequestId"] = returnRequest.Id; // Store for tracking
                return RedirectToAction("OrderDetails", "Home", new { area = "Customer", id = encryptedOrderId, email = orderHeader.Email });
            }
            
            return RedirectToAction(nameof(OrderDetails), new { id = returnRequestVM.OrderHeaderId });
        }
    }
}

