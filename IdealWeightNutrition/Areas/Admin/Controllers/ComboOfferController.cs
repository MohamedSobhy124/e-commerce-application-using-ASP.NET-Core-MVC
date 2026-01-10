using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
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

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ComboOfferController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ILogger<ComboOfferController> _logger;
        private readonly ApplicationDBContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ComboOfferController(
            IUnitOfWork unitOfWork, 
            IEmailSender emailSender,
            IStringLocalizer<SharedResources> localizer,
            ILogger<ComboOfferController> logger,
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

        // GET: Combo Offers List
        public IActionResult Index()
        {
            var comboOffers = _unitOfWork.ComboOffer.GetAll(includeProperties: "ComboOfferItems");
            return View(comboOffers);
        }

        // GET: Create Combo Offer
        public IActionResult Create()
        {
            var comboOffer = new ComboOffer
            {
                StartDate = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                EndDate = IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(30),
                MinimumQuantity = 1
            };
            return View(comboOffer);
        }

        // POST: Create Combo Offer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ComboOffer comboOffer, List<IFormFile>? files, bool notifySubscribers = false)
        {
            // Debug: Log received files
            _logger.LogInformation($"Create ComboOffer - Received {files?.Count ?? 0} files");
            if (files != null)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    _logger.LogInformation($"File {i}: {(files[i] == null ? "null" : $"{files[i].FileName} ({files[i].Length} bytes)")}");
                }
            }

            // Initialize ImageUrl to empty string to avoid null validation errors
            if (string.IsNullOrEmpty(comboOffer.ImageUrl))
            {
                comboOffer.ImageUrl = string.Empty;
            }

            // Validate at least one image is required
            bool hasExistingImages = false;
            if (files == null || files.Count == 0 || files.All(f => f == null || f.Length == 0))
            {
                ModelState.AddModelError("", "At least one image is required.");
            }

            // Remove ImageUrl from ModelState validation since we handle it manually
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ImageFile");
            ModelState.Remove("ComboOfferItems");
            ModelState.Remove("ComboOfferImages");
            ModelState.Remove("OriginalTotalPrice");
            ModelState.Remove("DiscountPercentage");
            ModelState.Remove("TotalSavings");

            // Validate dates
            if (comboOffer.EndDate <= comboOffer.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            // Validate combo price
            if (comboOffer.ComboPrice <= 0)
            {
                ModelState.AddModelError("ComboPrice", "Combo price must be greater than 0");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle multiple image uploads
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string comboOfferPath = Path.Combine(wwwRootPath, @"images\combooffers");

                    if (!Directory.Exists(comboOfferPath))
                    {
                        Directory.CreateDirectory(comboOfferPath);
                    }

                    // Save all images
                    if (files != null && files.Any(f => f != null && f.Length > 0))
                    {
                        var validFiles = files.Where(f => f != null && f.Length > 0).ToList();
                        _logger.LogInformation($"Processing {validFiles.Count} valid files");
                        
                        // Set first image as main ImageUrl for backward compatibility
                        if (validFiles.Count > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(validFiles[0].FileName);
                            string filePath = Path.Combine(comboOfferPath, fileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await validFiles[0].CopyToAsync(fileStream);
                            }

                            comboOffer.ImageUrl = @"/images/combooffers/" + fileName;
                            _logger.LogInformation($"Saved main image: {fileName}");
                        }

                        _unitOfWork.ComboOffer.add(comboOffer);
                        _unitOfWork.save();

                        // Save all additional images (starting from index 1)
                        if (validFiles.Count > 1)
                        {
                            int displayOrder = 1; // Start from 1 since first image is already saved as ImageUrl
                            _logger.LogInformation($"Saving {validFiles.Count - 1} additional images");
                            
                            for (int i = 1; i < validFiles.Count; i++)
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(validFiles[i].FileName);
                                string filePath = Path.Combine(comboOfferPath, fileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await validFiles[i].CopyToAsync(fileStream);
                                }

                                var comboOfferImage = new ComboOfferImage
                                {
                                    ComboOfferId = comboOffer.Id,
                                    ImageUrl = @"/images/combooffers/" + fileName,
                                    DisplayOrder = displayOrder++
                                };

                                _dbContext.ComboOfferImages.Add(comboOfferImage);
                                _logger.LogInformation($"Added image {i}: {fileName} with display order {displayOrder - 1}");
                            }
                            _unitOfWork.save();
                            _logger.LogInformation($"Saved {validFiles.Count - 1} additional images");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No valid files to save");
                        _unitOfWork.ComboOffer.add(comboOffer);
                        _unitOfWork.save();
                    }

                    TempData["success"] = _localizer["ComboOfferCreatedSuccessfully"].ToString();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating combo offer");
                    TempData["error"] = _localizer["ErrorCreatingComboOffer"].ToString() + ": " + ex.Message;
                }
            }

            return View(comboOffer);
        }

        // GET: Edit Combo Offer
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id.Value);
            if (comboOffer == null)
            {
                return NotFound();
            }

            // Load images if not already loaded
            if (comboOffer.ComboOfferImages == null || !comboOffer.ComboOfferImages.Any())
            {
                comboOffer.ComboOfferImages = _dbContext.ComboOfferImages
                    .Where(img => img.ComboOfferId == id.Value)
                    .OrderBy(img => img.DisplayOrder)
                    .ToList();
            }

            return View(comboOffer);
        }

        // POST: Edit Combo Offer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ComboOffer comboOffer, List<IFormFile>? files, string? deletedImageIds)
        {
            // Debug: Log received files
            _logger.LogInformation($"Edit ComboOffer {id} - Received {files?.Count ?? 0} files");
            if (files != null)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    _logger.LogInformation($"File {i}: {(files[i] == null ? "null" : $"{files[i].FileName} ({files[i].Length} bytes)")}");
                }
            }

            if (id != comboOffer.Id)
            {
                return NotFound();
            }

            // Validate at least one image exists (either existing or new)
            var existingImages = _dbContext.ComboOfferImages.Where(img => img.ComboOfferId == id).ToList();
            bool hasNewImages = files != null && files.Any(f => f != null && f.Length > 0);
            bool hasExistingImages = existingImages.Any() || !string.IsNullOrEmpty(comboOffer.ImageUrl);
            
            // Check if images are being deleted
            List<int> deletedIds = new List<int>();
            bool deleteMainImage = false;
            if (!string.IsNullOrEmpty(deletedImageIds))
            {
                var deletedParts = deletedImageIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in deletedParts)
                {
                    if (part.Trim().ToLower() == "main")
                    {
                        deleteMainImage = true;
                    }
                    else if (int.TryParse(part, out int imgId))
                    {
                        deletedIds.Add(imgId);
                    }
                }
            }
            
            int remainingImages = existingImages.Count - deletedIds.Count + (hasNewImages ? files.Count(f => f != null && f.Length > 0) : 0);
            if (deleteMainImage && !hasExistingImages)
            {
                remainingImages--; // Account for main image deletion
            }
            
            if (remainingImages < 1)
            {
                ModelState.AddModelError("", "At least one image is required.");
            }

            // Remove calculated properties and navigation properties from validation
            ModelState.Remove("ImageFile");
            ModelState.Remove("ComboOfferItems");
            ModelState.Remove("ComboOfferImages");
            ModelState.Remove("OriginalTotalPrice");
            ModelState.Remove("DiscountPercentage");
            ModelState.Remove("TotalSavings");
            ModelState.Remove("ImageUrl"); // Remove ImageUrl validation as it's optional now
            ModelState.Remove("IsCurrentlyActive");
            ModelState.Remove("HasStarted");
            ModelState.Remove("HasEnded");
            ModelState.Remove("HasAvailableStock");
            ModelState.Remove("TimeRemaining");
            ModelState.Remove("TotalProducts");
            ModelState.Remove("RequiredProductsCount");
            ModelState.Remove("IsDeleted");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifiedDate");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");

            // Log ModelState errors before validation
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Edit ComboOffer {id} - ModelState is invalid. Errors:");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        foreach (var err in error.Value.Errors)
                        {
                            _logger.LogWarning($"  - {error.Key}: {err.ErrorMessage}");
                        }
                    }
                }
            }

            // Validate dates
            if (comboOffer.EndDate <= comboOffer.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            // Validate combo price
            if (comboOffer.ComboPrice <= 0)
            {
                ModelState.AddModelError("ComboPrice", "Combo price must be greater than 0");
            }

            // Log ModelState errors after validation
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"Edit ComboOffer {id} - ModelState is invalid after validation. Errors:");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        foreach (var err in error.Value.Errors)
                        {
                            _logger.LogWarning($"  - {error.Key}: {err.ErrorMessage}");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingComboOffer = _unitOfWork.ComboOffer.Get(u => u.Id == id);
                    if (existingComboOffer == null)
                    {
                        return NotFound();
                    }

                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string comboOfferPath = Path.Combine(wwwRootPath, @"images\combooffers");

                    if (!Directory.Exists(comboOfferPath))
                    {
                        Directory.CreateDirectory(comboOfferPath);
                    }

                    // Handle deleted images
                    if (deletedIds.Any())
                    {
                        var imagesToDelete = _dbContext.ComboOfferImages.Where(img => deletedIds.Contains(img.Id)).ToList();
                        foreach (var img in imagesToDelete)
                        {
                            var imagePath = Path.Combine(wwwRootPath, img.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            _dbContext.ComboOfferImages.Remove(img);
                        }
                    }
                    
                    // Handle main image deletion
                    if (deleteMainImage && !string.IsNullOrEmpty(existingComboOffer.ImageUrl))
                    {
                        var mainImagePath = Path.Combine(wwwRootPath, existingComboOffer.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(mainImagePath))
                        {
                            System.IO.File.Delete(mainImagePath);
                        }
                        existingComboOffer.ImageUrl = string.Empty;
                    }

                    // Handle new image uploads
                    if (files != null && files.Any(f => f != null && f.Length > 0))
                    {
                        var validFiles = files.Where(f => f != null && f.Length > 0).ToList();
                        _logger.LogInformation($"Edit: Processing {validFiles.Count} valid files");
                        
                        // Get current max display order from remaining images (after deletions)
                        var imagesAfterDeletion = _dbContext.ComboOfferImages
                            .Where(img => img.ComboOfferId == id && !deletedIds.Contains(img.Id))
                            .ToList();
                        
                        int maxDisplayOrder = imagesAfterDeletion.Any() 
                            ? imagesAfterDeletion.Max(img => img.DisplayOrder) 
                            : 0;
                        
                        _logger.LogInformation($"Edit: Current max display order: {maxDisplayOrder}, Images after deletion: {imagesAfterDeletion.Count}");
                        
                        // If main image was deleted and we have new files, set first new file as main ImageUrl
                        if (deleteMainImage && validFiles.Count > 0 && string.IsNullOrEmpty(existingComboOffer.ImageUrl))
                        {
                            _logger.LogInformation("Edit: Main image was deleted, setting first new file as main ImageUrl");
                            
                            string firstFileName = Guid.NewGuid().ToString() + Path.GetExtension(validFiles[0].FileName);
                            string firstFilePath = Path.Combine(comboOfferPath, firstFileName);

                            using (var fileStream = new FileStream(firstFilePath, FileMode.Create))
                            {
                                await validFiles[0].CopyToAsync(fileStream);
                            }

                            existingComboOffer.ImageUrl = @"/images/combooffers/" + firstFileName;
                            _logger.LogInformation($"Edit: Saved main image: {firstFileName}");
                            
                            // Add remaining files as additional images
                            if (validFiles.Count > 1)
                            {
                                _logger.LogInformation($"Edit: Adding {validFiles.Count - 1} additional images");
                                
                                for (int i = 1; i < validFiles.Count; i++)
                                {
                                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(validFiles[i].FileName);
                                    string filePath = Path.Combine(comboOfferPath, fileName);

                                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await validFiles[i].CopyToAsync(fileStream);
                                    }

                                    var comboOfferImage = new ComboOfferImage
                                    {
                                        ComboOfferId = id,
                                        ImageUrl = @"/images/combooffers/" + fileName,
                                        DisplayOrder = ++maxDisplayOrder
                                    };

                                    _dbContext.ComboOfferImages.Add(comboOfferImage);
                                    _logger.LogInformation($"Edit: Added image {i}: {fileName} with display order {maxDisplayOrder}");
                                }
                            }
                        }
                        else
                        {
                            // Add all new files as additional images
                            _logger.LogInformation($"Edit: Adding {validFiles.Count} new files as additional images");
                            
                            foreach (var file in validFiles)
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                string filePath = Path.Combine(comboOfferPath, fileName);

                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(fileStream);
                                }

                                var comboOfferImage = new ComboOfferImage
                                {
                                    ComboOfferId = id,
                                    ImageUrl = @"/images/combooffers/" + fileName,
                                    DisplayOrder = ++maxDisplayOrder
                                };

                                _dbContext.ComboOfferImages.Add(comboOfferImage);
                                _logger.LogInformation($"Edit: Added image: {fileName} with display order {maxDisplayOrder}");
                            }
                        }
                        
                        // Save the new images
                        _unitOfWork.save();
                        _logger.LogInformation($"Edit: Saved {validFiles.Count} new images");
                    }
                    else
                    {
                        _logger.LogInformation("Edit: No new files to add");
                    }

                    // Update main ImageUrl if it's empty and we have images (for backward compatibility)
                    if (string.IsNullOrEmpty(existingComboOffer.ImageUrl))
                    {
                        var firstImage = _dbContext.ComboOfferImages
                            .Where(img => img.ComboOfferId == id)
                            .OrderBy(img => img.DisplayOrder)
                            .FirstOrDefault();
                        
                        if (firstImage != null)
                        {
                            existingComboOffer.ImageUrl = firstImage.ImageUrl;
                        }
                    }

                    // Update other properties
                    existingComboOffer.Name = comboOffer.Name;
                    existingComboOffer.NameAr = comboOffer.NameAr;
                    existingComboOffer.Description = comboOffer.Description;
                    existingComboOffer.DescriptionAr = comboOffer.DescriptionAr;
                    existingComboOffer.ComboPrice = comboOffer.ComboPrice;
                    existingComboOffer.StartDate = comboOffer.StartDate;
                    existingComboOffer.EndDate = comboOffer.EndDate;
                    existingComboOffer.IsActive = comboOffer.IsActive;
                    existingComboOffer.MinimumQuantity = comboOffer.MinimumQuantity;
                    existingComboOffer.MaximumQuantity = comboOffer.MaximumQuantity;
                    existingComboOffer.DisplayOrder = comboOffer.DisplayOrder;

                    _unitOfWork.ComboOffer.Update(existingComboOffer);
                    _unitOfWork.save();

                    TempData["success"] = _localizer["ComboOfferUpdatedSuccessfully"].ToString();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating combo offer");
                    TempData["error"] = _localizer["ErrorUpdatingComboOffer"].ToString() + ": " + ex.Message;
                }
            }

            // If ModelState is invalid, reload combo offer with items and images for display
            if (!ModelState.IsValid)
            {
                // Log all validation errors
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();
                
                _logger.LogWarning($"Edit ComboOffer {id} - Validation failed. Errors: {string.Join(", ", errors.Select(e => $"{e.Field}: {string.Join(", ", e.Errors)}"))}");
                
                // Add error message to TempData
                var errorMessages = errors.SelectMany(e => e.Errors).ToList();
                TempData["error"] = "Validation failed: " + string.Join("; ", errorMessages);
            }
            
            // Reload combo offer with items and images for display
            comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id);
            if (comboOffer.ComboOfferImages == null || !comboOffer.ComboOfferImages.Any())
            {
                comboOffer.ComboOfferImages = _dbContext.ComboOfferImages
                    .Where(img => img.ComboOfferId == id)
                    .OrderBy(img => img.DisplayOrder)
                    .ToList();
            }
            return View(comboOffer);
        }

        // GET: Combo Offer Details
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id.Value);
            if (comboOffer == null)
            {
                return NotFound();
            }

            return View(comboOffer);
        }

        // GET: Delete Combo Offer
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id.Value);
            if (comboOffer == null)
            {
                return NotFound();
            }

            return View(comboOffer);
        }

        // POST: Delete Combo Offer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int id)
        {
            var comboOffer = _unitOfWork.ComboOffer.Get(u => u.Id == id);
            if (comboOffer == null)
            {
                return NotFound();
            }

            try
            {
                // Soft delete
                _unitOfWork.ComboOffer.Remove(comboOffer);
                _unitOfWork.save();

                TempData["success"] = _localizer["ComboOfferDeletedSuccessfully"].ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting combo offer");
                TempData["error"] = _localizer["ErrorDeletingComboOffer"].ToString() + ": " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Add Products to Combo Offer
        public IActionResult AddProducts(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id.Value);
            if (comboOffer == null)
            {
                return NotFound();
            }

            // Load variant option values for combo offer items
            if (comboOffer.ComboOfferItems != null)
            {
                foreach (var item in comboOffer.ComboOfferItems.Where(i => !i.IsDeleted && i.ProductVariantId.HasValue))
                {
                    if (item.ProductVariant != null)
                    {
                        // Load variant option values using DbContext
                        item.ProductVariant.VariantOptionValues = _dbContext.ProductVariantOptionValues
                            .Include(vov => vov.OptionValue)
                                .ThenInclude(ov => ov.ProductOption)
                            .Where(vov => vov.ProductVariantId == item.ProductVariant.Id)
                            .ToList();
                    }
                }
            }

            // Get all products that are not deleted and have stock
            var allProducts = _unitOfWork.product.GetAll(
                filter: p => !p.IsDeleted && p.StockQuantity > 0,
                includeProperties: "ProductImages,categry"
            ).ToList();

            // Get products already in combo
            var existingProductIds = comboOffer.ComboOfferItems
                .Where(item => !item.IsDeleted)
                .Select(item => item.ProductId)
                .ToList();

            // Get current culture for localization
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            var currencySymbol = CurrencyHelper.GetCurrencySymbol(currentCulture);
            
            ViewBag.AvailableProducts = allProducts
                .Where(p => !existingProductIds.Contains(p.Id))
                .Select(p => {
                    // Get localized product title
                    var productTitle = (currentCulture == "ar" && !string.IsNullOrEmpty(p.TitleAr)) 
                        ? p.TitleAr 
                        : p.Title;
                    
                    // Format price with currency symbol
                    var priceText = $"{currencySymbol}{p.Price:N2}";
                    
                    // Get localized "Stock" text
                    var stockText = _localizer["Stock"]?.Value ?? "Stock";
                    
                    return new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = $"{productTitle} - {priceText} ({stockText}: {p.StockQuantity})"
                    };
                })
                .ToList();

            ViewBag.ComboOffer = comboOffer;
            return View(comboOffer);
        }

        // POST: Add Product to Combo Offer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(int comboOfferId, int productId, int? productVariantId, int quantity, int displayOrder, bool isRequired)
        {
            var comboOffer = _unitOfWork.ComboOffer.Get(u => u.Id == comboOfferId);
            if (comboOffer == null)
            {
                return NotFound();
            }

            var product = _unitOfWork.product.Get(u => u.Id == productId);
            if (product == null)
            {
                TempData["error"] = _localizer["ProductNotFound"].ToString();
                return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
            }

            // Check if product already exists in combo
            var existingItem = _unitOfWork.ComboOfferItem.Get(
                item => item.ComboOfferId == comboOfferId && 
                       item.ProductId == productId && 
                       (productVariantId == null || item.ProductVariantId == productVariantId) &&
                       !item.IsDeleted
            );

            if (existingItem != null)
            {
                TempData["error"] = _localizer["ProductAlreadyInCombo"].ToString();
                return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
            }

            // Validate stock
            int availableStock = 0;
            if (productVariantId.HasValue)
            {
                var variant = _unitOfWork.ProductVariant.Get(v => v.Id == productVariantId.Value && !v.IsDeleted);
                if (variant == null)
                {
                    TempData["error"] = _localizer["ProductVariantNotFound"].ToString();
                    return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
                }
                availableStock = variant.StockQuantity;
            }
            else
            {
                availableStock = product.StockQuantity;
            }

            if (quantity > availableStock)
            {
                TempData["error"] = _localizer["InsufficientStock"].ToString() + $" (Available: {availableStock})";
                return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
            }

            try
            {
                var comboOfferItem = new ComboOfferItem
                {
                    ComboOfferId = comboOfferId,
                    ProductId = productId,
                    ProductVariantId = productVariantId,
                    Quantity = quantity,
                    DisplayOrder = displayOrder,
                    IsRequired = isRequired
                };

                _unitOfWork.ComboOfferItem.add(comboOfferItem);
                _unitOfWork.save();

                TempData["success"] = _localizer["ProductAddedToCombo"].ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product to combo");
                TempData["error"] = _localizer["ErrorAddingProductToCombo"].ToString() + ": " + ex.Message;
            }

            return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
        }

        // POST: Remove Product from Combo Offer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveProduct(int comboOfferId, int itemId)
        {
            var comboOfferItem = _unitOfWork.ComboOfferItem.Get(u => u.Id == itemId && u.ComboOfferId == comboOfferId);
            if (comboOfferItem == null)
            {
                return NotFound();
            }

            try
            {
                _unitOfWork.ComboOfferItem.Remove(comboOfferItem);
                _unitOfWork.save();

                TempData["success"] = _localizer["ProductRemovedFromCombo"].ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product from combo");
                TempData["error"] = _localizer["ErrorRemovingProductFromCombo"].ToString() + ": " + ex.Message;
            }

            return RedirectToAction(nameof(AddProducts), new { id = comboOfferId });
        }

        // POST: Update Combo Offer Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateItem(int itemId, int quantity, int displayOrder, bool isRequired)
        {
            var comboOfferItem = _unitOfWork.ComboOfferItem.Get(u => u.Id == itemId);
            if (comboOfferItem == null)
            {
                return NotFound();
            }

            // Validate stock
            int availableStock = 0;
            if (comboOfferItem.ProductVariantId.HasValue)
            {
                var variant = _unitOfWork.ProductVariant.Get(v => v.Id == comboOfferItem.ProductVariantId.Value && !v.IsDeleted);
                if (variant != null)
                {
                    availableStock = variant.StockQuantity;
                }
            }
            else
            {
                var product = _unitOfWork.product.Get(p => p.Id == comboOfferItem.ProductId);
                if (product != null)
                {
                    availableStock = product.StockQuantity;
                }
            }

            if (quantity > availableStock)
            {
                TempData["error"] = _localizer["InsufficientStock"].ToString() + $" (Available: {availableStock})";
                return RedirectToAction(nameof(AddProducts), new { id = comboOfferItem.ComboOfferId });
            }

            try
            {
                comboOfferItem.Quantity = quantity;
                comboOfferItem.DisplayOrder = displayOrder;
                comboOfferItem.IsRequired = isRequired;

                _unitOfWork.ComboOfferItem.Update(comboOfferItem);
                _unitOfWork.save();

                TempData["success"] = _localizer["ComboOfferItemUpdated"].ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating combo offer item");
                TempData["error"] = _localizer["ErrorUpdatingComboOfferItem"].ToString() + ": " + ex.Message;
            }

            return RedirectToAction(nameof(AddProducts), new { id = comboOfferItem.ComboOfferId });
        }

        // GET: Get Product Info (for variant selection)
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
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
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


