using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Areas.Admin.Controllers
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

        public ServiceSubscriptionController(
            IUnitOfWork unitOfWork, 
            IStringLocalizer<SharedResources> localizer,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
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

        // GET: ServiceSubscription/GetAllPurchases (API for DataTable)
        [HttpGet]
        public IActionResult GetAllPurchases()
        {
            var purchases = _unitOfWork.ServicePurchases.GetAll(
                includeProperties: "ServiceSubscription,ApplicationUser"
            );
            
            var purchaseList = purchases.Select(p => new
            {
                id = p.Id,
                serviceTitle = p.ServiceSubscription?.Title ?? "N/A",
                customerName = p.ApplicationUser != null ? p.ApplicationUser.Name : p.GuestName,
                email = p.ApplicationUser != null ? p.ApplicationUser.Email : p.GuestEmail,
                phone = p.ApplicationUser != null ? p.ApplicationUser.PhoneNumber : p.GuestPhone,
                totalAmount = p.TotalAmount,
                amountPaid = p.AmountPaid,
                paymentStatus = p.PaymentStatus,
                purchaseDate = p.PurchaseDate
            }).OrderByDescending(p => p.purchaseDate).ToList();

            return Json(new { data = purchaseList });
        }

        // GET: ServiceSubscription/Create
        public IActionResult Create()
        {
            var service = new ServiceSubscription
            {
                IsActive = true,
                ServiceType = ServiceType.Online,
                CreatedDate = BulkyBook.Utility.DateTimeHelper.Now,
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
                service.CreatedDate = BulkyBook.Utility.DateTimeHelper.Now;

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

                existingService.UpdatedDate = BulkyBook.Utility.DateTimeHelper.Now;

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
            service.UpdatedDate = BulkyBook.Utility.DateTimeHelper.Now;
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
    }
}
