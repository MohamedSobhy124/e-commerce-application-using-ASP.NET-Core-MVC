using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using IdealWeightNutrition.Services;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TamaraSettings _tamaraSettings;
        private readonly GeideaSettings _geideaSettings;
        private readonly TappySettings _tappySettings;
        private readonly ILogger<OrderController> _logger;
        private readonly INotificationService _notificationService;

        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(
            IUnitOfWork unitOfWork, 
            IOptions<TamaraSettings> tamaraSettings, 
            IOptions<GeideaSettings> geideaSettings,
            IOptions<TappySettings> tappySettings,
            ILogger<OrderController> logger,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _tamaraSettings = tamaraSettings.Value;
            _geideaSettings = geideaSettings.Value;
            _tappySettings = tappySettings.Value;
            _logger = logger;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            return View();
        }
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Details(int id)
        {
            // Get order header without ApplicationUser first
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            // Load ApplicationUser only if it's not a guest order
            if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
            {
                orderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
            }

            OrderVM = new OrderVM
            {
                OrderHeader = orderHeader,
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.save();

            TempData["success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { id = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult StartProcessing(int id)
        {
            var orderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            if (orderFromDb != null)
            {
                var oldOrderStatus = orderFromDb.OrderStatus;
                orderFromDb.OrderStatus = SD.StatusInProcess;
                _unitOfWork.OrderHeader.Update(orderFromDb);
                _unitOfWork.save();
                
                // Log audit trail
                LogAuditAction(id, "OrderProcessingStarted",
                    $"Order processing started. Status changed from {oldOrderStatus} to {SD.StatusInProcess}",
                    oldOrderStatus, SD.StatusInProcess, orderFromDb.PaymentStatus, orderFromDb.PaymentStatus);
                
                TempData["success"] = "Order Status Updated Successfully.";
            }
            else
            {
                TempData["error"] = "Order not found.";
            }
            
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> ShipOrder(int id, string carrier, string trackingNumber)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }
            
            orderHeader.TrackingNumber = trackingNumber;
            orderHeader.Carrier = carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(30);
            }

            // Capture payment when shipping (only if not already captured)
            // For Tamara: Capture authorized payment
            // For Geidea/Tabby: Payment is typically captured automatically, but we verify status
            if (orderHeader.PaymentMethod == SD.PaymentMethodTamara && 
                !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                orderHeader.PaymentStatus != SD.PaymentStatusPaid && // Only capture if not already paid
                _tamaraSettings.Enabled)
            {
                try
                {
                    var tamaraHelper = new TamaraHelper(_tamaraSettings);
                    
                    // Check order status in Tamara first
                    var orderDetailsCheck = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);
                    if (orderDetailsCheck.Success && 
                        (orderDetailsCheck.Status?.ToLower().Contains("captured") == true ||
                         orderDetailsCheck.Status?.ToLower().Contains("paid") == true))
                    {
                        _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} is already captured");
                        orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                    }
                    else
                    {
                        // Get order details to prepare capture request
                        var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product");
                        
                        var captureRequest = new TamaraCaptureRequest
                    {
                        TotalAmount = new TamaraAmount
                        {
                            Amount = (decimal)orderHeader.OrderTotal,
                            Currency = _tamaraSettings.Currency ?? "AED"
                        },
                        ShippingInfo = new TamaraShippingInfo
                        {
                            ShippedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            ShippingCompany = carrier
                        },
                        TaxAmount = new TamaraAmount
                        {
                            Amount = 0,
                            Currency = _tamaraSettings.Currency ?? "AED"
                        },
                        ShippingAmount = new TamaraAmount
                        {
                            Amount = 0,
                            Currency = _tamaraSettings.Currency ?? "AED"
                        },
                        Discount = null,
                        Items = orderDetails.Select(item => new TamaraItem
                        {
                            ReferenceId = item.ProductId.ToString(),
                            Type = "Physical",
                            Name = item.Product.Title,
                            Sku = item.ProductId.ToString(),
                            Quantity = item.Count,
                            UnitPrice = new TamaraAmount
                            {
                                Amount = (decimal)item.Price,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            TotalAmount = new TamaraAmount
                            {
                                Amount = (decimal)(item.Price * item.Count),
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            DiscountAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            TaxAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            }
                        }).ToList()
                        };
                        
                        var captureResponse = await tamaraHelper.CaptureOrderAsync(orderHeader.PaymentIntentId, captureRequest);
                        
                        if (captureResponse.Success)
                        {
                            _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} captured successfully");
                            orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                            TempData["success"] = "Order Shipped and Payment Captured Successfully with Tamara.";
                        }
                        else
                        {
                            _logger.LogWarning($"Tamara capture failed for order {id}: {captureResponse.Message}");
                            TempData["warning"] = $"Order Shipped but Tamara capture failed: {captureResponse.Message}. Please capture manually in Tamara dashboard.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error capturing Tamara payment for order {id}");
                    TempData["warning"] = "Order Shipped but Tamara capture encountered an error. Please check Tamara dashboard.";
                }
            }
            // For Geidea and Tabby, payment is typically captured automatically
            // Just ensure payment status is updated
            else if ((orderHeader.PaymentMethod == SD.PaymentMethodGeidea || 
                     orderHeader.PaymentMethod == SD.PaymentMethodTappy) &&
                     orderHeader.PaymentStatus != SD.PaymentStatusPaid)
            {
                // Verify payment status with gateway
                try
                {
                    if (orderHeader.PaymentMethod == SD.PaymentMethodGeidea && !string.IsNullOrEmpty(orderHeader.SessionId))
                    {
                        var geideaHelper = new GeideaHelper(_geideaSettings);
                        var verifyResult = await geideaHelper.VerifyPaymentAsync(orderHeader.Id.ToString());
                        if (verifyResult.Success && verifyResult.IsPaid)
                        {
                            orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                            _logger.LogInformation($"Geidea order {orderHeader.Id} payment verified as paid");
                        }
                    }
                    else if (orderHeader.PaymentMethod == SD.PaymentMethodTappy && !string.IsNullOrEmpty(orderHeader.PaymentIntentId))
                    {
                        var tappyHelper = new TappyHelper(_tappySettings);
                        var verifyResult = await tappyHelper.VerifyPaymentAsync(orderHeader.PaymentIntentId);
                        if (verifyResult.Success && verifyResult.IsPaid)
                        {
                            orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                            _logger.LogInformation($"Tabby order {orderHeader.PaymentIntentId} payment verified as paid");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error verifying payment status for order {id}");
                    // Continue with shipping even if verification fails
                }
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.save();

            LogAuditAction(id, "OrderShipped",
                 $"Order Shipped. Status changed from {SD.StatusInProcess} to {SD.StatusShipped}",
                 SD.StatusInProcess, SD.StatusShipped);
            if (!TempData.ContainsKey("success") && !TempData.ContainsKey("warning"))
            {
                TempData["success"] = "Order Shipped Successfully.";
            }
            
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> MarkAsDelivered(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            if (orderHeader.OrderStatus != SD.StatusShipped)
            {
                TempData["error"] = "Only shipped orders can be marked as delivered";
                return RedirectToAction(nameof(Details), new { id = id });
            }


            orderHeader.OrderStatus = SD.StatusDelivered;
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.save();
            
            // Send delivery confirmation email to customer
            try
            {
                await _notificationService.SendOrderDeliveredNotification(orderHeader);
                _logger.LogInformation("Delivery notification sent for order {OrderId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send delivery notification for order {OrderId}", id);
                // Don't fail the action if email fails - order is still delivered
            }
            
            LogAuditAction(id, "OrderMarkedasDelivered",
            $"Order Marked as Delivered. Status changed from {SD.StatusShipped} to {SD.StatusDelivered}",
            SD.StatusShipped, SD.StatusDelivered);
            TempData["success"] = "Order Marked as Delivered Successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        /// <summary>
        /// Cancel an order - Only works for orders that are authorized but NOT yet captured
        /// Cancel is allowed only before capture (before shipping)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            // Validate order can be cancelled
            // Cancel is only allowed if:
            // 1. Order is NOT shipped (shipped = captured/paid)
            // 2. Payment is authorized but NOT captured
            // 3. Order status is not already cancelled
            if (orderHeader.OrderStatus == SD.StatusCancelled)
            {
                var message = "Order is already cancelled";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, info = true });
                }
                TempData["info"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (orderHeader.OrderStatus == SD.StatusShipped || 
                orderHeader.OrderStatus == SD.StatusDelivered)
            {
                var message = "Cannot cancel order. Order has already been shipped (payment captured). Use refund instead.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // For cancel to work, payment should be authorized but NOT captured
            // If order is shipped, payment is captured - cannot cancel, must refund
            // If order is paid but not shipped, check with gateway if it's captured
            bool isPaymentCaptured = false;
            
            // Check with gateway if payment is captured (for Tamara, this is important)
            if (orderHeader.PaymentMethod == SD.PaymentMethodTamara && 
                !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                orderHeader.PaymentStatus == SD.PaymentStatusPaid)
            {
                try
                {
                    var tamaraHelper = new TamaraHelper(_tamaraSettings);
                    var orderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);
                    if (orderDetails.Success)
                    {
                        var statusLower = orderDetails.Status?.ToLower() ?? "";
                        isPaymentCaptured = statusLower.Contains("captured") || 
                                          statusLower.Contains("shipped") ||
                                          orderDetails.PaymentStatus?.ToLower().Contains("captured") == true;
                    }
                }
                catch
                {
                    // If check fails, assume not captured if order not shipped
                    isPaymentCaptured = false;
                }
            }
            
            if (isPaymentCaptured)
            {
                var message = "Cannot cancel order. Payment has already been captured. Use refund instead.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Process cancellation with payment gateway if applicable
            bool gatewayCancelSuccess = false;
            string gatewayMessage = "";

            // Cancel with Tamara
            if (orderHeader.PaymentMethod == SD.PaymentMethodTamara && 
                !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                _tamaraSettings.Enabled)
            {
                try
                {
                    var tamaraHelper = new TamaraHelper(_tamaraSettings);
                    
                    // Get order details for cancel request
                    var orderDetails = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product");
                    
                    var cancelRequest = new TamaraCancelRequest
                    {
                        TotalAmount = new TamaraAmount
                        {
                            Amount = (decimal)orderHeader.OrderTotal,
                            Currency = _tamaraSettings.Currency ?? "AED"
                        },
                        Items = orderDetails.Select(item => new TamaraItem
                        {
                            ReferenceId = item.ProductId.ToString(),
                            Type = "Physical",
                            Name = item.Product.Title,
                            Sku = item.ProductId.ToString(),
                            Quantity = item.Count,
                            UnitPrice = new TamaraAmount
                            {
                                Amount = (decimal)item.Price,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            TotalAmount = new TamaraAmount
                            {
                                Amount = (decimal)(item.Price * item.Count),
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            DiscountAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            },
                            TaxAmount = new TamaraAmount
                            {
                                Amount = 0,
                                Currency = _tamaraSettings.Currency ?? "AED"
                            }
                        }).ToList()
                    };
                    
                    var cancelResponse = await tamaraHelper.CancelOrderAsync(orderHeader.PaymentIntentId, cancelRequest);
                    
                    if (cancelResponse.Success)
                    {
                        gatewayCancelSuccess = true;
                        gatewayMessage = "Order cancelled successfully with Tamara";
                        _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} cancelled successfully");
                    }
                    else
                    {
                        _logger.LogWarning($"Tamara cancel failed for order {id}: {cancelResponse.Message}");
                        gatewayMessage = $"Tamara cancellation failed: {cancelResponse.Message}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cancelling Tamara payment for order {id}");
                    gatewayMessage = $"Tamara cancellation error: {ex.Message}";
                }
            }
            // Cancel with Geidea
            else if (orderHeader.PaymentMethod == SD.PaymentMethodGeidea && 
                     !string.IsNullOrEmpty(_geideaSettings.MerchantPublicKey))
            {
                try
                {
                    var geideaHelper = new GeideaHelper(_geideaSettings);
                    
                    // For Geidea, the OrderId sent during payment creation is orderHeader.Id.ToString()
                    // SessionId contains the TransactionId from Geidea's response
                    // Try using the original OrderId first (as that's what was sent during payment creation)
                    // Then fallback to TransactionId (SessionId) if OrderId doesn't work
                    string orderIdToUse = orderHeader.PaymentIntentId.ToString();
                    string transactionIdToUse = orderHeader.SessionId;
                    
                    var cancelResponse = await geideaHelper.CancelPaymentAsync(
                        orderId: orderIdToUse, // Use original OrderId that was sent during payment creation
                        transactionId: transactionIdToUse); // Also pass TransactionId as fallback
                    
                    if (cancelResponse.Success)
                    {
                        gatewayCancelSuccess = true;
                        gatewayMessage = "Order cancelled successfully with Geidea";
                        _logger.LogInformation($"Geidea order {orderIdToUse} (TransactionId: {transactionIdToUse}) cancelled successfully");
                    }
                    else
                    {
                        _logger.LogWarning($"Geidea cancel failed for order {id}: {cancelResponse.Message}");
                        
                        // Check for NotFound error and provide better message
                        if (cancelResponse.Message != null && 
                            (cancelResponse.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
                             cancelResponse.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                             cancelResponse.Message.Contains("404", StringComparison.OrdinalIgnoreCase)))
                        {
                            gatewayMessage = $"Geidea order not found. The order with ID '{orderIdToUse}' (TransactionId: '{transactionIdToUse}') does not exist in Geidea's system. " +
                                           $"This may occur if:\n" +
                                           $"• The order was never successfully created in Geidea\n" +
                                           $"• The order was already cancelled or completed\n" +
                                           $"• The order identifier is incorrect\n\n" +
                                           $"Please verify the order status in Geidea dashboard using Order ID: {orderIdToUse} or Transaction ID: {transactionIdToUse}";
                        }
                        else
                        {
                            gatewayMessage = $"Geidea cancellation failed: {cancelResponse.Message}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cancelling Geidea payment for order {id}");
                    
                    // Check if exception indicates NotFound
                    if (ex.Message != null && 
                        (ex.Message.Contains("NotFound", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("404", StringComparison.OrdinalIgnoreCase)))
                    {
                        gatewayMessage = $"Geidea order not found. The order with ID '{orderHeader.Id}' (TransactionId: '{orderHeader.SessionId}') does not exist in Geidea's system. " +
                                       $"This may occur if:\n" +
                                       $"• The order was never successfully created in Geidea\n" +
                                       $"• The order was already cancelled or completed\n" +
                                       $"• The order identifier is incorrect\n\n" +
                                       $"Please verify the order status in Geidea dashboard.";
                    }
                    else
                    {
                        gatewayMessage = $"Geidea cancellation error: {ex.Message}";
                    }
                }
            }
            // Cancel with Tappy/Tabby
            else if (orderHeader.PaymentMethod == SD.PaymentMethodTappy && 
                     !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                     _tappySettings.Enabled)
            {
                try
                {
                    var tappyHelper = new TappyHelper(_tappySettings);
                    var cancelResponse = await tappyHelper.CancelPaymentAsync(orderHeader.PaymentIntentId);
                    
                    if (cancelResponse.Success)
                    {
                        gatewayCancelSuccess = true;
                        gatewayMessage = "Order cancelled successfully with Tabby";
                        _logger.LogInformation($"Tabby order {orderHeader.PaymentIntentId} cancelled successfully");
                    }
                    else
                    {
                        _logger.LogWarning($"Tabby cancel failed for order {id}: {cancelResponse.Message}");
                        gatewayMessage = $"Tabby cancellation failed: {cancelResponse.Message}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cancelling Tabby payment for order {id}");
                    gatewayMessage = $"Tabby cancellation error: {ex.Message}";
                }
            }
            
            // Only update order status if gateway cancel succeeded OR if no gateway integration
            // If gateway cancel failed, don't update order status - show error to user
            if (gatewayCancelSuccess || 
                (orderHeader.PaymentMethod != SD.PaymentMethodTamara && 
                 orderHeader.PaymentMethod != SD.PaymentMethodGeidea && 
                 orderHeader.PaymentMethod != SD.PaymentMethodTappy))
            {
                // Gateway cancel succeeded or no gateway - update order status
                var oldPaymentStatus = orderHeader.PaymentStatus;
                var oldOrderStatus = orderHeader.OrderStatus;
                
                orderHeader.PaymentStatus = SD.PaymentStatusCancelled;
                orderHeader.OrderStatus = SD.StatusCancelled;
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.save();

                // Log audit trail
                LogAuditAction(id, "OrderCancelled",
                    $"Order cancelled. Gateway: {orderHeader.PaymentMethod}. Gateway result: {gatewayMessage}",
                    oldOrderStatus, SD.StatusCancelled, oldPaymentStatus, SD.PaymentStatusCancelled);
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    // Return JSON for AJAX requests
                    return Json(new 
                    { 
                        success = true, 
                        message = gatewayCancelSuccess ? gatewayMessage : "Order cancelled successfully.",
                        orderStatus = SD.StatusCancelled,
                        paymentStatus = SD.PaymentStatusCancelled
                    });
                }
                
                TempData["success"] = gatewayCancelSuccess ? gatewayMessage : "Order cancelled successfully.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            else
            {
                // Gateway cancel failed - don't update order, return error
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    // Return JSON for AJAX requests
                    return Json(new 
                    { 
                        success = false, 
                        message = gatewayMessage ?? "Order cancellation failed. Please verify in payment gateway dashboard.",
                        error = true
                    });
                }
                
                TempData["error"] = gatewayMessage ?? "Order cancellation failed. Please verify in payment gateway dashboard.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
        }
        
        /// <summary>
        /// Refund an order - Only works for orders that are captured/paid
        /// Refund is allowed only after capture (after shipping)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RefundOrder(int id, decimal refundAmount, string refundReason = "")
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Order not found", error = true });
                }
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            // Validate refund amount
            if (refundAmount <= 0)
            {
                var message = "Refund amount must be greater than 0";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (refundAmount > (decimal)orderHeader.OrderTotal)
            {
                var message = $"Refund amount ({refundAmount:C}) cannot exceed order total ({orderHeader.OrderTotal:C})";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }
            
            // Validate order can be refunded
            // Refund is only allowed if:
            // 1. Payment has been captured/paid
            // 2. Order is shipped or delivered (captured orders)
            if (orderHeader.PaymentStatus != SD.PaymentStatusPaid)
            {
                var message = "Cannot refund order. Payment has not been captured/paid. Only captured payments can be refunded.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (orderHeader.OrderStatus == SD.StatusCancelled)
            {
                var message = "Cannot refund a cancelled order";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = message, error = true });
                }
                TempData["error"] = message;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Determine if full or partial refund
            bool isFullRefund = refundAmount >= (decimal)orderHeader.OrderTotal;
            
            // Process refund with payment gateway if applicable
            bool gatewayRefundSuccess = false;
            string gatewayMessage = "";
            
            // Refund with Tamara
            if (orderHeader.PaymentMethod == SD.PaymentMethodTamara && 
                !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                _tamaraSettings.Enabled)
            {
                decimal? remainingRefundableAmount = null; // Declare outside try block for use in catch
                
                try
                {
                    var tamaraHelper = new TamaraHelper(_tamaraSettings);
                    
                    // Check order details first to get remaining refundable amount
                    var orderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);
                    
                    if (orderDetails.Success)
                    {
                        _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} status before refund: {orderDetails.Status}");
                        
                        // Get remaining refundable amount if available
                        if (orderDetails.RemainingRefundableAmount.HasValue)
                        {
                            remainingRefundableAmount = orderDetails.RemainingRefundableAmount.Value;
                            _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} remaining refundable amount: {remainingRefundableAmount:C}");
                            
                            // Validate refund amount against remaining refundable amount
                            if (refundAmount > remainingRefundableAmount.Value)
                            {
                                var message = $"Cannot refund {refundAmount:C}. " +
                                            $"The remaining refundable amount is {remainingRefundableAmount.Value:C}. " +
                                            $"This order has already been partially refunded. " +
                                            $"Please enter a refund amount that does not exceed {remainingRefundableAmount.Value:C}.";
                                
                                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                                {
                                    return Json(new { success = false, message = message, error = true, remainingRefundableAmount = remainingRefundableAmount.Value });
                                }
                                
                                TempData["error"] = message;
                                return RedirectToAction(nameof(Details), new { id = id });
                            }
                        }
                        else if (orderDetails.TotalAmount.HasValue && orderDetails.RefundedAmount.HasValue)
                        {
                            // Calculate remaining amount
                            remainingRefundableAmount = orderDetails.TotalAmount.Value - orderDetails.RefundedAmount.Value;
                            _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} calculated remaining refundable amount: {remainingRefundableAmount:C} (Total: {orderDetails.TotalAmount.Value:C}, Refunded: {orderDetails.RefundedAmount.Value:C})");
                            
                            if (refundAmount > remainingRefundableAmount.Value)
                            {
                                var message = $"Cannot refund {refundAmount:C}. " +
                                            $"The remaining refundable amount is {remainingRefundableAmount.Value:C}. " +
                                            $"This order has already been partially refunded ({orderDetails.RefundedAmount.Value:C} refunded out of {orderDetails.TotalAmount.Value:C}). " +
                                            $"Please enter a refund amount that does not exceed {remainingRefundableAmount.Value:C}.";
                                
                                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                                {
                                    return Json(new { success = false, message = message, error = true, remainingRefundableAmount = remainingRefundableAmount.Value });
                                }
                                
                                TempData["error"] = message;
                                return RedirectToAction(nameof(Details), new { id = id });
                            }
                        }
                    }
                    
                    var refundRequest = new TamaraRefundRequest
                    {
                        TotalAmount = new TamaraAmount
                        {
                            Amount = refundAmount,
                            Currency = _tamaraSettings.Currency ?? "AED"
                        },
                        Comment = !string.IsNullOrEmpty(refundReason) ? refundReason : "Refund requested by merchant"
                    };
                    
                    var refundResponse = await tamaraHelper.RefundOrderAsync(orderHeader.PaymentIntentId, refundRequest);
                    
                    if (refundResponse.Success)
                    {
                        gatewayRefundSuccess = true;
                        gatewayMessage = $"Refund of {refundAmount:C} processed successfully with Tamara";
                        _logger.LogInformation($"Tamara order {orderHeader.PaymentIntentId} refunded successfully. Amount: {refundAmount}");
                    }
                    else
                    {
                        _logger.LogWarning($"Tamara refund failed for order {id}: {refundResponse.Message}");
                        
                        // Check for specific error about remaining refundable amount
                        if (refundResponse.Message != null && 
                            (refundResponse.Message.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase) ||
                             (refundResponse.Message.Contains("remaining", StringComparison.OrdinalIgnoreCase) && 
                              refundResponse.Message.Contains("refund", StringComparison.OrdinalIgnoreCase))))
                        {
                            // If we validated the amount before and it passed, but Tamara still rejects it,
                            // there might be a discrepancy. Try to get fresh order details to see actual remaining amount.
                            if (remainingRefundableAmount.HasValue && refundAmount <= remainingRefundableAmount.Value)
                            {
                                // Amount was validated but still rejected - get fresh order details to check actual remaining amount
                                _logger.LogWarning($"Tamara rejected refund of {refundAmount:C} despite validation showing {remainingRefundableAmount.Value:C} remaining. Fetching fresh order details...");
                                
                                try
                                {
                                    var freshOrderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);
                                    if (freshOrderDetails.Success)
                                    {
                                        decimal? actualRemaining = null;
                                        if (freshOrderDetails.RemainingRefundableAmount.HasValue)
                                        {
                                            actualRemaining = freshOrderDetails.RemainingRefundableAmount.Value;
                                        }
                                        else if (freshOrderDetails.TotalAmount.HasValue && freshOrderDetails.RefundedAmount.HasValue)
                                        {
                                            actualRemaining = freshOrderDetails.TotalAmount.Value - freshOrderDetails.RefundedAmount.Value;
                                        }
                                        
                                        if (actualRemaining.HasValue)
                                        {
                                            if (refundAmount > actualRemaining.Value)
                                            {
                                                gatewayMessage = $"Cannot refund {refundAmount:C}. " +
                                                               $"The actual remaining refundable amount is {actualRemaining.Value:C} (not {remainingRefundableAmount.Value:C} as initially shown). " +
                                                               $"Please enter a refund amount that does not exceed {actualRemaining.Value:C}.";
                                            }
                                            else
                                            {
                                                gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount is {actualRemaining.Value:C}. " +
                                                               $"This may be due to a timing issue or Tamara's internal validation. " +
                                                               $"Please try again in a few moments, or check the order status in Tamara dashboard.";
                                            }
                                        }
                                        else
                                        {
                                            gatewayMessage = $"Tamara rejected the refund of {refundAmount:C}. " +
                                                           $"The order details could not be retrieved to verify the remaining refundable amount. " +
                                                           $"Please check the order status in Tamara dashboard and try again.";
                                        }
                                    }
                                    else
                                    {
                                        gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount appears to be {remainingRefundableAmount.Value:C}. " +
                                                       $"Unable to fetch fresh order details. " +
                                                       $"Please check the order status in Tamara dashboard for the current remaining refundable amount, " +
                                                       $"or try refreshing the order details and attempt the refund again.";
                                    }
                                }
                                catch (Exception refreshEx)
                                {
                                    _logger.LogError(refreshEx, $"Error fetching fresh order details after refund rejection");
                                    gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount appears to be {remainingRefundableAmount.Value:C}. " +
                                                   $"This may be due to a recent refund or a timing issue. " +
                                                   $"Please check the order status in Tamara dashboard for the current remaining refundable amount, " +
                                                   $"or try refreshing the order details and attempt the refund again.";
                                }
                            }
                            else
                            {
                                // Amount exceeds what we calculated
                                var message = $"Cannot refund {refundAmount:C}. ";
                                
                                if (remainingRefundableAmount.HasValue)
                                {
                                    message += $"The remaining refundable amount is {remainingRefundableAmount.Value:C}. " +
                                             $"Please enter a refund amount that does not exceed {remainingRefundableAmount.Value:C}.";
                                }
                                else
                                {
                                    message += "The refund amount exceeds the remaining refundable amount. " +
                                             "This order may have already been partially refunded. " +
                                             "Please check the order status in Tamara dashboard to see the remaining refundable amount, " +
                                             "or try a smaller refund amount.";
                                }
                                
                                gatewayMessage = message;
                            }
                        }
                        else
                        {
                            // Clean up error message - remove raw type names
                            var cleanMessage = refundResponse.Message;
                            if (cleanMessage != null && cleanMessage.Contains("Tamara.Net.SDK.Models.Exception.ErrorResult"))
                            {
                                cleanMessage = cleanMessage.Replace("Tamara.Net.SDK.Models.Exception.ErrorResult", "").Trim();
                                if (cleanMessage.StartsWith(":"))
                                {
                                    cleanMessage = cleanMessage.Substring(1).Trim();
                                }
                                
                                if (string.IsNullOrWhiteSpace(cleanMessage))
                                {
                                    cleanMessage = "Tamara refund failed. Please check the order status in Tamara dashboard.";
                                }
                            }
                            
                            gatewayMessage = $"Tamara refund failed: {cleanMessage}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error refunding Tamara payment for order {id}");
                    
                    // Check if exception message contains the refund amount error
                    if (ex.Message != null && 
                        (ex.Message.Contains("refund_amount_greater_than_remaining_captured_amount", StringComparison.OrdinalIgnoreCase) ||
                         (ex.Message.Contains("remaining", StringComparison.OrdinalIgnoreCase) && 
                          ex.Message.Contains("refund", StringComparison.OrdinalIgnoreCase))))
                    {
                        // If we validated the amount before and it passed, but Tamara still rejects it,
                        // there might be a discrepancy. Try to get fresh order details to see actual remaining amount.
                        if (remainingRefundableAmount.HasValue && refundAmount <= remainingRefundableAmount.Value)
                        {
                            // Amount was validated but still rejected - get fresh order details to check actual remaining amount
                            _logger.LogWarning($"Tamara exception for refund of {refundAmount:C} despite validation showing {remainingRefundableAmount.Value:C} remaining. Fetching fresh order details...");
                            
                            try
                            {
                                var tamaraHelper = new TamaraHelper(_tamaraSettings);
                                var freshOrderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);
                                if (freshOrderDetails.Success)
                                {
                                    decimal? actualRemaining = null;
                                    if (freshOrderDetails.RemainingRefundableAmount.HasValue)
                                    {
                                        actualRemaining = freshOrderDetails.RemainingRefundableAmount.Value;
                                    }
                                    else if (freshOrderDetails.TotalAmount.HasValue && freshOrderDetails.RefundedAmount.HasValue)
                                    {
                                        actualRemaining = freshOrderDetails.TotalAmount.Value - freshOrderDetails.RefundedAmount.Value;
                                    }
                                    
                                    if (actualRemaining.HasValue)
                                    {
                                        if (refundAmount > actualRemaining.Value)
                                        {
                                            gatewayMessage = $"Cannot refund {refundAmount:C}. " +
                                                           $"The actual remaining refundable amount is {actualRemaining.Value:C} (not {remainingRefundableAmount.Value:C} as initially shown). " +
                                                           $"Please enter a refund amount that does not exceed {actualRemaining.Value:C}.";
                                        }
                                        else
                                        {
                                            gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount is {actualRemaining.Value:C}. " +
                                                           $"This may be due to a timing issue or Tamara's internal validation. " +
                                                           $"Please try again in a few moments, or check the order status in Tamara dashboard.";
                                        }
                                    }
                                    else
                                    {
                                        gatewayMessage = $"Tamara rejected the refund of {refundAmount:C}. " +
                                                       $"The order details could not be retrieved to verify the remaining refundable amount. " +
                                                       $"Please check the order status in Tamara dashboard and try again.";
                                    }
                                }
                                else
                                {
                                    gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount appears to be {remainingRefundableAmount.Value:C}. " +
                                                   $"Unable to fetch fresh order details. " +
                                                   $"Please check the order status in Tamara dashboard for the current remaining refundable amount, " +
                                                   $"or try refreshing the order details and attempt the refund again.";
                                }
                            }
                            catch (Exception refreshEx)
                            {
                                _logger.LogError(refreshEx, $"Error fetching fresh order details after refund exception");
                                gatewayMessage = $"Tamara rejected the refund of {refundAmount:C} even though the remaining refundable amount appears to be {remainingRefundableAmount.Value:C}. " +
                                               $"This may be due to a recent refund or a timing issue. " +
                                               $"Please check the order status in Tamara dashboard for the current remaining refundable amount, " +
                                               $"or try refreshing the order details and attempt the refund again.";
                            }
                        }
                        else
                        {
                            var message = $"Cannot refund {refundAmount:C}. ";
                            
                            if (remainingRefundableAmount.HasValue)
                            {
                                message += $"The remaining refundable amount is {remainingRefundableAmount.Value:C}. " +
                                         $"Please enter a refund amount that does not exceed {remainingRefundableAmount.Value:C}.";
                            }
                            else
                            {
                                message += "The refund amount exceeds the remaining refundable amount. " +
                                         "This order may have already been partially refunded. " +
                                         "Please check the order status in Tamara dashboard to see the remaining refundable amount, " +
                                         "or try a smaller refund amount.";
                            }
                            
                            gatewayMessage = message;
                        }
                    }
                    else
                    {
                        // Clean up error message - remove raw type names
                        var cleanMessage = ex.Message;
                        if (cleanMessage != null && cleanMessage.Contains("Tamara.Net.SDK.Models.Exception.ErrorResult"))
                        {
                            cleanMessage = cleanMessage.Replace("Tamara.Net.SDK.Models.Exception.ErrorResult", "").Trim();
                            if (cleanMessage.StartsWith(":"))
                            {
                                cleanMessage = cleanMessage.Substring(1).Trim();
                            }
                            
                            if (string.IsNullOrWhiteSpace(cleanMessage))
                            {
                                cleanMessage = "An error occurred while processing the refund. Please check the order status in Tamara dashboard.";
                            }
                        }
                        
                        gatewayMessage = $"Tamara refund error: {cleanMessage}";
                    }
                }
            }
            // Refund with Geidea
            else if (orderHeader.PaymentMethod == SD.PaymentMethodGeidea && 
                     !string.IsNullOrEmpty(orderHeader.SessionId) &&
                     !string.IsNullOrEmpty(_geideaSettings.MerchantPublicKey))
            {
                try
                {
                    var geideaHelper = new GeideaHelper(_geideaSettings);
                    var refundResponse = await geideaHelper.RefundPaymentAsync(
                        orderHeader.Id.ToString(),
                        refundAmount,
                        "AED", // Geidea default currency
                        orderHeader.SessionId,
                        refundReason);
                    
                    if (refundResponse.Success)
                    {
                        gatewayRefundSuccess = true;
                        gatewayMessage = $"Refund of {refundAmount:C} processed successfully with Geidea";
                        _logger.LogInformation($"Geidea order {orderHeader.SessionId} refunded successfully. Amount: {refundAmount}");
                    }
                    else
                    {
                        _logger.LogWarning($"Geidea refund failed for order {id}: {refundResponse.Message}");
                        gatewayMessage = $"Geidea refund failed: {refundResponse.Message}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error refunding Geidea payment for order {id}");
                    gatewayMessage = $"Geidea refund error: {ex.Message}";
                }
            }
            // Refund with Tappy/Tabby
            else if (orderHeader.PaymentMethod == SD.PaymentMethodTappy && 
                     !string.IsNullOrEmpty(orderHeader.PaymentIntentId) &&
                     _tappySettings.Enabled)
            {
                try
                {
                    var tappyHelper = new TappyHelper(_tappySettings);
                    var refundResponse = await tappyHelper.RefundPaymentAsync(
                        orderHeader.PaymentIntentId,
                        refundAmount,
                        "AED", // Tabby default currency
                        refundReason);
                    
                    if (refundResponse.Success)
                    {
                        gatewayRefundSuccess = true;
                        gatewayMessage = $"Refund of {refundAmount:C} processed successfully with Tabby";
                        _logger.LogInformation($"Tabby order {orderHeader.PaymentIntentId} refunded successfully. Amount: {refundAmount}");
                    }
                    else
                    {
                        _logger.LogWarning($"Tabby refund failed for order {id}: {refundResponse.Message}");
                        gatewayMessage = $"Tabby refund failed: {refundResponse.Message}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error refunding Tabby payment for order {id}");
                    gatewayMessage = $"Tabby refund error: {ex.Message}";
                }
            }

            // Update order status based on refund result
            var oldPaymentStatus = orderHeader.PaymentStatus;
            var oldOrderStatus = orderHeader.OrderStatus;
            
            if (gatewayRefundSuccess)
            {
                if (isFullRefund)
                {
                    orderHeader.OrderStatus = SD.StatusRefunded;
                    orderHeader.PaymentStatus = SD.PaymentStatusRefunded;
                }
                else
                {
                    // Partial refund - update payment status but keep order status
                    orderHeader.PaymentStatus = SD.PaymentStatusPartiallyRefunded;
                }
                
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.save();

                // Log audit trail
                LogAuditAction(id, "OrderRefunded",
                    $"Order refunded. Amount: {refundAmount:C}, Full: {isFullRefund}, Gateway: {orderHeader.PaymentMethod}, Reason: {refundReason}",
                    oldOrderStatus, orderHeader.OrderStatus, oldPaymentStatus, orderHeader.PaymentStatus);

                // Return JSON for AJAX requests
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new 
                    { 
                        success = true, 
                        message = gatewayMessage,
                        orderStatus = orderHeader.OrderStatus,
                        paymentStatus = orderHeader.PaymentStatus,
                        isFullRefund = isFullRefund
                    });
                }
                
                TempData["success"] = gatewayMessage;
            }
            else
            {
                // Gateway refund failed - don't update status, show error
                // Only append "Please process refund manually" if the message doesn't already provide clear guidance
                var errorMessage = !string.IsNullOrEmpty(gatewayMessage)
                    ? (gatewayMessage.Contains("check the order status") || 
                       gatewayMessage.Contains("try again") || 
                       gatewayMessage.Contains("try a smaller") ||
                       gatewayMessage.Contains("does not exceed") ||
                       gatewayMessage.Contains("timing issue"))
                        ? gatewayMessage // Message already provides clear guidance, don't append
                        : $"{gatewayMessage}. Please process refund manually in payment gateway dashboard if the issue persists."
                    : "Refund processing failed. Please process refund manually in payment gateway dashboard.";
                
                // Return JSON for AJAX requests
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new 
                    { 
                        success = false, 
                        message = errorMessage,
                        error = true
                    });
                }
                
                TempData["error"] = errorMessage;
                return RedirectToAction(nameof(Details), new { id = id });
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        #region PAYMENT RECONCILIATION

        /// <summary>
        /// Manually recheck payment status with payment provider API
        /// </summary>
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> RecheckPaymentStatus(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            var oldPaymentStatus = orderHeader.PaymentStatus;
            var oldOrderStatus = orderHeader.OrderStatus;

            try
            {
                bool paymentVerified = false;
                string verificationMessage = "";
                string paymentStatus = "";

                // Verify payment based on payment method
                switch (orderHeader.PaymentMethod)
                {
                    case SD.PaymentMethodGeidea:
                        var geideaHelper = new GeideaHelper(_geideaSettings);
                        var merchantRefId = orderHeader.Id.ToString();
                        var geideaResult = await geideaHelper.VerifyPaymentAsync(merchantRefId);

                        if (geideaResult.Success)
                        {
                            paymentVerified = geideaResult.IsPaid;
                            paymentStatus = geideaResult.Status ?? "Unknown";
                            verificationMessage = geideaResult.Message ?? "Verification completed";
                        }
                        else
                        {
                            verificationMessage = geideaResult.Message ?? "Verification failed";
                        }
                        break;

                    case SD.PaymentMethodTamara:
                        if (!string.IsNullOrEmpty(orderHeader.PaymentIntentId))
                        {
                            var tamaraHelper = new TamaraHelper(_tamaraSettings);
                            var tamaraResult = await tamaraHelper.GetOrderDetailsAsync(orderHeader.PaymentIntentId);

                            if (tamaraResult.Success)
                            {
                                paymentVerified = tamaraResult.Status?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.Status?.Equals("authorized", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.Status?.Equals("authorised", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.Status?.Equals("fully_refunded", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.Status?.Equals("partially_refunded", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.Status?.Equals("canceled", StringComparison.OrdinalIgnoreCase) == true ||
                                                 tamaraResult.PaymentStatus?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true;
                                paymentStatus = tamaraResult.Status ?? "Unknown";
                                verificationMessage = tamaraResult.Message ?? "Verification completed";
                            }
                            else
                            {
                                verificationMessage = tamaraResult.Message ?? "Verification failed";
                            }
                        }
                        break;

                    case SD.PaymentMethodTappy:
                        if (!string.IsNullOrEmpty(orderHeader.SessionId))
                        {
                            var tappyHelper = new TappyHelper(_tappySettings);
                            var tappyResult = await tappyHelper.VerifyPaymentAsync(orderHeader.SessionId);

                            if (tappyResult.Success)
                            {
                                paymentVerified = tappyResult.IsPaid;
                                paymentStatus = tappyResult.Status ?? "Unknown";
                                verificationMessage = tappyResult.Message ?? "Verification completed";
                            }
                            else
                            {
                                verificationMessage = tappyResult.Message ?? "Verification failed";
                            }
                        }
                        break;

                    default:
                        TempData["warning"] = $"Payment verification not supported for payment method: {orderHeader.PaymentMethod}";
                        return RedirectToAction(nameof(Details), new { id });
                }

                // Update order status based on verification
                if (paymentVerified)
                {
                    // Update order status directly
                    orderHeader.PaymentStatus = paymentStatus;
                    if(orderHeader.OrderStatus == SD.PaymentStatusPending)
                        orderHeader.OrderStatus = SD.PaymentStatusPaid;
                    orderHeader.PaymentDate = DateTimeHelper.Now;
                    
                    _unitOfWork.OrderHeader.Update(orderHeader);
                    _unitOfWork.save();

                    // Log audit trail
                    LogAuditAction(id, "PaymentVerified", 
                        $"Payment verified via {orderHeader.PaymentMethod} API. Status: {paymentStatus}. Message: {verificationMessage}",
                        oldOrderStatus, SD.StatusApproved, oldPaymentStatus, SD.PaymentStatusPaid);

                    TempData["success"] = $"Payment verified successfully! Order status updated to Approved/Paid. Details: {verificationMessage}";
                }
                else
                {
                    LogAuditAction(id, "PaymentVerificationFailed",
                        $"Payment verification via {orderHeader.PaymentMethod} API failed. Status: {paymentStatus}. Message: {verificationMessage}",
                        oldOrderStatus, oldOrderStatus, oldPaymentStatus, oldPaymentStatus);

                    TempData["warning"] = $"Payment not verified. Status: {paymentStatus}. Details: {verificationMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rechecking payment status for order {id}");
                LogAuditAction(id, "PaymentVerificationError", $"Error verifying payment: {ex.Message}",
                    oldOrderStatus, oldOrderStatus, oldPaymentStatus, oldPaymentStatus);
                TempData["error"] = $"Error verifying payment: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Force mark order as complete (admin override)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ForceComplete(int id, string reason)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            var oldPaymentStatus = orderHeader.PaymentStatus;
            var oldOrderStatus = orderHeader.OrderStatus;

            try
            {
                // Update order status directly
                orderHeader.OrderStatus = SD.StatusApproved;
                orderHeader.PaymentStatus = SD.PaymentStatusPaid;
                orderHeader.PaymentDate = DateTimeHelper.Now;
                
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.save();

                LogAuditAction(id, "ForceComplete",
                    $"Order force completed by admin. Reason: {reason ?? "No reason provided"}. Payment Method: {orderHeader.PaymentMethod}",
                    oldOrderStatus, SD.StatusApproved, oldPaymentStatus, SD.PaymentStatusPaid);

                TempData["success"] = "Order force completed successfully. Payment marked as paid.";
                _logger.LogWarning($"Order {id} force completed by admin. Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error force completing order {id}");
                TempData["error"] = $"Error completing order: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Force cancel order with audit logging
        /// </summary>
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ForceCancelOrder(int id, string reason)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            var oldPaymentStatus = orderHeader.PaymentStatus;
            var oldOrderStatus = orderHeader.OrderStatus;

            try
            {
                // Update order status directly
                orderHeader.OrderStatus = SD.StatusCancelled;
                orderHeader.PaymentStatus = SD.PaymentStatusRejected;
                
                _unitOfWork.OrderHeader.Update(orderHeader);
                _unitOfWork.save();

                LogAuditAction(id, "ForceCancelled",
                    $"Order force cancelled by admin. Reason: {reason ?? "No reason provided"}. Payment Method: {orderHeader.PaymentMethod}",
                    oldOrderStatus, SD.StatusCancelled, oldPaymentStatus, SD.PaymentStatusRejected);

                TempData["success"] = "Order cancelled successfully.";
                _logger.LogWarning($"Order {id} force cancelled by admin. Reason: {reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error force cancelling order {id}");
                TempData["error"] = $"Error cancelling order: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Helper method to log audit actions
        /// </summary>
        private void LogAuditAction(int orderHeaderId, string action, string? actionDetails = null,
            string? oldOrderStatus = null, string? newOrderStatus = null,
            string? oldPaymentStatus = null, string? newPaymentStatus = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var auditLog = new OrderAuditLog
                {
                    OrderHeaderId = orderHeaderId,
                    Action = action,
                    ActionDetails = actionDetails,
                    PerformedByUserId = userId,
                    PerformedByUserEmail = userEmail,
                    OldOrderStatus = oldOrderStatus,
                    NewOrderStatus = newOrderStatus,
                    OldPaymentStatus = oldPaymentStatus,
                    NewPaymentStatus = newPaymentStatus,
                    ActionDate = DateTimeHelper.Now,
                    IpAddress = ipAddress?.Length > 45 ? ipAddress.Substring(0, 45) : ipAddress,
                    UserAgent = userAgent?.Length > 500 ? userAgent.Substring(0, 500) : userAgent
                };

                _unitOfWork.OrderAuditLog.Add(auditLog);
                _unitOfWork.save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating audit log for order {orderHeaderId}");
            }
        }

        #endregion

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(
            string status,
            string paymentStatus = "",
            string paymentMethod = "",
            string dateFrom = "",
            string dateTo = "",
            string searchValue = "",
            int start = 0,
            int length = 10,
            string sortColumn = "Id",
            string sortDirection = "desc")
        {
            try
            {
                // Start with base query
                IQueryable<OrderHeader> query = _unitOfWork.OrderHeader.GetAll().AsQueryable();

                // Apply order status filter
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(o => o.OrderStatus == status);
                }

                // Apply payment status filter
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    query = query.Where(o => o.PaymentStatus == paymentStatus);
                }

                // Apply payment method filter
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(o => o.PaymentMethod == paymentMethod);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateFrom))
                {
                    if (DateTime.TryParse(dateFrom, out var fromDate))
                    {
                        query = query.Where(o => o.OrderDate >= fromDate.Date);
                    }
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    if (DateTime.TryParse(dateTo, out var toDate))
                    {
                        // Include the entire day
                        query = query.Where(o => o.OrderDate <= toDate.Date.AddDays(1).AddTicks(-1));
                    }
                }

                // Apply search filter (search across multiple fields)
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(o =>
                        o.Id.ToString().Contains(searchValue) ||
                        (o.Name != null && o.Name.ToLower().Contains(searchValue)) ||
                        (o.Email != null && o.Email.ToLower().Contains(searchValue)) ||
                        (o.PhoneNumber != null && o.PhoneNumber.Contains(searchValue)) ||
                        (o.OrderStatus != null && o.OrderStatus.ToLower().Contains(searchValue)) ||
                        (o.PaymentStatus != null && o.PaymentStatus.ToLower().Contains(searchValue)) ||
                        (o.PaymentMethod != null && o.PaymentMethod.ToLower().Contains(searchValue)) ||
                        o.OrderTotal.ToString().Contains(searchValue)
                    );
                }

                // Get total count before pagination
                var totalRecords = query.Count();

                // Apply sorting
                query = sortColumn.ToLower() switch
                {
                    "id" => sortDirection == "asc" ? query.OrderBy(o => o.Id) : query.OrderByDescending(o => o.Id),
                    "name" => sortDirection == "asc" ? query.OrderBy(o => o.Name) : query.OrderByDescending(o => o.Name),
                    "orderdate" => sortDirection == "asc" ? query.OrderBy(o => o.OrderDate) : query.OrderByDescending(o => o.OrderDate),
                    "ordertotal" => sortDirection == "asc" ? query.OrderBy(o => o.OrderTotal) : query.OrderByDescending(o => o.OrderTotal),
                    "orderstatus" => sortDirection == "asc" ? query.OrderBy(o => o.OrderStatus) : query.OrderByDescending(o => o.OrderStatus),
                    "paymentstatus" => sortDirection == "asc" ? query.OrderBy(o => o.PaymentStatus) : query.OrderByDescending(o => o.PaymentStatus),
                    _ => query.OrderByDescending(o => o.Id)
                };

                // Apply pagination
                var orders = query
                    .Skip(start)
                    .Take(length)
                    .ToList();

                // Load ApplicationUser only for non-guest orders (for the current page only)
                foreach (var order in orders)
                {
                    if (!order.IsGuestOrder && !string.IsNullOrEmpty(order.ApplicationUserId))
                    {
                        order.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == order.ApplicationUserId);
                    }
                }

                // Map to lowercase properties for Tabulator
                var orderData = orders.Select(o => new
                {
                    id = o.Id,
                    name = o.Name,
                    email = o.Email,
                    phoneNumber = o.PhoneNumber,
                    orderDate = o.OrderDate,
                    paymentMethod=o.PaymentMethod,
                    orderSubtotal = o.OrderSubtotal ?? 0,
                    orderTotal = o.OrderTotal,
                    vatAmount = CalculateVAT(o.OrderSubtotal, o.OrderTotal),
                    orderStatus = o.OrderStatus,
                    paymentStatus = o.PaymentStatus,
                    isGuestOrder = o.IsGuestOrder,
                    applicationUser = o.ApplicationUser != null ? new
                    {
                        email = o.ApplicationUser.Email,
                        name = o.ApplicationUser.Name
                    } : null
                }).ToList();

                // Return data in Tabulator format
                return Json(new
                {
                    last_page = (int)Math.Ceiling((double)totalRecords / length),
                    data = orderData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                return Json(new { error = "Error loading orders" });
            }
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult GetAuditLog(int id)
        {
            try
            {
                var auditLogs = _unitOfWork.OrderAuditLog
                    .GetAll(log => log.OrderHeaderId == id)
                    .OrderByDescending(log => log.ActionDate)
                    .Select(log => new
                    {
                        log.Id,
                        log.OrderHeaderId,
                        log.Action,
                        log.ActionDetails,
                        log.PerformedByUserId,
                        log.PerformedByUserEmail,
                        log.OldOrderStatus,
                        log.NewOrderStatus,
                        log.OldPaymentStatus,
                        log.NewPaymentStatus,
                        log.ActionDate,
                        log.IpAddress,
                        log.UserAgent
                    })
                    .ToList();

                return Json(new { success = true, logs = auditLogs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading audit log for order {id}");
                return Json(new { success = false, message = "Error loading audit log" });
            }
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult GetOrderStatistics()
        {
            try
            {
                var allOrders = _unitOfWork.OrderHeader.GetAll().ToList();

                var stats = new
                {
                    all = allOrders.Count,
                    pending = allOrders.Count(o => o.OrderStatus == SD.StatusPending),
                    approved = allOrders.Count(o => o.OrderStatus == SD.StatusApproved),
                    processing = allOrders.Count(o => o.OrderStatus == SD.StatusInProcess),
                    shipped = allOrders.Count(o => o.OrderStatus == SD.StatusShipped),
                    delivered = allOrders.Count(o => o.OrderStatus == SD.StatusDelivered),
                    cancelled = allOrders.Count(o => o.OrderStatus == SD.StatusCancelled)
                };

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order statistics");
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        /// <summary>
        /// Calculate VAT amount (5% of subtotal)
        /// If subtotal is not available, calculate from total (reverse calculation)
        /// </summary>
        private double CalculateVAT(double? orderSubtotal, double orderTotal)
        {
            const double vatRate = 0.05; // 5% VAT rate
         
            if (orderSubtotal.HasValue && orderSubtotal.Value > 0)
            {
                return (double)(orderSubtotal * (vatRate / (1 + vatRate)));
            }
            else
            {
                

                return (double)(orderTotal * (vatRate / (1 + vatRate))); ;
            }
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Export(
            string status,
            string paymentStatus = "",
            string paymentMethod = "",
            string dateFrom = "",
            string dateTo = "",
            string searchValue = "")
        {
            try
            {
                // Start with base query (same as GetAll)
                IQueryable<OrderHeader> query = _unitOfWork.OrderHeader.GetAll().AsQueryable();

                // Apply order status filter
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(o => o.OrderStatus == status);
                }

                // Apply payment status filter
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    query = query.Where(o => o.PaymentStatus == paymentStatus);
                }

                // Apply payment method filter
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query = query.Where(o => o.PaymentMethod == paymentMethod);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateFrom))
                {
                    if (DateTime.TryParse(dateFrom, out var fromDate))
                    {
                        query = query.Where(o => o.OrderDate >= fromDate.Date);
                    }
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    if (DateTime.TryParse(dateTo, out var toDate))
                    {
                        query = query.Where(o => o.OrderDate <= toDate.Date.AddDays(1).AddTicks(-1));
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(o =>
                        o.Id.ToString().Contains(searchValue) ||
                        (o.Name != null && o.Name.ToLower().Contains(searchValue)) ||
                        (o.Email != null && o.Email.ToLower().Contains(searchValue)) ||
                        (o.PhoneNumber != null && o.PhoneNumber.Contains(searchValue)) ||
                        (o.OrderStatus != null && o.OrderStatus.ToLower().Contains(searchValue)) ||
                        (o.PaymentStatus != null && o.PaymentStatus.ToLower().Contains(searchValue)) ||
                        (o.PaymentMethod != null && o.PaymentMethod.ToLower().Contains(searchValue)) ||
                        o.OrderTotal.ToString().Contains(searchValue)
                    );
                }

                // Get all orders (no pagination for export)
                var orders = query.OrderByDescending(o => o.Id).ToList();

                // Load ApplicationUser for non-guest orders
                foreach (var order in orders)
                {
                    if (!order.IsGuestOrder && !string.IsNullOrEmpty(order.ApplicationUserId))
                    {
                        order.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == order.ApplicationUserId);
                    }
                }

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Order ID,Customer Name,Email,Phone,Order Date,Payment Method,Total Without VAT,VAT Amount,Total Inc VAT,Total(include delivery),Order Status,Payment Status");

                foreach (var order in orders)
                {
                    var subtotal = order.OrderSubtotal ?? 0;
                    var vatAmount = CalculateVAT(order.OrderSubtotal, order.OrderTotal);
                    var totalWithoutVat = subtotal - vatAmount;
                    
                    csv.AppendLine($"{order.Id}," +
                        $"\"{order.Name?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{order.Email?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{order.PhoneNumber?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"{order.OrderDate:yyyy-MM-dd HH:mm:ss}," +
                        $"\"{order.PaymentMethod?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"{totalWithoutVat:F2}," +
                        $"{vatAmount:F2}," +
                        $"{subtotal:F2}," +
                        $"{order.OrderTotal:F2}," +
                        $"\"{order.OrderStatus?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{order.PaymentStatus?.Replace("\"", "\"\"") ?? ""}\"");
                }

                var fileName = $"Orders_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders");
                TempData["error"] = "Error exporting orders";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ExportProductProfits(
            string dateFrom = "",
            string dateTo = "")
        {
            try
            {
                // Get successful orders (Paid, Approved, or Authorized payment status)
                IQueryable<OrderHeader> query = _unitOfWork.OrderHeader.GetAll()
                    .Where(o => o.PaymentStatus == SD.PaymentStatusPaid || 
                               o.PaymentStatus == SD.PaymentStatusDelayedPayment || 
                               o.PaymentStatus == SD.PaymentStatusAuthorized)
                    .AsQueryable();

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateFrom))
                {
                    if (DateTime.TryParse(dateFrom, out var fromDate))
                    {
                        query = query.Where(o => o.OrderDate >= fromDate.Date);
                    }
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    if (DateTime.TryParse(dateTo, out var toDate))
                    {
                        query = query.Where(o => o.OrderDate <= toDate.Date.AddDays(1).AddTicks(-1));
                    }
                }

                var successfulOrders = query.ToList();
                var orderIds = successfulOrders.Select(o => o.Id).ToList();

                // Get all order details for successful orders with product information
                var orderDetails = _unitOfWork.OrderDetail.GetAll(
                    od => orderIds.Contains(od.OrderHeaderId),
                    includeProperties: "Product,OrderHeader"
                ).ToList();

                // Filter products that have StoreCost and group by Product
                var productProfits = orderDetails
                    .Where(od => od.Product != null && 
                                od.Product.StoreCost.HasValue && 
                                od.Product.StoreCost.Value > 0)
                    .GroupBy(od => new
                    {
                        ProductId = od.ProductId,
                        ProductTitle = od.Product.Title ?? "N/A",
                        ProductTitleAr = od.Product.TitleAr ?? "N/A",
                        StoreCost = od.Product.StoreCost.Value
                    })
                    .Select(g => new
                    {
                        ProductId = g.Key.ProductId,
                        ProductTitle = g.Key.ProductTitle,
                        ProductTitleAr = g.Key.ProductTitleAr,
                        StoreCost = g.Key.StoreCost,
                        TotalQuantitySold = g.Sum(od => od.Count),
                        TotalRevenue = g.Sum(od => od.Price * od.Count),
                        TotalCost = g.Sum(od => g.Key.StoreCost * od.Count),
                        TotalProfit = g.Sum(od => (od.Price - g.Key.StoreCost) * od.Count),
                        AverageSellingPrice = g.Average(od => od.Price),
                        ProfitPerUnit = g.Average(od => od.Price - g.Key.StoreCost),
                        ProfitPercentage = g.Average(od => od.Price > 0 ? ((od.Price - g.Key.StoreCost) / od.Price) * 100 : 0),
                        OrderCount = g.Select(od => od.OrderHeaderId).Distinct().Count()
                    })
                    .OrderByDescending(p => p.TotalProfit)
                    .ToList();

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Product ID,Product Title (EN),Product Title (AR),Store Cost,Total Quantity Sold,Total Revenue,Total Cost,Total Profit,Average Selling Price,Profit Per Unit,Profit %,Number of Orders");

                foreach (var profit in productProfits)
                {
                    csv.AppendLine($"{profit.ProductId}," +
                        $"\"{profit.ProductTitle?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{profit.ProductTitleAr?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"{profit.StoreCost:F2}," +
                        $"{profit.TotalQuantitySold}," +
                        $"{profit.TotalRevenue:F2}," +
                        $"{profit.TotalCost:F2}," +
                        $"{profit.TotalProfit:F2}," +
                        $"{profit.AverageSellingPrice:F2}," +
                        $"{profit.ProfitPerUnit:F2}," +
                        $"{profit.ProfitPercentage:F2}," +
                        $"{profit.OrderCount}");
                }

                // Add summary row
                csv.AppendLine();
                csv.AppendLine("SUMMARY");
                csv.AppendLine($"Total Products,{productProfits.Count}");
                csv.AppendLine($"Total Quantity Sold,{productProfits.Sum(p => p.TotalQuantitySold)}");
                csv.AppendLine($"Total Revenue,{productProfits.Sum(p => p.TotalRevenue):F2}");
                csv.AppendLine($"Total Cost,{productProfits.Sum(p => p.TotalCost):F2}");
                csv.AppendLine($"Total Profit,{productProfits.Sum(p => p.TotalProfit):F2}");
                csv.AppendLine($"Total Orders,{successfulOrders.Count}");

                var fileName = $"Product_Profits_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting product profits");
                TempData["error"] = "Error exporting product profits";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion
    }
}

