using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Services
{
    /// <summary>
    /// Background service that runs every 5 minutes to verify pending payment orders
    /// Checks orders older than 20 minutes and verifies their payment status with the payment provider
    /// </summary>
    public class PaymentVerificationBackgroundService : BackgroundService
    {
        private readonly ILogger<PaymentVerificationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _pendingOrderThreshold = TimeSpan.FromMinutes(20);

        public PaymentVerificationBackgroundService(
            ILogger<PaymentVerificationBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment Verification Background Service started at {Time}", DateTimeHelper.Now);

            // Wait a bit before first execution to allow application to fully start
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting payment verification job at {Time}", DateTimeHelper.Now);
                    await VerifyPendingPaymentsAsync(stoppingToken);
                    _logger.LogInformation("Completed payment verification job at {Time}", DateTimeHelper.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while verifying pending payments at {Time}", DateTimeHelper.Now);
                }

                // Wait for next execution
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Payment Verification Background Service stopped at {Time}", DateTimeHelper.Now);
        }

        private async Task VerifyPendingPaymentsAsync(CancellationToken stoppingToken)
        {
            // Create a new scope for each execution to get fresh DbContext
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var geideaSettings = scope.ServiceProvider.GetRequiredService<IOptions<GeideaSettings>>().Value;
            var tamaraSettings = scope.ServiceProvider.GetRequiredService<IOptions<TamaraSettings>>().Value;
            var tappySettings = scope.ServiceProvider.GetRequiredService<IOptions<TappySettings>>().Value;

            // Calculate cutoff time (20 minutes ago)
            var cutoffTime = DateTimeHelper.Now.AddMinutes(-20);

            _logger.LogInformation("Fetching orders with PaymentStatus=Pending and OrderDate < {CutoffTime}", cutoffTime);

            // Fetch pending payment orders older than 20 minutes using direct DbContext
            var pendingOrders = await db.orderHeaders
                .Where(o => o.PaymentStatus == SD.PaymentStatusPending && o.OrderDate < cutoffTime)
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            if (!pendingOrders.Any())
            {
                _logger.LogInformation("No pending orders found older than {Minutes} minutes", 20);
                return;
            }

            _logger.LogInformation("Found {Count} pending orders to verify", pendingOrders.Count);

            // Group orders by payment method
            var ordersByPaymentMethod = pendingOrders
                .GroupBy(o => o.PaymentMethod ?? "Unknown")
                .ToList();

            foreach (var group in ordersByPaymentMethod)
            {
                var paymentMethod = group.Key;
                var orders = group.ToList();

                _logger.LogInformation("Processing {Count} orders for payment method: {PaymentMethod}", orders.Count, paymentMethod);

                foreach (var order in orders)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await VerifyAndUpdateOrderAsync(order, paymentMethod, db, geideaSettings, tamaraSettings, tappySettings);
                }
            }
        }

        private async Task VerifyAndUpdateOrderAsync(
            OrderHeader order,
            string paymentMethod,
            ApplicationDBContext db,
            GeideaSettings geideaSettings,
            TamaraSettings tamaraSettings,
            TappySettings tappySettings)
        {
            try
            {
                _logger.LogInformation("Verifying Order #{OrderId} (Ref: {SessionId}) via {PaymentMethod}", 
                    order.Id, order.SessionId ?? order.PaymentIntentId ?? "N/A", paymentMethod);

                bool isPaid = false;
                bool orderNotFoundInGateway = false;
                string verificationStatus = "Unknown";
                string verificationMessage = "";

                // Verify payment based on payment method
                switch (paymentMethod)
                {
                    case SD.PaymentMethodGeidea:
                        var geideaResult = await VerifyGeideaPaymentAsync(order, geideaSettings);
                        isPaid = geideaResult.IsPaid;
                        orderNotFoundInGateway = geideaResult.NotFound;
                        verificationStatus = geideaResult.Status;
                        verificationMessage = geideaResult.Message;
                        break;

                    case SD.PaymentMethodTamara:
                        var tamaraResult = await VerifyTamaraPaymentAsync(order, tamaraSettings);
                        isPaid = tamaraResult.IsPaid;
                        orderNotFoundInGateway = tamaraResult.NotFound;
                        verificationStatus = tamaraResult.Status;
                        verificationMessage = tamaraResult.Message;
                        break;

                    case SD.PaymentMethodTappy:
                        var tappyResult = await VerifyTappyPaymentAsync(order, tappySettings);
                        isPaid = tappyResult.IsPaid;
                        orderNotFoundInGateway = tappyResult.NotFound;
                        verificationStatus = tappyResult.Status;
                        verificationMessage = tappyResult.Message;
                        break;

                    default:
                        _logger.LogWarning("Order #{OrderId} has unknown payment method: {PaymentMethod}. Skipping verification.", 
                            order.Id, paymentMethod);
                        return;
                }

                // Update order based on verification result
                if (isPaid)
                {
                    _logger.LogInformation("Order #{OrderId} payment VERIFIED. Marking as Approved/Paid. Status: {Status}, Message: {Message}", 
                        order.Id, verificationStatus, verificationMessage);

                    // Update order and payment status using direct DbContext with tracking enabled
                    var orderFromDb = await db.orderHeaders
                        .AsTracking()
                        .FirstOrDefaultAsync(u => u.Id == order.Id);
                    
                    if (orderFromDb != null)
                    {
                        orderFromDb.OrderStatus = SD.StatusApproved;
                        orderFromDb.PaymentStatus = SD.PaymentStatusPaid;
                        orderFromDb.PaymentDate = DateTimeHelper.Now;
                        await db.SaveChangesAsync();
                        
                        _logger.LogInformation("Order #{OrderId} successfully updated to Approved/Paid in database", order.Id);
                    }
                    else
                    {
                        _logger.LogError("Order #{OrderId} not found in database for update", order.Id);
                    }
                }
                else if (orderNotFoundInGateway)
                {
                    // Order doesn't exist in payment gateway - cancel immediately
                    _logger.LogWarning("Order #{OrderId} does NOT exist in {PaymentMethod} gateway. Cancelling immediately. Message: {Message}", 
                        order.Id, paymentMethod, verificationMessage);

                    var orderFromDb = await db.orderHeaders
                        .AsTracking()
                        .FirstOrDefaultAsync(u => u.Id == order.Id);
                    
                    if (orderFromDb != null)
                    {
                        orderFromDb.OrderStatus = SD.StatusCancelled;
                        orderFromDb.PaymentStatus = SD.PaymentStatusRejected;
                        orderFromDb.PaymentDate = DateTimeHelper.Now;
                        await db.SaveChangesAsync();
                        
                        _logger.LogInformation("Order #{OrderId} cancelled in database - order not found in payment gateway", order.Id);
                    }
                    else
                    {
                        _logger.LogError("Order #{OrderId} not found in database for cancellation", order.Id);
                    }
                }
                else
                {
                    // Check if order is too old (more than 2 hours)
                    var timeSinceOrder = DateTimeHelper.Now - order.OrderDate;
                    if (timeSinceOrder.TotalMinutes > 20)
                    {
                        _logger.LogWarning("Order #{OrderId} payment NOT verified after {Hours} hours. Cancelling order. Status: {Status}, Message: {Message}", 
                            order.Id, timeSinceOrder.TotalHours, verificationStatus, verificationMessage);

                        // Cancel the order using direct DbContext with tracking enabled
                        var orderFromDb = await db.orderHeaders
                            .AsTracking()
                            .FirstOrDefaultAsync(u => u.Id == order.Id);
                        
                        if (orderFromDb != null)
                        {
                            orderFromDb.OrderStatus = SD.StatusCancelled;
                            orderFromDb.PaymentStatus = SD.PaymentStatusRejected;
                            orderFromDb.PaymentDate = DateTimeHelper.Now;
                            await db.SaveChangesAsync();
                            
                            _logger.LogInformation("Order #{OrderId} cancelled in database due to unverified payment", order.Id);
                        }
                        else
                        {
                            _logger.LogError("Order #{OrderId} not found in database for cancellation", order.Id);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Order #{OrderId} payment not yet verified. Keeping pending. Status: {Status}, Message: {Message}. Age: {Minutes} minutes", 
                            order.Id, verificationStatus, verificationMessage, timeSinceOrder.TotalMinutes);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying/updating Order #{OrderId} via {PaymentMethod}", 
                    order.Id, paymentMethod);
            }
        }

        private async Task<(bool IsPaid, bool NotFound, string Status, string Message)> VerifyGeideaPaymentAsync(OrderHeader order, GeideaSettings settings)
        {
            try
            {
                var geideaHelper = new GeideaHelper(settings);
                
                // Use merchant reference ID (our order ID) to verify payment
                var merchantRefId = order.Id.ToString();
                
                var result = await geideaHelper.VerifyPaymentAsync(merchantRefId);
                
                if (result.Success)
                {
                    // Check if order was found but not paid, or not found at all
                    bool notFound = result.Message?.Contains("No orders found", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true;
                    
                    return (result.IsPaid, notFound, result.Status ?? "Unknown", result.Message ?? "Success");
                }
                else
                {
                    // Check if error indicates order doesn't exist in gateway
                    bool notFound = result.Message?.Contains("No orders found", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("404", StringComparison.OrdinalIgnoreCase) == true;
                    
                    return (false, notFound, "Error", result.Message ?? "Verification failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Geidea payment verification for Order #{OrderId}", order.Id);
                return (false, false, "Exception", ex.Message);
            }
        }

        private async Task<(bool IsPaid, bool NotFound, string Status, string Message)> VerifyTamaraPaymentAsync(OrderHeader order, TamaraSettings settings)
        {
            try
            {
                var tamaraHelper = new TamaraHelper(settings);
                
                // Use the Tamara order ID stored in PaymentIntentId
                var tamaraOrderId = order.PaymentIntentId;
                
                if (string.IsNullOrEmpty(tamaraOrderId))
                {
                    _logger.LogWarning("Order #{OrderId} has no Tamara order ID (PaymentIntentId is null). Marking as not found.", order.Id);
                    // No order ID means payment was never initiated - consider it not found
                    return (false, true, "NoOrderId", "Tamara order ID not found - payment never initiated");
                }
                
                var result = await tamaraHelper.GetOrderDetailsAsync(tamaraOrderId);
                
                if (result.Success)
                {
                    // Check if payment status indicates the order is paid/authorized
                    var isPaid = result.Status?.Equals("approved", StringComparison.OrdinalIgnoreCase) == true ||
                                 result.Status?.Equals("authorized", StringComparison.OrdinalIgnoreCase) == true ||
                                 result.PaymentStatus?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true;
                    
                    return (isPaid, false, result.Status ?? "Unknown", result.Message ?? "Success");
                }
                else
                {
                    // Check if error indicates order doesn't exist in Tamara
                    bool notFound = result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("404", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) == true;
                    
                    return (false, notFound, "Error", result.Message ?? "Verification failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Tamara payment verification for Order #{OrderId}", order.Id);
                return (false, false, "Exception", ex.Message);
            }
        }

        private async Task<(bool IsPaid, bool NotFound, string Status, string Message)> VerifyTappyPaymentAsync(OrderHeader order, TappySettings settings)
        {
            try
            {
                var tappyHelper = new TappyHelper(settings);
                
                // Use the Tabby payment ID stored in PaymentIntentId
                var tabbyPaymentId = order.PaymentIntentId;
                
                if (string.IsNullOrEmpty(tabbyPaymentId))
                {
                    _logger.LogWarning("Order #{OrderId} has no Tabby payment ID (PaymentIntentId is null). Marking as not found.", order.Id);
                    // No payment ID means payment was never initiated - consider it not found
                    return (false, true, "NoPaymentId", "Tabby payment ID not found - payment never initiated");
                }
                
                var result = await tappyHelper.VerifyPaymentAsync(tabbyPaymentId);
                
                if (result.Success)
                {
                    return (result.IsPaid, false, result.Status ?? "Unknown", result.Message ?? "Success");
                }
                else
                {
                    // Check if error indicates payment doesn't exist in Tabby (404)
                    bool notFound = result.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("404", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("does not exist", StringComparison.OrdinalIgnoreCase) == true ||
                                   result.Message?.Contains("Payment not found", StringComparison.OrdinalIgnoreCase) == true;
                    
                    // If not found, cancel immediately. Otherwise keep pending (might be API permission issue)
                    if (notFound)
                    {
                        _logger.LogWarning("Tabby payment not found in gateway for Order #{OrderId}: {Message}", order.Id, result.Message);
                    }
                    else
                    {
                        _logger.LogWarning("Tabby verification failed for Order #{OrderId} (keeping pending): {Message}", order.Id, result.Message);
                    }
                    
                    return (false, notFound, "VerificationFailed", result.Message ?? "Verification failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Tabby payment verification for Order #{OrderId}", order.Id);
                return (false, false, "Exception", ex.Message);
            }
        }
    }
}

