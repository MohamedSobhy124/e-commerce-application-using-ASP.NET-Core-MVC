using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using System.IO;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class FlashSaleController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<FlashSaleController> _logger;
        private readonly ApplicationDBContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FlashSaleController(
            IUnitOfWork unitOfWork, 
            IEmailSender emailSender,
            IStringLocalizer<SharedResources> localizer,
            ILogger<FlashSaleController> logger,
            ApplicationDBContext dbContext,
            IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _localizer = localizer;
            _logger = logger;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Flash Sales List
        public IActionResult Index()
        {
            var flashSales = _unitOfWork.FlashSale.GetAll(includeProperties: "FlashSaleItems");
            return View(flashSales);
        }

        // GET: Create Flash Sale
        public IActionResult Create()
        {
            var flashSale = new FlashSale
            {
                StartDate = BulkyBook.Utility.DateTimeHelper.Now,
                EndDate = BulkyBook.Utility.DateTimeHelper.Now.AddDays(1)
            };
            return View(flashSale);
        }

        // POST: Create Flash Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlashSale flashSale, bool notifySubscribers = false)
        {
            // Initialize ImageUrl to empty string to avoid null validation errors
            if (string.IsNullOrEmpty(flashSale.ImageUrl))
            {
                flashSale.ImageUrl = string.Empty;
            }

            // Validate image is required
            if (flashSale.ImageFile == null || flashSale.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Image is required.");
            }

            // Remove ImageUrl from ModelState validation since we handle it manually
            ModelState.Remove("ImageUrl");

            // Validate dates
            if (flashSale.EndDate <= flashSale.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string flashSalePath = Path.Combine(wwwRootPath, @"images\flashsales");

                    if (!Directory.Exists(flashSalePath))
                    {
                        Directory.CreateDirectory(flashSalePath);
                    }

                    if (flashSale.ImageFile != null && flashSale.ImageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(flashSale.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(flashSalePath, fileName), FileMode.Create))
                        {
                            flashSale.ImageFile.CopyTo(fileStream);
                        }
                        flashSale.ImageUrl = @"\images\flashsales\" + fileName;
                    }
                    else
                    {
                        // This shouldn't happen if validation passed, but just in case
                        ModelState.AddModelError("ImageFile", "Image is required.");
                        return View(flashSale);
                    }

                // Set audit fields
                AuditHelper.SetCreatedAudit(flashSale, User);
                _unitOfWork.FlashSale.Add(flashSale);
                _unitOfWork.save();
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    _logger.LogError(ex, "Error creating flash sale");
                    ModelState.AddModelError("", $"An error occurred while creating the flash sale: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                    return View(flashSale);
                }
                
                // Send notification to subscribers if requested
                if (notifySubscribers)
                {
                    try
                    {
                        var activeSubscribers = _unitOfWork.NewsletterSubscription.GetAll(s => s.IsActive).ToList();
                        int sentCount = 0;
                        int failedCount = 0;

                        foreach (var subscriber in activeSubscribers)
                        {
                            try
                            {
                                var flashSaleUrl = Url.Action("Index", "FlashSale", new { area = "Customer" }, Request.Scheme);
                                var subject = _localizer["NewFlashSaleNotification"].ToString();
                                var message = GenerateFlashSaleEmailHtml(flashSale, flashSaleUrl);

                                await _emailSender.SendEmailAsync(subscriber.Email, subject, message);
                                sentCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending flash sale email to {Email}", subscriber.Email);
                                failedCount++;
                            }
                        }

                        if (sentCount > 0)
                        {
                            TempData["success"] = $"Flash sale created successfully! Notification sent to {sentCount} subscriber(s).";
                            if (failedCount > 0)
                            {
                                TempData["warning"] = $"Failed to send notification to {failedCount} subscriber(s).";
                            }
                        }
                        else
                        {
                            TempData["success"] = "Flash sale created successfully!";
                            TempData["warning"] = "Failed to send notifications to subscribers.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending flash sale notifications to subscribers");
                        TempData["success"] = "Flash sale created successfully!";
                        TempData["warning"] = "Failed to send notifications to subscribers.";
                    }
                }
                else
                {
                    TempData["success"] = "Flash sale created successfully! Now add products to it.";
                }
                
                return RedirectToAction(nameof(AddProducts), new { id = flashSale.Id });
            }

            return View(flashSale);
        }

        private string GenerateFlashSaleEmailHtml(FlashSale flashSale, string flashSaleUrl)
        {
            var startDate = flashSale.StartDate.ToString("MMMM dd, yyyy HH:mm");
            var endDate = flashSale.EndDate.ToString("MMMM dd, yyyy HH:mm");
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .flash-sale-name {{ font-size: 28px; font-weight: bold; margin-bottom: 10px; }}
        .flash-sale-desc {{ font-size: 16px; margin-bottom: 20px; }}
        .details {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ margin: 10px 0; }}
        .detail-label {{ font-weight: bold; color: #667eea; }}
        .cta-button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; margin-top: 20px; font-weight: bold; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='flash-sale-name'>⚡ {flashSale.Name}</div>
            <div class='flash-sale-desc'>{_localizer["FlashSale"]}</div>
        </div>
        <div class='content'>
            {(string.IsNullOrEmpty(flashSale.Description) ? "" : $"<p>{flashSale.Description}</p>")}
            <div class='details'>
                <div class='detail-row'>
                    <span class='detail-label'>{_localizer["StartDate"]}:</span> {startDate}
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>{_localizer["EndDate"]}:</span> {endDate}
                </div>
            </div>
            <div style='text-align: center;'>
                <a href='{flashSaleUrl}' class='cta-button'>{_localizer["ViewFlashSale"]}</a>
            </div>
            <div class='footer'>
                <p>{_localizer["FlashSaleEmailFooter"]}</p>
            </div>
        </div>
    </div>
</body>
</html>";
        }

        // GET: Add Products to Flash Sale
        public IActionResult AddProducts(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                TempData["error"] = "Flash sale not found";
                return RedirectToAction(nameof(Index));
            }

            // Get all products with stock info
            var allProducts = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages").ToList();
            
            // Get products that are in conflicting flash sales
            var conflictingProductIds = _unitOfWork.FlashSale.GetAll(includeProperties: "FlashSaleItems")
                .Where(fs => fs.Id != id && // Not the current flash sale
                             fs.IsActive && // Must be active
                             // Check for time overlap
                             ((fs.StartDate <= flashSale.EndDate && fs.EndDate >= flashSale.StartDate)))
                .SelectMany(fs => fs.FlashSaleItems.Select(item => item.ProductId))
                .Distinct()
                .ToList();

            // Get current culture for localization
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            var currencySymbol = CurrencyHelper.GetCurrencySymbol(currentCulture);
            
            // Get localized "Stock" text
            var stockText = _localizer["Stock"]?.Value ?? "Stock";
            
            var products = allProducts
                .Where(p => !p.IsDeleted) // Filter deleted products
                .Select(p => {
                    // Get localized product title
                    var productTitle = (currentCulture == "ar" && !string.IsNullOrEmpty(p.TitleAr)) 
                        ? p.TitleAr 
                        : p.Title;
                    
                    var isConflicting = conflictingProductIds.Contains(p.Id);
                    var conflictText = currentCulture == "ar" ? "⚠️ في عرض آخر" : "⚠️ IN ANOTHER FLASH SALE";
                    
                    return new SelectListItem
                    {
                        Text = isConflicting 
                            ? $"{productTitle} ({stockText}: {p.StockQuantity}) {conflictText}"
                            : $"{productTitle} ({stockText}: {p.StockQuantity})",
                        Value = p.Id.ToString(),
                        Disabled = isConflicting // Disable conflicting products
                    };
                }).ToList();

            ViewBag.Products = products;
            ViewBag.FlashSale = flashSale;

            return View();
        }

        // POST: Add Product to Flash Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProductToSale(int flashSaleId, int productId, int quantity, decimal price, int? productVariantId = null)
        {
            var flashSale = _unitOfWork.FlashSale.Get(f => f.Id == flashSaleId);
            if (flashSale == null)
            {
                return Json(new { success = false, message = "Flash sale not found" });
            }

            var product = _unitOfWork.product.Get(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Get variant if provided
            ProductVariant? variant = null;
            if (productVariantId.HasValue && productVariantId.Value > 0)
            {
                variant = _unitOfWork.ProductVariant.Get(v => v.Id == productVariantId.Value && v.ProductId == productId);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Variant not found" });
                }
            }

            // Validation - use variant stock/price if variant exists
            int availableStock = variant?.StockQuantity ?? product.StockQuantity;
            decimal originalPrice = variant?.Price ?? (decimal)product.Price;

            if (quantity > availableStock)
            {
                return Json(new { success = false, message = $"Quantity cannot exceed stock quantity ({availableStock})" });
            }
             
            if (price >= originalPrice)
            {
                return Json(new { success = false, message = $"Price cannot exceed or equal original ({originalPrice})" });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Quantity must be greater than 0" });
            }

            if (price <= 0)
            {
                return Json(new { success = false, message = "Price must be greater than 0" });
            }

            // Check if product/variant already in this flash sale
            var existingItem = _unitOfWork.FlashSaleItem.Get(
                i => i.FlashSaleId == flashSaleId && 
                     i.ProductId == productId && 
                     i.ProductVariantId == productVariantId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "This product/variant is already in this flash sale" });
            }

            // 🔥 NEW: Check if product is in another active flash sale during overlapping time
            var conflictingFlashSales = _unitOfWork.FlashSale.GetAll(includeProperties: "FlashSaleItems")
                .Where(fs => fs.Id != flashSaleId && // Not the current flash sale
                             fs.IsActive && // Must be active
                             fs.FlashSaleItems.Any(item => item.ProductId == productId) && // Has this product
                             // Check for time overlap
                             ((fs.StartDate <= flashSale.EndDate && fs.EndDate >= flashSale.StartDate)))
                .ToList();

            if (conflictingFlashSales.Any())
            {
                var conflictingSale = conflictingFlashSales.First();
                return Json(new { 
                    success = false, 
                    message = $"⚠️ This product is already in another active flash sale '{conflictingSale.Name}' " +
                              $"from {conflictingSale.StartDate:MMM dd, yyyy HH:mm} to {conflictingSale.EndDate:MMM dd, yyyy HH:mm}. " +
                              $"Please deactivate or remove it from the other flash sale first, or change the dates to avoid overlap." 
                });
            }

            // Add product to flash sale
            var flashSaleItem = new FlashSaleItem
            {
                FlashSaleId = flashSaleId,
                ProductId = productId,
                ProductVariantId = productVariantId,
                FlashSaleQuantity = quantity,
                FlashSaleQuantityCreated = quantity,
                FlashSalePrice = price,
                AddedDate = BulkyBook.Utility.DateTimeHelper.Now
            };

            // Set audit fields
            AuditHelper.SetCreatedAudit(flashSaleItem, User);
            _unitOfWork.FlashSaleItem.Add(flashSaleItem);
            _unitOfWork.save();

            return Json(new { success = true, message = "Product added successfully" });
        }

        // POST: Remove Product from Flash Sale
        [HttpPost]
        public IActionResult RemoveProduct(int itemId)
        {
            var item = _unitOfWork.FlashSaleItem.Get(i => i.Id == itemId);
            if (item == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            // Soft delete - handled by repository, but set audit fields
            AuditHelper.SetDeletedAudit(item, User);
            _unitOfWork.FlashSaleItem.Remove(item);
            _unitOfWork.save();

            return Json(new { success = true, message = "Product removed from flash sale" });
        }

        // GET: Edit Flash Sale
        public IActionResult Edit(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                TempData["error"] = "Flash sale not found";
                return RedirectToAction(nameof(Index));
            }

            return View(flashSale);
        }

        // POST: Edit Flash Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, FlashSale flashSale)
        {
            if (id != flashSale.Id)
            {
                return NotFound();
            }

            // Get existing flash sale to preserve image if not changed
            var existingFlashSale = _unitOfWork.FlashSale.Get(f => f.Id == id);
            if (existingFlashSale == null)
            {
                return NotFound();
            }

            // Initialize ImageUrl from existing flash sale first
            flashSale.ImageUrl = existingFlashSale.ImageUrl ?? string.Empty;

            // Remove ImageUrl from ModelState validation since we handle it manually
            ModelState.Remove("ImageUrl");

            // Validate dates
            if (flashSale.EndDate <= flashSale.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload if a new image is provided
                    if (flashSale.ImageFile != null && flashSale.ImageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string flashSalePath = Path.Combine(wwwRootPath, @"images\flashsales");

                        if (!Directory.Exists(flashSalePath))
                        {
                            Directory.CreateDirectory(flashSalePath);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingFlashSale.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, existingFlashSale.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Upload new image
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(flashSale.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(flashSalePath, fileName), FileMode.Create))
                        {
                            flashSale.ImageFile.CopyTo(fileStream);
                        }
                        flashSale.ImageUrl = @"\images\flashsales\" + fileName;
                    }
                    // If no new image, ImageUrl already set from existingFlashSale above

                // Set audit fields
                AuditHelper.SetModifiedAudit(flashSale, User);
                _unitOfWork.FlashSale.Update(flashSale);
                _unitOfWork.save();
                TempData["success"] = "Flash sale updated successfully";
                return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    _logger.LogError(ex, "Error updating flash sale");
                    ModelState.AddModelError("", $"An error occurred while updating the flash sale: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                    return View(flashSale);
                }
            }

            return View(flashSale);
        }

        // GET: Flash Sale Details
        public IActionResult Details(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                TempData["error"] = "Flash sale not found";
                return RedirectToAction(nameof(Index));
            }

            return View(flashSale);
        }

        // POST: Delete Flash Sale
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                return Json(new { success = false, message = "Flash sale not found" });
            }

            // Soft delete all items first
            if (flashSale.FlashSaleItems != null && flashSale.FlashSaleItems.Any())
            {
                foreach (var item in flashSale.FlashSaleItems.ToList())
                {
                    AuditHelper.SetDeletedAudit(item, User);
                    _unitOfWork.FlashSaleItem.Remove(item);
                }
            }

            // Soft delete flash sale
            AuditHelper.SetDeletedAudit(flashSale, User);
            _unitOfWork.FlashSale.Remove(flashSale);
            _unitOfWork.save();

            return Json(new { success = true, message = "Flash sale deleted successfully" });
        }

        // POST: Toggle Active Status
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            var flashSale = _unitOfWork.FlashSale.Get(f => f.Id == id);
            
            if (flashSale == null)
            {
                return Json(new { success = false, message = "Flash sale not found" });
            }

            flashSale.IsActive = !flashSale.IsActive;
            _unitOfWork.FlashSale.Update(flashSale);
            _unitOfWork.save();

            return Json(new { 
                success = true, 
                message = $"Flash sale {(flashSale.IsActive ? "activated" : "deactivated")} successfully",
                isActive = flashSale.IsActive
            });
        }

        // GET: Get Product Info (for AJAX)
        [HttpGet]
        public IActionResult GetProductInfo(int productId)
        {
            // Get product with non-deleted variants only
            var product = _unitOfWork.product.Get(
                p => p.Id == productId && !p.IsDeleted, 
                includeProperties: "ProductVariants,ProductOptions"
            );
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            // Filter out deleted variants
            if (product.ProductVariants != null)
            {
                product.ProductVariants = product.ProductVariants
                    .Where(v => !v.IsDeleted)
                    .ToList();
            }

            // Load variant option values for variable products (excluding deleted items)
            if (product.ProductType == ProductType.Variable && product.ProductVariants != null)
            {
                foreach (var variant in product.ProductVariants.Where(v => !v.IsDeleted))
                {
                    // Load variant option values using DbContext, filtering out deleted items
                    variant.VariantOptionValues = _dbContext.ProductVariantOptionValues
                        .Include(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                        .Where(vov => vov.ProductVariantId == variant.Id 
                            && vov.OptionValue != null
                            && !vov.OptionValue.IsDeleted 
                            && vov.OptionValue.ProductOption != null
                            && !vov.OptionValue.ProductOption.IsDeleted)
                        .ToList();
                }
            }

            // Get current culture for localization
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            
            // Get localized product title
            var productTitle = (currentCulture == "ar" && !string.IsNullOrEmpty(product.TitleAr)) 
                ? product.TitleAr 
                : product.Title;

            var result = new { 
                success = true, 
                stockQuantity = product.StockQuantity,
                price = product.Price,
                title = productTitle,
                productType = (int)product.ProductType
            };

            // If variable product, include variants (only non-deleted)
            if (product.ProductType == ProductType.Variable && product.ProductVariants != null)
            {
                var variants = product.ProductVariants
                    .Where(v => !v.IsDeleted) // Filter deleted variants
                    .Select(v => 
                    {
                        // Build variant name from option values (localized), filtering deleted items
                        string variantDisplayName = "Default";
                        if (v.VariantOptionValues != null && v.VariantOptionValues.Any())
                        {
                            var optionValues = v.VariantOptionValues
                                .Where(vov => vov.OptionValue != null 
                                    && !vov.OptionValue.IsDeleted 
                                    && vov.OptionValue.ProductOption != null 
                                    && !vov.OptionValue.ProductOption.IsDeleted) // Filter deleted option values and options
                                .OrderBy(vov => vov.OptionValue?.ProductOption?.DisplayOrder ?? 0)
                                .ThenBy(vov => vov.OptionValue?.DisplayOrder ?? 0)
                                .Select(vov => {
                                    var optionName = (currentCulture == "ar" && !string.IsNullOrEmpty(vov.OptionValue?.ProductOption?.NameAr)) 
                                        ? vov.OptionValue.ProductOption.NameAr 
                                        : vov.OptionValue?.ProductOption?.Name;
                                    
                                    var optionValue = (currentCulture == "ar" && !string.IsNullOrEmpty(vov.OptionValue?.ValueAr)) 
                                        ? vov.OptionValue.ValueAr 
                                        : vov.OptionValue?.Value;
                                    
                                    return $"{optionName}: {optionValue}";
                                })
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();
                            
                            if (optionValues.Any())
                            {
                                variantDisplayName = string.Join(" / ", optionValues);
                            }
                        }
                        else if (!string.IsNullOrEmpty(v.VariantName))
                        {
                            variantDisplayName = v.VariantName;
                        }

                        return new
                        {
                            id = v.Id,
                            name = variantDisplayName,
                            price = v.Price,
                            stockQuantity = v.StockQuantity,
                            imageUrl = v.ImageUrl
                        };
                    })
                    .ToList();

                return Json(new { 
                    success = true, 
                    stockQuantity = product.StockQuantity,
                    price = product.Price,
                    title = productTitle,
                    productType = (int)product.ProductType,
                    variants = variants
                });
            }

            return Json(result);
        }
    }
}

