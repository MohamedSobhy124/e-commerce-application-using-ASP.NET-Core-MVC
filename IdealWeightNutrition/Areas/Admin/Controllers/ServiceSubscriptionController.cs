using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ServiceSubscriptionController : Controller
    {
        public class DeleteImageRequest
        {
            public int imageId { get; set; }
        }
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDBContext _dbContext;
        private readonly IEmailSender _emailSender;
        private readonly InvoiceService _invoiceService;

        public ServiceSubscriptionController(
            IUnitOfWork unitOfWork, 
            IStringLocalizer<SharedResources> localizer,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDBContext dbContext,
            IEmailSender emailSender,
            InvoiceService invoiceService)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
            _emailSender = emailSender;
            _invoiceService = invoiceService;
        }

        // GET: ServiceSubscription/Index
        public IActionResult Index()
        {
            var services = _unitOfWork.ServiceSubscriptions.GetAll().OrderBy(s => s.DisplayOrder).ThenByDescending(s => s.CreatedDate);
            return View(services);
        }

        // GET: ServiceSubscription/GetAll (API for DataTable)
        [HttpGet]
        public IActionResult GetAll()
        {
            var services = _unitOfWork.ServiceSubscriptions.GetAll(includeProperties: "ServicePurchases");
            var serviceList = services.Select(s => new
            {
                id = s.Id,
                title = s.Title,
                price = s.Price,
                serviceType = s.ServiceType,
                offlinePaymentPercent = s.OfflinePaymentPercent,
                imageUrl = s.ImageUrl,
                isActive = s.IsActive,
                purchaseCount = s.ServicePurchases?.Count() ?? 0
            }).ToList();

            return Json(new { data = serviceList });
        }

        // GET: ServiceSubscription/GetAllPurchases (API for Tabulator with filtering)
        [HttpGet]
        public IActionResult GetAllPurchases(
            string paymentStatus = "",
            string serviceStatus = "",
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
                IQueryable<ServicePurchase> query = _unitOfWork.ServicePurchases.GetAll(
                    includeProperties: "ServiceSubscription,ApplicationUser"
                ).AsQueryable();

                // Apply payment status filter
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    query = query.Where(p => p.PaymentStatus == paymentStatus);
                }

                // Apply service status filter
                if (!string.IsNullOrEmpty(serviceStatus))
                {
                    query = query.Where(p => p.Status == serviceStatus);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateFrom))
                {
                    if (DateTime.TryParse(dateFrom, out var fromDate))
                    {
                        query = query.Where(p => p.PurchaseDate >= fromDate.Date);
                    }
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    if (DateTime.TryParse(dateTo, out var toDate))
                    {
                        query = query.Where(p => p.PurchaseDate <= toDate.Date.AddDays(1).AddTicks(-1));
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(p =>
                        p.Id.ToString().Contains(searchValue) ||
                        (p.ServiceSubscription != null && p.ServiceSubscription.Title != null && p.ServiceSubscription.Title.ToLower().Contains(searchValue)) ||
                        (p.ApplicationUser != null && p.ApplicationUser.Name != null && p.ApplicationUser.Name.ToLower().Contains(searchValue)) ||
                        (p.GuestName != null && p.GuestName.ToLower().Contains(searchValue)) ||
                        (p.ApplicationUser != null && p.ApplicationUser.Email != null && p.ApplicationUser.Email.ToLower().Contains(searchValue)) ||
                        (p.GuestEmail != null && p.GuestEmail.ToLower().Contains(searchValue)) ||
                        (p.ApplicationUser != null && p.ApplicationUser.PhoneNumber != null && p.ApplicationUser.PhoneNumber.Contains(searchValue)) ||
                        (p.GuestPhone != null && p.GuestPhone.Contains(searchValue)) ||
                        (p.PaymentStatus != null && p.PaymentStatus.ToLower().Contains(searchValue)) ||
                        (p.Status != null && p.Status.ToLower().Contains(searchValue)) ||
                        p.TotalAmount.ToString().Contains(searchValue) ||
                        p.AmountPaid.ToString().Contains(searchValue)
                    );
                }

                // Get total count before pagination
                var totalRecords = query.Count();

                // Apply sorting
                if (sortColumn.ToLower() == "id")
                {
                    query = sortDirection.ToLower() == "asc" ? query.OrderBy(p => p.Id) : query.OrderByDescending(p => p.Id);
                }
                else if (sortColumn.ToLower() == "purchasedate")
                {
                    query = sortDirection.ToLower() == "asc" ? query.OrderBy(p => p.PurchaseDate) : query.OrderByDescending(p => p.PurchaseDate);
                }
                else if (sortColumn.ToLower() == "totalamount")
                {
                    query = sortDirection.ToLower() == "asc" ? query.OrderBy(p => p.TotalAmount) : query.OrderByDescending(p => p.TotalAmount);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }

                // Apply pagination
                var purchases = query.Skip(start).Take(length).ToList();

                // Map to lowercase properties for Tabulator
                var purchaseData = purchases.Select(p => new
                {
                    id = p.Id,
                    serviceTitle = p.ServiceSubscription?.Title ?? "N/A",
                    customerName = p.ApplicationUser != null ? p.ApplicationUser.Name : p.GuestName,
                    email = p.ApplicationUser != null ? p.ApplicationUser.Email : p.GuestEmail,
                    phone = p.ApplicationUser != null ? p.ApplicationUser.PhoneNumber : p.GuestPhone,
                    totalAmount = p.TotalAmount,
                    amountPaid = p.AmountPaid,
                    discountAmount = p.DiscountAmount,
                    vatAmount = CalculateServiceVAT(p.TotalAmount, p.DiscountAmount),
                    paymentStatus = p.PaymentStatus,
                    serviceStatus = p.Status,
                    purchaseDate = p.PurchaseDate
                }).ToList();

                // Return data in Tabulator format
                return Json(new
                {
                    last_page = (int)Math.Ceiling((double)totalRecords / length),
                    data = purchaseData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error loading service purchases" });
            }
        }

        /// <summary>
        /// Calculate VAT amount (5% of total amount after discount)
        /// </summary>
        private decimal CalculateServiceVAT(decimal totalAmount, decimal discountAmount)
        {
            const decimal vatRate = 0.05m; // 5% VAT rate
            var taxableAmount = totalAmount - discountAmount;
            return taxableAmount * (vatRate / (1 + vatRate));
        }

        // GET: ServiceSubscription/GetServicePurchaseStatistics
        [HttpGet]
        public IActionResult GetServicePurchaseStatistics()
        {
            try
            {
                var allPurchases = _unitOfWork.ServicePurchases.GetAll().ToList();

                var stats = new
                {
                    all = allPurchases.Count,
                    pending = allPurchases.Count(p => p.PaymentStatus == "Pending"),
                    approved = allPurchases.Count(p => p.PaymentStatus == "Approved"),
                    rejected = allPurchases.Count(p => p.PaymentStatus == "Rejected")
                };

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        // GET: ServiceSubscription/ExportServicePurchases
        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ExportServicePurchases(
            string paymentStatus = "",
            string serviceStatus = "",
            string dateFrom = "",
            string dateTo = "",
            string searchValue = "")
        {
            try
            {
                // Start with base query (same as GetAllPurchases)
                IQueryable<ServicePurchase> query = _unitOfWork.ServicePurchases.GetAll(
                    includeProperties: "ServiceSubscription,ApplicationUser"
                ).AsQueryable();

                // Apply payment status filter
                if (!string.IsNullOrEmpty(paymentStatus))
                {
                    query = query.Where(p => p.PaymentStatus == paymentStatus);
                }

                // Apply service status filter
                if (!string.IsNullOrEmpty(serviceStatus))
                {
                    query = query.Where(p => p.Status == serviceStatus);
                }

                // Apply date range filter
                if (!string.IsNullOrEmpty(dateFrom))
                {
                    if (DateTime.TryParse(dateFrom, out var fromDate))
                    {
                        query = query.Where(p => p.PurchaseDate >= fromDate.Date);
                    }
                }

                if (!string.IsNullOrEmpty(dateTo))
                {
                    if (DateTime.TryParse(dateTo, out var toDate))
                    {
                        query = query.Where(p => p.PurchaseDate <= toDate.Date.AddDays(1).AddTicks(-1));
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(p =>
                        p.Id.ToString().Contains(searchValue) ||
                        (p.ServiceSubscription != null && p.ServiceSubscription.Title != null && p.ServiceSubscription.Title.ToLower().Contains(searchValue)) ||
                        (p.ApplicationUser != null && p.ApplicationUser.Name != null && p.ApplicationUser.Name.ToLower().Contains(searchValue)) ||
                        (p.GuestName != null && p.GuestName.ToLower().Contains(searchValue)) ||
                        (p.ApplicationUser != null && p.ApplicationUser.Email != null && p.ApplicationUser.Email.ToLower().Contains(searchValue)) ||
                        (p.GuestEmail != null && p.GuestEmail.ToLower().Contains(searchValue)) ||
                        (p.PaymentStatus != null && p.PaymentStatus.ToLower().Contains(searchValue)) ||
                        (p.Status != null && p.Status.ToLower().Contains(searchValue)) ||
                        p.TotalAmount.ToString().Contains(searchValue) ||
                        p.AmountPaid.ToString().Contains(searchValue)
                    );
                }

                // Get all purchases (no pagination for export)
                var purchases = query.OrderByDescending(p => p.Id).ToList();

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Purchase ID,Service Title,Customer Name,Email,Phone,Purchase Date,Total Without VAT,VAT Amount,Total Inc VAT,Discount Amount,Amount Paid,Remaining Amount,Payment Status,Service Status");

                foreach (var purchase in purchases)
                {
                    var vatAmount = CalculateServiceVAT(purchase.TotalAmount, purchase.DiscountAmount);
                    var totalWithoutVat = purchase.TotalAmount - purchase.DiscountAmount - vatAmount;
                    var remainingAmount = purchase.TotalAmount - purchase.AmountPaid;
                    
                    csv.AppendLine($"{purchase.Id}," +
                        $"\"{(purchase.ServiceSubscription?.Title ?? "N/A").Replace("\"", "\"\"")}\"," +
                        $"\"{(purchase.ApplicationUser?.Name ?? purchase.GuestName ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(purchase.ApplicationUser?.Email ?? purchase.GuestEmail ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(purchase.ApplicationUser?.PhoneNumber ?? purchase.GuestPhone ?? "").Replace("\"", "\"\"")}\"," +
                        $"{purchase.PurchaseDate:yyyy-MM-dd HH:mm:ss}," +
                        $"{totalWithoutVat:F2}," +
                        $"{vatAmount:F2}," +
                        $"{(purchase.TotalAmount - purchase.DiscountAmount):F2}," +
                        $"{purchase.DiscountAmount:F2}," +
                        $"{purchase.AmountPaid:F2}," +
                        $"{remainingAmount:F2}," +
                        $"\"{(purchase.PaymentStatus ?? "").Replace("\"", "\"\"")}\"," +
                        $"\"{(purchase.Status ?? "").Replace("\"", "\"\"")}\"");
                }

                var fileName = $"ServicePurchases_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error exporting service purchases";
                return RedirectToAction(nameof(Purchases));
            }
        }

        // GET: ServiceSubscription/Create
        public IActionResult Create()
        {
            var service = new ServiceSubscription
            {
                IsActive = true,
                ServiceType = ServiceType.Online,
                CreatedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                ServiceImages = new List<ServiceImage>()
            };
            return View(service);
        }

        // POST: ServiceSubscription/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ServiceSubscription service, List<IFormFile>? files)
        {
            // Validate at least one image is required
            if (files == null || files.Count == 0 || !files.Any(f => f != null && f.Length > 0))
            {
                ModelState.AddModelError("", "At least one image is required.");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string servicePath = Path.Combine(wwwRootPath, @"images\services");

                if (!Directory.Exists(servicePath))
                {
                    Directory.CreateDirectory(servicePath);
                }

                // Handle multiple image uploads
                if (files != null && files.Count > 0)
                {
                    int displayOrder = 0;
                    service.ServiceImages = new List<ServiceImage>();

                    foreach (var file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                            using (var fileStream = new FileStream(Path.Combine(servicePath, fileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            string imageUrl = @"\images\services\" + fileName;

                            // Set first image as main ImageUrl for backward compatibility
                            if (string.IsNullOrEmpty(service.ImageUrl))
                            {
                                service.ImageUrl = imageUrl;
                            }

                            // Add ServiceImage
                            service.ServiceImages.Add(new ServiceImage
                            {
                                ImageUrl = imageUrl,
                                DisplayOrder = displayOrder++
                            });
                        }
                    }
                }

                // Validate offline payment percent for offline services
                if (service.ServiceType == ServiceType.Offline)
                {
                    if (!service.OfflinePaymentPercent.HasValue || service.OfflinePaymentPercent.Value <= 0 || service.OfflinePaymentPercent.Value > 100)
                    {
                        ModelState.AddModelError("OfflinePaymentPercent", "Offline payment percentage must be between 1 and 100");
                        return View(service);
                    }
                }
                else
                {
                    service.OfflinePaymentPercent = null;
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                service.CreatedBy = userId;
                service.CreatedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;

                _unitOfWork.ServiceSubscriptions.Add(service);
                _unitOfWork.save();

                TempData["success"] = "Service subscription created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(service);
        }

        // GET: ServiceSubscription/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == id, includeProperties: "ServiceImages");
            
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: ServiceSubscription/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ServiceSubscription service, List<IFormFile>? files)
        {
            // Get existing service with images
            var existingService = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == service.Id, includeProperties: "ServiceImages");
            
            if (existingService == null)
            {
                return NotFound();
            }

            // Validate at least one image exists (either existing or new)
            bool hasExistingImages = existingService.ServiceImages != null && existingService.ServiceImages.Any();
            bool hasNewImages = files != null && files.Count > 0 && files.Any(f => f != null && f.Length > 0);
            
            if (!hasExistingImages && !hasNewImages)
            {
                ModelState.AddModelError("", "At least one image is required.");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string servicePath = Path.Combine(wwwRootPath, @"images\services");

                if (!Directory.Exists(servicePath))
                {
                    Directory.CreateDirectory(servicePath);
                }

                // Handle multiple image uploads
                if (files != null && files.Count > 0)
                {
                    int displayOrder = existingService.ServiceImages != null && existingService.ServiceImages.Any()
                        ? existingService.ServiceImages.Max(si => si.DisplayOrder) + 1
                        : 0;

                    foreach (var file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                            using (var fileStream = new FileStream(Path.Combine(servicePath, fileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            string imageUrl = @"\images\services\" + fileName;

                            // Set first image as main ImageUrl if not set
                            if (string.IsNullOrEmpty(service.ImageUrl) && string.IsNullOrEmpty(existingService.ImageUrl))
                            {
                                service.ImageUrl = imageUrl;
                            }
                            else if (string.IsNullOrEmpty(service.ImageUrl))
                            {
                                service.ImageUrl = existingService.ImageUrl;
                            }

                            // Add ServiceImage
                            var serviceImage = new ServiceImage
                            {
                                ServiceSubscriptionId = service.Id,
                                ImageUrl = imageUrl,
                                DisplayOrder = displayOrder++
                            };
                            _dbContext.ServiceImages.Add(serviceImage);
                        }
                    }
                }
                else
                {
                    // Keep existing ImageUrl if no new images uploaded
                    service.ImageUrl = existingService.ImageUrl;
                }

                // Update existing service properties instead of updating a new instance
                existingService.Title = service.Title;
                existingService.TitleAr = service.TitleAr;
                existingService.Description = service.Description;
                existingService.DescriptionAr = service.DescriptionAr;
                existingService.ServiceType = service.ServiceType;
                existingService.Price = service.Price;
                existingService.IsActive = service.IsActive;
                existingService.DisplayOrder = service.DisplayOrder;
                existingService.ImageUrl = service.ImageUrl;

                // Validate offline payment percent for offline services
                if (service.ServiceType == ServiceType.Offline)
                {
                    if (!service.OfflinePaymentPercent.HasValue || service.OfflinePaymentPercent.Value <= 0 || service.OfflinePaymentPercent.Value > 100)
                    {
                        ModelState.AddModelError("OfflinePaymentPercent", "Offline payment percentage must be between 1 and 100");
                        service.ServiceImages = existingService.ServiceImages;
                        return View(service);
                    }
                    existingService.OfflinePaymentPercent = service.OfflinePaymentPercent;
                }
                else
                {
                    existingService.OfflinePaymentPercent = null;
                }

                existingService.UpdatedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;

                _unitOfWork.ServiceSubscriptions.Update(existingService);
                _unitOfWork.save();

                TempData["success"] = "Service subscription updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            service.ServiceImages = existingService.ServiceImages;
            return View(service);
        }

        // POST: ServiceSubscription/DeleteImage
        [HttpPost]
        public IActionResult DeleteImage([FromBody] DeleteImageRequest request)
        {
            if (request == null || request.imageId <= 0)
            {
                return Json(new { success = false, message = "Invalid image ID" });
            }

            var image = _dbContext.ServiceImages.Find(request.imageId);
            if (image == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }

            // Delete physical file
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(wwwRootPath, image.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            // Get service to check if this is the main image
            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == image.ServiceSubscriptionId);
            if (service != null && service.ImageUrl == image.ImageUrl)
            {
                // Set another image as main if available
                var otherImages = _dbContext.ServiceImages
                    .Where(si => si.ServiceSubscriptionId == image.ServiceSubscriptionId && si.Id != request.imageId)
                    .OrderBy(si => si.DisplayOrder)
                    .FirstOrDefault();
                
                if (otherImages != null)
                {
                    service.ImageUrl = otherImages.ImageUrl;
                }
                else
                {
                    service.ImageUrl = null;
                }
                
                // Update the tracked entity
                _unitOfWork.ServiceSubscriptions.Update(service);
                _unitOfWork.save();
            }

            // Delete from database
            _dbContext.ServiceImages.Remove(image);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Image deleted successfully" });
        }

        // GET: ServiceSubscription/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == id, includeProperties: "ServiceOffers,ServicePurchases");
            
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // GET: ServiceSubscription/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == id, includeProperties: "ServicePurchases");
            
            if (service == null)
            {
                return NotFound();
            }

            return View(service);
        }

        // POST: ServiceSubscription/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == id);
            
            if (service == null)
            {
                return NotFound();
            }

            // Delete image if exists
            if (!string.IsNullOrEmpty(service.ImageUrl))
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(wwwRootPath, service.ImageUrl.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _unitOfWork.ServiceSubscriptions.Remove(service);
            _unitOfWork.save();

            TempData["success"] = "Service subscription deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: ServiceSubscription/ToggleActive
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == id);
            
            if (service == null)
            {
                return Json(new { success = false, message = "Service not found" });
            }

            service.IsActive = !service.IsActive;
            service.UpdatedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            _unitOfWork.ServiceSubscriptions.Update(service);
            _unitOfWork.save();

            var message = service.IsActive ? "Service activated" : "Service deactivated";
            
            return Json(new 
            { 
                success = true, 
                message = message,
                isActive = service.IsActive
            });
        }

        // GET: ServiceSubscription/Purchases - View all service purchases
        public IActionResult Purchases()
        {
            var purchases = _unitOfWork.ServicePurchases.GetAll(
                includeProperties: "ServiceSubscription,ApplicationUser,ServiceOffer"
            ).OrderByDescending(p => p.PurchaseDate).ToList();
            
            return View(purchases);
        }

        // GET: ServiceSubscription/PurchaseDetails/5 - View details of a specific purchase
        public IActionResult PurchaseDetails(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var purchase = _unitOfWork.ServicePurchases.Get(
                p => p.Id == id,
                includeProperties: "ServiceSubscription,ApplicationUser,ServiceOffer"
            );

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // POST: ServiceSubscription/UpdatePaymentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdatePaymentStatus(int id, string paymentStatus)
        {
            var purchase = _unitOfWork.ServicePurchases.Get(p => p.Id == id);
            
            if (purchase == null)
            {
                return Json(new { success = false, message = "Purchase not found" });
            }

            var oldStatus = purchase.PaymentStatus;
            purchase.PaymentStatus = paymentStatus;
            _unitOfWork.ServicePurchases.Update(purchase);
            _unitOfWork.save();

            return Json(new { 
                success = true, 
                message = $"Payment status updated from {oldStatus} to {paymentStatus}",
                oldStatus = oldStatus,
                newStatus = paymentStatus
            });
        }

        // POST: ServiceSubscription/UpdateServiceStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateServiceStatus(int id, string status)
        {
            var purchase = _unitOfWork.ServicePurchases.Get(p => p.Id == id);
            
            if (purchase == null)
            {
                return Json(new { success = false, message = "Purchase not found" });
            }

            var oldStatus = purchase.Status;
            purchase.Status = status;
            _unitOfWork.ServicePurchases.Update(purchase);
            _unitOfWork.save();

            return Json(new { 
                success = true, 
                message = $"Service status updated from {oldStatus} to {status}",
                oldStatus = oldStatus,
                newStatus = status
            });
        }

        // POST: ServiceSubscription/UpdateAmountPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAmountPaid(int id, decimal amountPaid)
        {
            var purchase = _unitOfWork.ServicePurchases.Get(
                p => p.Id == id,
                includeProperties: "ServiceSubscription,ApplicationUser"
            );
            
            if (purchase == null)
            {
                return Json(new { success = false, message = "Purchase not found" });
            }

            if (amountPaid < 0)
            {
                return Json(new { success = false, message = "Amount paid cannot be negative" });
            }

            var newTotalAmount = amountPaid + purchase.AmountPaid;
            if (newTotalAmount > purchase.TotalAmount)
            {
                return Json(new { 
                    success = false, 
                    message = $"Total amount paid ({newTotalAmount:C}) cannot exceed total amount ({purchase.TotalAmount:C})" 
                });
            }

            var oldAmount = purchase.AmountPaid;
            purchase.AmountPaid = newTotalAmount;
            
            // Auto-update payment status if fully paid
            if (purchase.AmountPaid >= purchase.TotalAmount && (purchase.PaymentStatus == "Pending" || purchase.PaymentStatus == "Rejected"))
            {
                purchase.PaymentStatus = "Approved";
            }
            
            _unitOfWork.ServicePurchases.Update(purchase);
            _unitOfWork.save();

            // Send email with invoice PDF to customer
            try
            {
                var customerEmail = purchase.ApplicationUser?.Email ?? purchase.GuestEmail;
                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    // Generate PDF invoice
                    byte[] invoicePdf = _invoiceService.GenerateServicePurchaseInvoicePdf(purchase, purchase.ApplicationUser);
                    
                    // Generate email body
                    var customerName = purchase.ApplicationUser?.Name ?? purchase.GuestName ?? "Customer";
                    var serviceName = purchase.ServiceSubscription?.Title ?? "Service Subscription";
                    var emailSubject = $"Payment Update - Service Purchase #{purchase.Id} - Ideal Weight Nutrition";
                    
                    var emailBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9fafb;'>
                            <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                <h2 style='color: #059669; margin-top: 0;'>Payment Update Notification</h2>
                                <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                    Dear {customerName},
                                </p>
                                <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                    This email is to confirm that your payment for <strong>{serviceName}</strong> has been updated.
                                </p>
                                <div style='background-color: #f3f4f6; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                    <h3 style='color: #1f2937; margin-top: 0;'>Payment Details</h3>
                                    <table style='width: 100%; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 8px 0; color: #6b7280;'>Previous Amount Paid:</td>
                                            <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #1f2937;'>{oldAmount:C}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #6b7280;'>Amount Added:</td>
                                            <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #059669;'>+{amountPaid:C}</td>
                                        </tr>
                                        <tr style='border-top: 2px solid #e5e7eb;'>
                                            <td style='padding: 8px 0; color: #1f2937; font-weight: 600;'>New Total Amount Paid:</td>
                                            <td style='padding: 8px 0; text-align: right; font-weight: 700; color: #059669; font-size: 18px;'>{purchase.AmountPaid:C}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #6b7280;'>Total Amount:</td>
                                            <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #1f2937;'>{purchase.TotalAmount:C}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #6b7280;'>Remaining Amount:</td>
                                            <td style='padding: 8px 0; text-align: right; font-weight: 600; color: #f59e0b;'>{(purchase.TotalAmount - purchase.AmountPaid):C}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #6b7280;'>Payment Status:</td>
                                            <td style='padding: 8px 0; text-align: right;'>
                                                <span style='background-color: {(purchase.PaymentStatus == "Approved" ? "#10b981" : "#f59e0b")}; color: white; padding: 4px 12px; border-radius: 4px; font-size: 12px; font-weight: 600;'>{purchase.PaymentStatus}</span>
                                            </td>
                                        </tr>
                                    </table>
                                </div>
                                <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                    Please find attached the updated invoice for your records.
                                </p>
                                <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                    If you have any questions or concerns, please don't hesitate to contact us.
                                </p>
                                <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;' />
                                <p style='color: #9ca3af; font-size: 12px; text-align: center; margin: 0;'>
                                    © {DateTime.Now.Year} Ideal Weight Nutrition. All rights reserved.
                                </p>
                            </div>
                        </div>";

                    // Send email with PDF attachment
                    if (_emailSender is EmailSender customEmailSender)
                    {
                        await customEmailSender.SendEmailWithAttachmentAsync(
                            customerEmail,
                            emailSubject,
                            emailBody,
                            invoicePdf,
                            $"Invoice-SVC-{purchase.Id}.pdf"
                        );
                    }
                    else
                    {
                        // Fallback: send email without attachment
                        await _emailSender.SendEmailAsync(customerEmail, emailSubject, emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the request
                // The amount has already been updated successfully
                // In production, you might want to log this to a logging service
                System.Diagnostics.Debug.WriteLine($"Error sending invoice email: {ex.Message}");
            }

            return Json(new { 
                success = true, 
                message = $"Amount paid updated from {oldAmount:C} to {purchase.AmountPaid:C}. Invoice email sent to customer.",
                oldAmount = oldAmount,
                newAmount = purchase.AmountPaid,
                paymentStatus = purchase.PaymentStatus
            });
        }
    }
}
