using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    public class DeleteImageRequest
    {
        public int imageId { get; set; }
    }

    public class UpdateImageInfoRequest
    {
        public int ImageId { get; set; }
        public string? ImageInfo { get; set; }
    }

    public class DeleteInfoImageRequest
    {
        public int ProductId { get; set; }
        public int? ImageId { get; set; } // Optional, not needed since we find by marker
    }

    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDBContext _dbContext;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
        }

        // GET: Categries
        public async Task<IActionResult> Index()
        {
            // Get all products including deleted ones for admin view
            var allProducts = _dbContext.Products
                .Include(p => p.categry)
                .OrderByDescending(p => p.Id)
                .ToList();
            
            return View(allProducts);
         }

        // GET: Categries/Details/5
        public async Task<IActionResult> Details(int? id)
        {

            var Product = _unitOfWork.product.Get(m => m.Id == id);

            if (Product == null)
            {
                return NotFound();
            }

            return View(Product);
        }

        // GET: Categries/Create
        public IActionResult UpSert(int? id)
        {
            ProductVM productVM = new()
            {
                CategryList = _unitOfWork.categry.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                }),
                BrandList = _unitOfWork.brand.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                }),
                product = new Product()
                 
            };
            if(id==null || id == 0)
            {
                //create
                return View(productVM);

            }
            else
            {
                //update - load product with images, options, and variants
                // Get product directly from context to include deleted items, then filter in code
                productVM.product = _dbContext?.Products?
                    .Include(p => p.ProductImages)?
                    .Include(p => p.ProductOptions)?
                    .Include(p => p.ProductVariants)?
                    .FirstOrDefault(a => a.Id == id)!;
                
                if (productVM.product == null)
                {
                    return NotFound();
                }
                
                // Filter out deleted options and variants
                if (productVM.product.ProductOptions != null)
                {
                    productVM.product.ProductOptions = productVM.product.ProductOptions
                        .Where(o => !o.IsDeleted)
                        .ToList();
                }
                
                if (productVM.product.ProductVariants != null)
                {
                    productVM.product.ProductVariants = productVM.product.ProductVariants
                        .Where(v => !v.IsDeleted)
                        .ToList();
                } 
                
                // Load option values for each option (only non-deleted options and values)
                if (productVM.product.ProductOptions != null)
                {
                    foreach (var option in productVM.product.ProductOptions.Where(o => !o.IsDeleted))
                    {
                        option.OptionValues = _unitOfWork.ProductOptionValue.GetAll(
                            ov => ov.ProductOptionId == option.Id && !ov.IsDeleted
                        ).ToList();
                    }
                }
                
                // Load variant option values for each variant (only non-deleted variants)
                if (productVM.product.ProductVariants != null)
                {
                    foreach (var variant in productVM.product.ProductVariants.Where(v => !v.IsDeleted))
                    {
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
                
                return View(productVM);  
            }


        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult UpSert(ProductVM productVM, List<IFormFile>? files, string? saveOnly, string? imageUploadOption, IFormFile? infoImage)
        {
            // Check if this is an AJAX request (saveOnly flag)
            bool isAjaxRequest = !string.IsNullOrEmpty(saveOnly) && saveOnly == "true";
            
            // For AJAX requests, skip image validation (images can be added later)
            if (!isAjaxRequest)
            {
                bool isAtLeastOneRequired = imageUploadOption == "atLeastOne";
                bool hasExistingImages = productVM.product.Id > 0 && 
                    _dbContext.ProductImages.Any(pi => pi.ProductId == productVM.product.Id);
                
                // Validate based on selected option
                if (isAtLeastOneRequired)
                {
                    // At least one image required (either existing or new)
                    if (productVM.product.Id == 0 && (files == null || files.Count == 0))
                    {
                        ModelState.AddModelError("", "At least one image is required.");
                    }
                    else if (productVM.product.Id > 0 && !hasExistingImages && (files == null || files.Count == 0))
                    {
                        ModelState.AddModelError("", "At least one image is required.");
                    }
                }
                // For "multiple" option, no validation needed (images are optional)
            }

            // Validate slug if manually entered
            if (!string.IsNullOrWhiteSpace(productVM.product.SlugEn))
            {
                if (productVM.product.SlugEn.Length > 100)
                {
                    ModelState.AddModelError("product.SlugEn", "Slug must be 100 characters or less.");
                }
            }

            if (ModelState.IsValid)
            {
                string WWWRootPath = _webHostEnvironment.WebRootPath;
                string ProductPath = Path.Combine(WWWRootPath, @"Images\Products");

                // Handle multiple image uploads
                if (files != null && files.Count > 0)
                {
                    // Get existing product if updating (for image handling)
                    Product existingProductForImages = null;
                    if (productVM.product.Id != 0)
                    {
                        existingProductForImages = _unitOfWork.product.Get(a => a.Id == productVM.product.Id, includeProperties: "ProductImages");
                    }

                    int displayOrder = 0;
                    if (existingProductForImages != null && existingProductForImages.ProductImages != null)
                    {
                        displayOrder = existingProductForImages.ProductImages.Any() 
                            ? existingProductForImages.ProductImages.Max(pi => pi.DisplayOrder) + 1 
                            : 0;
                    }

                    foreach (var file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            string FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            
                            using (var fileStream = new FileStream(Path.Combine(ProductPath, FileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            // Set first image as main ImageUrl for backward compatibility
                            if (string.IsNullOrEmpty(productVM.product.ImageUrl))
                            {
                                productVM.product.ImageUrl = @"\Images\Products\" + FileName;
                            }

                            // Add ProductImage (ImageInfo will be set separately via UpdateImageInfo action)
                            if (productVM.product.Id == 0)
                            {
                                // New product - add to collection
                                if (productVM.product.ProductImages == null)
                                {
                                    productVM.product.ProductImages = new List<ProductImage>();
                                }
                                productVM.product.ProductImages.Add(new ProductImage
                                {
                                    ImageUrl = @"\Images\Products\" + FileName,
                                    DisplayOrder = displayOrder++
                                });
                            }
                            else
                            {
                                // Existing product - add to DbContext
                                var productImage = new ProductImage
                                {
                                    ProductId = productVM.product.Id,
                                    ImageUrl = @"\Images\Products\" + FileName,
                                    DisplayOrder = displayOrder++
                                };
                                _dbContext.ProductImages.Add(productImage);
                            }
                        }
                    }
                }

                // Generate slug for SEO-friendly URLs
                var allProducts = _dbContext.Products.AsNoTracking().ToList();
                var existingSlugsEn = allProducts.Where(p => p.Id != productVM.product.Id && !string.IsNullOrEmpty(p.SlugEn)).Select(p => p.SlugEn).ToList();
                
                // Get existing product to check if title changed (for slug regeneration logic)
                Product existingProductForSlug = null;
                if (productVM.product.Id > 0)
                {
                    existingProductForSlug = _dbContext.Products.AsNoTracking().FirstOrDefault(p => p.Id == productVM.product.Id);
                }
                
                // Generate slug (only if empty or title changed)
                if (!string.IsNullOrWhiteSpace(productVM.product.Title))
                {
                    // Check if slug needs regeneration
                    bool shouldRegenerate = string.IsNullOrWhiteSpace(productVM.product.SlugEn) ||
                                             (existingProductForSlug != null && existingProductForSlug.Title != productVM.product.Title && 
                                              productVM.product.SlugEn == existingProductForSlug.SlugEn);
                    
                    if (shouldRegenerate)
                    {
                        var baseSlug = IdealWeightNutrition.Utility.UrlSlugHelper.GenerateSlug(productVM.product.Title);
                        productVM.product.SlugEn = IdealWeightNutrition.Utility.UrlSlugHelper.GenerateUniqueSlug(baseSlug, existingSlugsEn);
                    }
                    else if (!string.IsNullOrWhiteSpace(productVM.product.SlugEn))
                    {
                        // Slug was manually edited - ensure it's clean and unique
                        var cleanedSlug = IdealWeightNutrition.Utility.UrlSlugHelper.GenerateSlug(productVM.product.SlugEn);
                        if (cleanedSlug != productVM.product.SlugEn)
                        {
                            // Clean invalid characters
                            productVM.product.SlugEn = IdealWeightNutrition.Utility.UrlSlugHelper.GenerateUniqueSlug(cleanedSlug, existingSlugsEn);
                        }
                        else
                        {
                            // Ensure uniqueness
                            productVM.product.SlugEn = IdealWeightNutrition.Utility.UrlSlugHelper.GenerateUniqueSlug(productVM.product.SlugEn, existingSlugsEn);
                        }
                    }
                }
                
                int savedProductId = 0;
                
                if (productVM.product.Id == 0)
                {
                    // Set audit fields for new product
                    AuditHelper.SetCreatedAudit(productVM.product, User);
                    _unitOfWork.product.add(productVM.product);
                    _unitOfWork.save(); // This automatically saves ProductImages too!
                    savedProductId = productVM.product.Id;
                }
                else
                {
                    // Get existing product from database to ensure proper tracking
                    var existingProduct = _dbContext.Products.FirstOrDefault(p => p.Id == productVM.product.Id);
                    if (existingProduct == null)
                    {
                        if (isAjaxRequest)
                        {
                            return Json(new { success = false, message = "Product not found" });
                        }
                        TempData["error"] = "Product not found";
                        return RedirectToAction("Index");
                    }
                    
                    // Update properties
                    existingProduct.Title = productVM.product.Title;
                    existingProduct.TitleAr = productVM.product.TitleAr;
                    existingProduct.SlugEn = productVM.product.SlugEn;
                    existingProduct.Description = productVM.product.Description;
                    existingProduct.DescriptionAr = productVM.product.DescriptionAr;
                    existingProduct.SuggestedUse = productVM.product.SuggestedUse;
                    existingProduct.SuggestedUseAr = productVM.product.SuggestedUseAr;
                    existingProduct.HealthNotes = productVM.product.HealthNotes;
                    existingProduct.HealthNotesAr = productVM.product.HealthNotesAr;
                    existingProduct.Specification = productVM.product.Specification;
                    existingProduct.SpecificationAr = productVM.product.SpecificationAr;
                    existingProduct.Price = productVM.product.Price;
                    existingProduct.ListPrice = productVM.product.ListPrice;
                    existingProduct.CategryId = productVM.product.CategryId;
                    existingProduct.BrandId = productVM.product.BrandId;
                    existingProduct.StockQuantity = productVM.product.StockQuantity;
                    existingProduct.MinimumStockAlert = productVM.product.MinimumStockAlert;
                    existingProduct.ProductType = productVM.product.ProductType;
                    existingProduct.ExpiryDate = productVM.product.ExpiryDate;
                    existingProduct.IsNew = productVM.product.IsNew;
                    existingProduct.IsTrending = productVM.product.IsTrending;
                    existingProduct.AllowFreeDelivery = productVM.product.AllowFreeDelivery;
                    existingProduct.FreeDeliveryMinimumAmount = productVM.product.FreeDeliveryMinimumAmount;
                    existingProduct.StoreCost = productVM.product.StoreCost;

                    
                    // Update ImageUrl only if provided
                    if (!string.IsNullOrEmpty(productVM.product.ImageUrl))
                    {
                        existingProduct.ImageUrl = productVM.product.ImageUrl;
                    }
                    
                    // Set audit fields
                    AuditHelper.SetModifiedAudit(existingProduct, User);
                    
                    // Update and save
                    _dbContext.Products.Update(existingProduct);
                    _dbContext.SaveChanges();
                    savedProductId = existingProduct.Id;
                }
                
                // Handle info image if provided - must be after product is saved
                // Info image is saved as a separate ProductImage entry marked with ImageInfo = "INFO_IMAGE"
                if (infoImage != null && infoImage.Length > 0 && savedProductId > 0)
                {
                    // Save the info image file first
                    string FileName = Guid.NewGuid().ToString() + Path.GetExtension(infoImage.FileName);
                    string filePath = Path.Combine(ProductPath, FileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        infoImage.CopyTo(fileStream);
                    }
                    
                    string infoImageUrl = @"\Images\Products\" + FileName;
                    
                    // Ensure all changes are saved before querying
                    _dbContext.SaveChanges();
                    
                    // Find existing info image (marked with ImageInfo = "INFO_IMAGE")
                    var existingInfoImage = _dbContext.ProductImages
                        .FirstOrDefault(pi => pi.ProductId == savedProductId && pi.ImageInfo == "INFO_IMAGE");
                    
                    if (existingInfoImage != null)
                    {
                        // Delete old info image file
                        if (!string.IsNullOrEmpty(existingInfoImage.ImageUrl))
                        {
                            try
                            {
                                string oldFilePath = Path.Combine(WWWRootPath, existingInfoImage.ImageUrl.TrimStart('\\', '/'));
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    System.IO.File.Delete(oldFilePath);
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        
                        // Update existing info image
                        existingInfoImage.ImageUrl = infoImageUrl;
                    }
                    else
                    {
                        // Create new info image as separate ProductImage entry
                        var newInfoImage = new ProductImage
                        {
                            ProductId = savedProductId,
                            ImageUrl = infoImageUrl,
                            DisplayOrder = -1, // Use -1 to mark as info image (separate from regular images)
                            ImageInfo = "INFO_IMAGE" // Marker to identify this as info image
                        };
                        _dbContext.ProductImages.Add(newInfoImage);
                    }
                    
                    _dbContext.SaveChanges();
                }
                
                // Return success messages after processing info image
                if (productVM.product.Id == 0)
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = true, productId = savedProductId, message = "Product Created Successfully" });
                    }
                    TempData["success"] = "Product Created Successfully";
                }
                else
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = true, productId = savedProductId, message = "Product Updated Successfully" });
                    }
                    TempData["success"] = "Product Updated Successfully";
                }
                
                if (isAjaxRequest)
                {
                    return Json(new { success = true, productId = productVM.product.Id });
                }
                
                return RedirectToAction("Index");
            }
            else
            {
                if (isAjaxRequest)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join(", ", errors) });
                }
                
                productVM.CategryList = _unitOfWork.categry.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                });
                productVM.BrandList = _unitOfWork.brand.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                });
                
                // Reload product with images if updating
                if (productVM.product.Id != 0)
                {
                    productVM.product = _unitOfWork.product.Get(a => a.Id == productVM.product.Id, includeProperties: "ProductImages");
                }
                
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult SaveProductStep([FromForm] int productId, [FromForm] int? productType)
        {
            try
            {
                // Get product directly from context to ensure it's tracked
                var product = _dbContext.Products.FirstOrDefault(p => p.Id == productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (productType.HasValue)
                {
                    product.ProductType = (ProductType)productType.Value;
                    
                    // Set audit fields
                    AuditHelper.SetModifiedAudit(product, User);
                    
                    // Update the product in the context
                    _dbContext.Products.Update(product);
                    _dbContext.SaveChanges();
                }

                return Json(new { success = true, message = "Step saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error saving step: {ex.Message}" });
            }
        }

        // GET: Categries/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{


        //    var Product = _unitOfWork.product.Get(m => m.Id == id);
        //    if (Product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(Product);
        //}

        //// POST: Categries/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id,Product Product)
        //{
        //    if (id != Product.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _unitOfWork.product.update(Product);
        //            _unitOfWork.save();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!ProductExists(Product.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        TempData["success"] = "product Modified Successfully";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(Product);
        //}

        // Delete Product Image
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteImage([FromBody] DeleteImageRequest request)
        {
            var productImage = _dbContext.ProductImages.FirstOrDefault(pi => pi.Id == request.imageId);
            if (productImage == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }

            // Delete physical file
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productImage.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            // Get product to check if this is the main image
            var product = _unitOfWork.product.Get(p => p.Id == productImage.ProductId);
            if (product != null && product.ImageUrl == productImage.ImageUrl)
            {
                // If this was the main image, set the first remaining image as main
                var remainingImages = _dbContext.ProductImages
                    .Where(pi => pi.ProductId == productImage.ProductId && pi.Id != request.imageId)
                    .OrderBy(pi => pi.DisplayOrder)
                    .FirstOrDefault();
                
                if (remainingImages != null)
                {
                    product.ImageUrl = remainingImages.ImageUrl;
                    _unitOfWork.product.update(product);
                }
                else
                {
                    product.ImageUrl = null;
                    _unitOfWork.product.update(product);
                }
            }

            // Delete from database
            _dbContext.ProductImages.Remove(productImage);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Image deleted successfully" });
        }

        // Update Product Image Info
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult UpdateImageInfo([FromBody] UpdateImageInfoRequest request)
        {
            if (request == null || request.ImageId <= 0)
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            var productImage = _dbContext.ProductImages.FirstOrDefault(pi => pi.Id == request.ImageId);
            if (productImage == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }

            productImage.ImageInfo = string.IsNullOrWhiteSpace(request.ImageInfo) ? null : request.ImageInfo.Trim();
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Image info updated successfully" });
        }

        [HttpPost]
        public IActionResult UploadInfoImage(IFormFile infoImage)
        {
            if (infoImage == null || infoImage.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            string WWWRootPath = _webHostEnvironment.WebRootPath;
            string ProductPath = Path.Combine(WWWRootPath, @"Images\Products");

            if (!Directory.Exists(ProductPath))
            {
                Directory.CreateDirectory(ProductPath);
            }

            string FileName = Guid.NewGuid().ToString() + Path.GetExtension(infoImage.FileName);
            string filePath = Path.Combine(ProductPath, FileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                infoImage.CopyTo(fileStream);
            }

            string imageUrl = @"\Images\Products\" + FileName;

            return Json(new { success = true, imageUrl = imageUrl, message = "Info image uploaded successfully" });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult DeleteInfoImage([FromBody] DeleteInfoImageRequest request)
        {
            if (request == null || request.ProductId <= 0)
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            try
            {
                // Find the info image by marker (ImageInfo = "INFO_IMAGE")
                var infoImage = _dbContext.ProductImages
                    .FirstOrDefault(pi => pi.ProductId == request.ProductId && pi.ImageInfo == "INFO_IMAGE");

                if (infoImage == null)
                {
                    return Json(new { success = false, message = "Info image not found" });
                }

                // Get the info image URL before deleting
                string infoImageUrl = infoImage.ImageUrl;

                // Delete the physical file if it exists
                if (!string.IsNullOrEmpty(infoImageUrl))
                {
                    try
                    {
                        string WWWRootPath = _webHostEnvironment.WebRootPath;
                        string filePath = Path.Combine(WWWRootPath, infoImageUrl.TrimStart('\\', '/'));
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }

                // Delete the ProductImage entry
                _dbContext.ProductImages.Remove(infoImage);
                _dbContext.SaveChanges();

                return Json(new { success = true, message = "Info image deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting info image: " + ex.Message });
            }
        }

        // GET: Categries/Delete/5
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
           var Product = _unitOfWork.product.Get(m => m.Id == id, includeProperties: "ProductImages");
            if (Product == null)
            {
                return Json(new { success = false, massage = "Error While Deleting" });
            }

            // Soft delete - handled by repository, but set audit fields
            AuditHelper.SetDeletedAudit(Product, User);
            _unitOfWork.product.remove(Product);
            _unitOfWork.save();
            return Json(new { success = true, massage = "Success To Delete Product" });

        }

        // POST: Undelete Product
        [HttpPost]
        public IActionResult Undelete(int? id)
        {
            try
            {
                // Get product directly from context to include deleted ones
                var product = _dbContext.Products.FirstOrDefault(p => p.Id == id);
                
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (!product.IsDeleted)
                {
                    return Json(new { success = false, message = "Product is not deleted" });
                }

                // Restore product
                product.IsDeleted = false;
                
                // Set audit fields for restoration
                AuditHelper.SetModifiedAudit(product, User);
                
                _dbContext.Products.Update(product);
                _dbContext.SaveChanges();
                
                return Json(new { success = true, message = "Product restored successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error restoring product: " + ex.Message });
            }
        }

        // POST: Categries/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    Product? Product = _unitOfWork.product.Get(m => m.Id == id);

        //    if (Product != null)
        //    {
        //        _unitOfWork.product.remove(Product);
        //        _unitOfWork.save();
        //        TempData["success"] = "product Deleted Successfully";
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

        private bool ProductExists(int id)
        {
            return (_unitOfWork.product.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        #region Variant Management API
        
        [HttpGet]
        public IActionResult GetProductOptions(int productId)
        {
            var product = _unitOfWork.product.Get(p => p.Id == productId, includeProperties: "ProductOptions");
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var options = new List<object>();
            foreach (var option in product.ProductOptions.Where(o => !o.IsDeleted).OrderBy(o => o.DisplayOrder))
            {
                var values = _unitOfWork.ProductOptionValue.GetAll(ov => ov.ProductOptionId == option.Id && !ov.IsDeleted)
                    .OrderBy(ov => ov.DisplayOrder)
                    .Select(ov => new { ov.Id, ov.Value, ov.ValueAr, ov.DisplayOrder })
                    .ToList();

                options.Add(new
                {
                    id = option.Id,
                    name = option.Name,
                    nameAr = option.NameAr,
                    displayOrder = option.DisplayOrder,
                    values = values
                });
            }

            return Json(new { success = true, options = options });
        }
        
        [HttpPost]
        public IActionResult AddOption([FromBody] AddOptionRequest request)
        {
            if (request == null || request.ProductId <= 0 || string.IsNullOrWhiteSpace(request.OptionName))
            {
                return Json(new { success = false, message = "Invalid request: Option Name (English) is required" });
            }
            
            if (string.IsNullOrWhiteSpace(request.OptionNameAr))
            {
                return Json(new { success = false, message = "Invalid request: Option Name (العربية) is required" });
            }

            var option = new ProductOption
            {
                ProductId = request.ProductId,
                Name = request.OptionName,
                NameAr = request.OptionNameAr,
                DisplayOrder = request.DisplayOrder
            };

            // Set audit fields
            AuditHelper.SetCreatedAudit(option, User);
            _unitOfWork.ProductOption.add(option);
            _unitOfWork.save();

            return Json(new { success = true, optionId = option.Id, message = "Option added successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteOption(int optionId)
        {
            try
            {
                var option = _unitOfWork.ProductOption.Get(o => o.Id == optionId);
                if (option == null)
                {
                    return Json(new { success = false, message = "Option not found" });
                }

                // Get all option values for this option
                var optionValues = _unitOfWork.ProductOptionValue.GetAll(ov => ov.ProductOptionId == optionId).ToList();
                
                // Collect all variant IDs that use any of these option values
                var allVariantIds = new HashSet<int>();
                foreach (var optionValue in optionValues)
                {
                    var variantOptionValues = _dbContext.ProductVariantOptionValues
                        .Where(vov => vov.ProductOptionValueId == optionValue.Id)
                        .Select(vov => vov.ProductVariantId)
                        .ToList();
                    
                    foreach (var variantId in variantOptionValues)
                    {
                        allVariantIds.Add(variantId);
                    }
                }

                // Check if any variants are in orders
                if (allVariantIds.Any())
                {
                    var variantsInOrders = _unitOfWork.OrderDetail.GetAll(od => 
                        od.ProductVariantId.HasValue && allVariantIds.Contains(od.ProductVariantId.Value))
                        .Any();

                    if (variantsInOrders)
                    {
                        return Json(new { 
                            success = false, 
                            message = "Cannot delete option: It is used in variants that have been ordered. Please delete or regenerate variants first." 
                        });
                    }

                    // Remove variant references from shopping carts
                    var cartItems = _unitOfWork.shoppingCart.GetAll(c => 
                        c.ProductVariantId.HasValue && allVariantIds.Contains(c.ProductVariantId.Value))
                        .ToList();
                    
                    foreach (var cartItem in cartItems)
                    {
                        cartItem.ProductVariantId = null;
                        _unitOfWork.shoppingCart.update(cartItem);
                    }
                    _unitOfWork.save();

                    // Delete all variant option value relationships for these option values
                    foreach (var optionValue in optionValues)
                    {
                        var variantOptionValues = _dbContext.ProductVariantOptionValues
                            .Where(vov => vov.ProductOptionValueId == optionValue.Id)
                            .ToList();
                        
                        if (variantOptionValues.Any())
                        {
                            _dbContext.ProductVariantOptionValues.RemoveRange(variantOptionValues);
                        }
                    }
                    _dbContext.SaveChanges();

                    // Delete the variants that used these option values
                    foreach (var variantId in allVariantIds)
                    {
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == variantId);
                        if (variant != null)
                        {
                            _unitOfWork.ProductVariant.remove(variant);
                        }
                    }
                    _unitOfWork.save();
                }

                // Now safe to delete all option values
                foreach (var value in optionValues)
                {
                    _unitOfWork.ProductOptionValue.remove(value);
                }
                _unitOfWork.save();

                // Finally delete the option
                _unitOfWork.ProductOption.remove(option);
                _unitOfWork.save();

                return Json(new { success = true, message = "Option deleted successfully" });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                return Json(new { 
                    success = false, 
                    message = "Cannot delete option: It is still in use. Please delete or regenerate variants that use this option first." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error deleting option: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public IActionResult AddOptionValue([FromBody] AddOptionValueRequest request)
        {
            if (request == null || request.OptionId <= 0 || string.IsNullOrWhiteSpace(request.Value))
            {
                return Json(new { success = false, message = "Invalid request: Value (English) is required" });
            }
            
            if (string.IsNullOrWhiteSpace(request.ValueAr))
            {
                return Json(new { success = false, message = "Invalid request: Value (العربية) is required" });
            }

            var optionValue = new ProductOptionValue
            {
                ProductOptionId = request.OptionId,
                Value = request.Value,
                ValueAr = request.ValueAr,
                DisplayOrder = request.DisplayOrder
            };

            // Set audit fields
            AuditHelper.SetCreatedAudit(optionValue, User);
            _unitOfWork.ProductOptionValue.add(optionValue);
            _unitOfWork.save();

            return Json(new { success = true, valueId = optionValue.Id, message = "Option value added successfully" });
        }

        [HttpDelete]
        public IActionResult DeleteOptionValue(int valueId)
        {
            try
            {
                var optionValue = _unitOfWork.ProductOptionValue.Get(ov => ov.Id == valueId);
                if (optionValue == null)
                {
                    return Json(new { success = false, message = "Option value not found" });
                }

                // Check if this option value is used in any variants
                var variantOptionValues = _dbContext.ProductVariantOptionValues
                    .Where(vov => vov.ProductOptionValueId == valueId)
                    .ToList();

                if (variantOptionValues.Any())
                {
                    // Get variant IDs that use this option value
                    var variantIds = variantOptionValues.Select(vov => vov.ProductVariantId).Distinct().ToList();
                    
                    // Check if any of these variants are in orders
                    var variantsInOrders = _unitOfWork.OrderDetail.GetAll(od => 
                        od.ProductVariantId.HasValue && variantIds.Contains(od.ProductVariantId.Value))
                        .Any();

                    if (variantsInOrders)
                    {
                        return Json(new { 
                            success = false, 
                            message = "Cannot delete option value: It is used in variants that have been ordered. Please delete or regenerate variants first." 
                        });
                    }

                    // Remove variant references from shopping carts
                    var cartItems = _unitOfWork.shoppingCart.GetAll(c => 
                        c.ProductVariantId.HasValue && variantIds.Contains(c.ProductVariantId.Value))
                        .ToList();
                    
                    foreach (var cartItem in cartItems)
                    {
                        cartItem.ProductVariantId = null;
                        _unitOfWork.shoppingCart.update(cartItem);
                    }
                    _unitOfWork.save();

                    // Delete the variant option value relationships
                    _dbContext.ProductVariantOptionValues.RemoveRange(variantOptionValues);
                    _dbContext.SaveChanges();

                    // Delete the variants that used this option value
                    foreach (var variantId in variantIds)
                    {
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == variantId);
                        if (variant != null)
                        {
                            _unitOfWork.ProductVariant.remove(variant);
                        }
                    }
                    _unitOfWork.save();
                }

                // Now safe to soft delete the option value (handled by repository)
                AuditHelper.SetDeletedAudit(optionValue, User);
                _unitOfWork.ProductOptionValue.remove(optionValue);
                _unitOfWork.save();

                return Json(new { success = true, message = "Option value deleted successfully" });
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                return Json(new { 
                    success = false, 
                    message = "Cannot delete option value: It is still in use. Please delete or regenerate variants that use this value first." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error deleting option value: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public IActionResult GenerateVariants(int productId)
        {
            try
            {
                // PERFORMANCE: Single query to get product with options
                var product = _dbContext.Products
                    .AsNoTracking()
                    .Include(p => p.ProductOptions.Where(o => !o.IsDeleted))
                    .FirstOrDefault(p => p.Id == productId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Filter to only non-deleted options for processing
                var nonDeletedOptions = product.ProductOptions?.Where(o => !o.IsDeleted).OrderBy(o => o.DisplayOrder).ToList() ?? new List<ProductOption>();
                if (!nonDeletedOptions.Any())
                {
                    return Json(new { success = false, message = "No options defined for this product" });
                }

                // PERFORMANCE: Batch load ALL option values in ONE query instead of N queries
                var optionIds = nonDeletedOptions.Select(o => o.Id).ToList();
                var allOptionValues = _dbContext.ProductOptionValues
                    .AsNoTracking()
                    .Where(ov => optionIds.Contains(ov.ProductOptionId) && !ov.IsDeleted)
                    .OrderBy(ov => ov.ProductOptionId)
                    .ThenBy(ov => ov.DisplayOrder)
                    .ToList();

                // Group option values by option ID
                var optionsWithValues = new List<OptionWithValues>();
                foreach (var option in nonDeletedOptions)
                {
                    var values = allOptionValues
                        .Where(ov => ov.ProductOptionId == option.Id)
                        .ToList();
                    
                    if (!values.Any())
                    {
                        return Json(new { success = false, message = $"Option '{option.Name}' has no values" });
                    }

                    optionsWithValues.Add(new OptionWithValues
                    {
                        OptionId = option.Id,
                        OptionName = option.Name,
                        Values = values.Select(v => new OptionValueInfo { Id = v.Id, Value = v.Value }).ToList()
                    });
                }

                // Generate all combinations
                var combinations = GenerateCombinations(optionsWithValues);

                // Get current ProductOptions count (only non-deleted)
                var currentOptionsCount = nonDeletedOptions.Count;

                // PERFORMANCE: Get existing variants in ONE query
                var existingVariants = _dbContext.ProductVariants
                    .AsNoTracking()
                    .Where(v => v.ProductId == productId && !v.IsDeleted)
                    .ToList();
                
                // PERFORMANCE: Count existing options in ONE query
                var existingOptionsCount = 0;
                if (existingVariants.Any())
                {
                    var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
                    existingOptionsCount = _dbContext.ProductVariantOptionValues
                        .AsNoTracking()
                        .Where(vov => existingVariantIds.Contains(vov.ProductVariantId))
                        .Select(vov => vov.OptionValue.ProductOptionId)
                        .Distinct()
                        .Count();
                }
                
                // If ProductOptions count has changed, mark all old variants as deleted (BATCH UPDATE)
                if (existingOptionsCount > 0 && existingOptionsCount != currentOptionsCount)
                {
                    var variantsToDelete = existingVariants.Select(v => v.Id).ToList();
                    foreach (var variantId in variantsToDelete)
                    {
                        var variant = _dbContext.ProductVariants.Find(variantId);
                        if (variant != null)
                        {
                            variant.IsDeleted = true;
                            variant.ModifiedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
                            AuditHelper.SetDeletedAudit(variant, User);
                        }
                    }
                    _dbContext.SaveChanges();
                    
                    // Clear existing variants list since they're now marked as deleted
                    existingVariants.Clear();
                }
                
                // PERFORMANCE: Batch load ALL existing variant option values in ONE query
                var existingVariantCombinations = new HashSet<string>();
                if (existingVariants.Any())
                {
                    var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
                    var allExistingVariantOptionValues = _dbContext.ProductVariantOptionValues
                        .AsNoTracking()
                        .Where(vov => existingVariantIds.Contains(vov.ProductVariantId))
                        .GroupBy(vov => vov.ProductVariantId)
                        .ToDictionary(g => g.Key, g => g.Select(vov => vov.ProductOptionValueId).OrderBy(id => id).ToList());
                    
                    foreach (var kvp in allExistingVariantOptionValues)
                    {
                        if (kvp.Value.Any())
                        {
                            var combinationKey = string.Join(",", kvp.Value);
                            existingVariantCombinations.Add(combinationKey);
                        }
                    }
                }

                // PERFORMANCE: Pre-load ALL option values and options for variant name generation
                var allValueIds = combinations.SelectMany(c => c).Distinct().ToList();
                var valueOptionMap = _dbContext.ProductOptionValues
                    .AsNoTracking()
                    .Where(ov => allValueIds.Contains(ov.Id))
                    .Include(ov => ov.ProductOption)
                    .ToDictionary(ov => ov.Id, ov => new { ov.Value, OptionName = ov.ProductOption != null ? ov.ProductOption.Name : "Unknown" });

                // PERFORMANCE: Batch create variants - collect all new variants first
                var basePrice = (decimal)product.Price;
                var newVariants = new List<ProductVariant>();
                var newVariantOptionValues = new List<ProductVariantOptionValue>();
                var variantNames = new Dictionary<int, string>();
                var variantCombinations = new Dictionary<int, List<int>>(); // Track which combination belongs to which variant
                var newVariantsCount = 0;
                var skippedCount = 0;
                
                foreach (var combination in combinations)
                {
                    // Create a unique key for this combination
                    var combinationKey = string.Join(",", combination.OrderBy(id => id));
                    
                    // Check if this combination already exists
                    if (existingVariantCombinations.Contains(combinationKey))
                    {
                        skippedCount++;
                        continue; // Skip creating this variant, it already exists
                    }
                    
                    // Create new variant (don't save yet)
                    var variant = new ProductVariant
                    {
                        ProductId = productId,
                        Price = basePrice,
                        ListPrice = (decimal?)product.ListPrice,
                        Price50 = (decimal?)product.Price50,
                        Price100 = (decimal?)product.Price100,
                        StockQuantity = 0,
                        MinimumStockAlert = product.MinimumStockAlert
                    };

                    var variantIndex = newVariants.Count;
                    newVariants.Add(variant);
                    variantCombinations[variantIndex] = combination;
                    
                    // Build variant name from pre-loaded data
                    var variantNameParts = combination
                        .Where(valueId => valueOptionMap.ContainsKey(valueId))
                        .Select(valueId => $"{valueOptionMap[valueId].OptionName}: {valueOptionMap[valueId].Value}")
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    
                    variantNames[variantIndex] = variantNameParts.Any() 
                        ? string.Join(" / ", variantNameParts) 
                        : "Variant";
                }

                // PERFORMANCE: Batch insert all new variants at once
                if (newVariants.Any())
                {
                    _dbContext.ProductVariants.AddRange(newVariants);
                    _dbContext.SaveChanges(); // Single save for all variants
                    newVariantsCount = newVariants.Count;

                    // PERFORMANCE: Batch create all variant option values
                    for (int i = 0; i < newVariants.Count; i++)
                    {
                        var variant = newVariants[i];
                        var combination = variantCombinations[i];
                        foreach (var valueId in combination)
                        {
                            newVariantOptionValues.Add(new ProductVariantOptionValue
                            {
                                ProductVariantId = variant.Id,
                                ProductOptionValueId = valueId
                            });
                        }
                    }
                    
                    _dbContext.ProductVariantOptionValues.AddRange(newVariantOptionValues);
                    _dbContext.SaveChanges(); // Single save for all variant option values
                }

                // PERFORMANCE: Batch load existing variant option values in ONE query
                var variants = new List<object>();
                if (existingVariants.Any())
                {
                    var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
                    var allExistingVariantOptionValues = _dbContext.ProductVariantOptionValues
                        .AsNoTracking()
                        .Include(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                        .Where(vov => existingVariantIds.Contains(vov.ProductVariantId))
                        .ToList()
                        .GroupBy(vov => vov.ProductVariantId)
                        .ToDictionary(g => g.Key, g => g.OrderBy(vov => vov.OptionValue?.ProductOption?.DisplayOrder ?? 0)
                                                          .ThenBy(vov => vov.OptionValue?.DisplayOrder ?? 0)
                                                          .ToList());
                    
                    foreach (var existingVariant in existingVariants)
                    {
                        string variantName = "Default";
                        if (allExistingVariantOptionValues.ContainsKey(existingVariant.Id))
                        {
                            var orderedValues = allExistingVariantOptionValues[existingVariant.Id];
                            variantName = string.Join(" / ", orderedValues.Select(vov => 
                                $"{vov.OptionValue?.ProductOption?.Name ?? "Unknown"}: {vov.OptionValue?.Value ?? "Unknown"}"));
                        }
                        
                        variants.Add(new
                        {
                            id = existingVariant.Id,
                            name = variantName,
                            variantName = variantName,
                            price = existingVariant.Price,
                            listPrice = existingVariant.ListPrice,
                            stockQuantity = existingVariant.StockQuantity,
                            minimumStockAlert = existingVariant.MinimumStockAlert,
                            imageUrl = existingVariant.ImageUrl ?? ""
                        });
                    }
                }

                // Add newly created variants to response
                for (int i = 0; i < newVariants.Count; i++)
                {
                    var variant = newVariants[i];
                    variants.Add(new
                    {
                        id = variant.Id,
                        name = variantNames[i],
                        variantName = variantNames[i],
                        price = variant.Price,
                        listPrice = variant.ListPrice,
                        stockQuantity = variant.StockQuantity,
                        minimumStockAlert = variant.MinimumStockAlert,
                        imageUrl = variant.ImageUrl ?? ""
                    });
                }

            // Build success message
            var message = "";
                if (newVariantsCount > 0)
                {
                    message = $"{newVariantsCount} new variant(s) added";
                }
                else
                {
                    message = "No new variants added";
                }
                
                if (skippedCount > 0)
                {
                    message += $", {skippedCount} variant(s) already exist (kept)";
                }
                
                if (existingVariants.Any())
                {
                    message += $". Total variants: {variants.Count}";
                }

                return Json(new { success = true, variants = variants, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = $"Error generating variants: {ex.Message}" 
                });
            }
        }

        [HttpPost]
        public IActionResult UpdateVariant([FromBody] UpdateVariantRequest request)
        {
            if (request == null || request.VariantId <= 0)
            {
                return Json(new { success = false, message = "Invalid request" });
            }

            // Get variant directly from context to ensure proper tracking
            var variant = _dbContext.ProductVariants.FirstOrDefault(v => v.Id == request.VariantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Variant not found" });
            }

            // Update properties
            variant.Price = request.Price;
            variant.ListPrice = request.ListPrice;
            variant.StockQuantity = request.StockQuantity;
            variant.MinimumStockAlert = request.MinimumStockAlert;
            
            // Update ExpiryDate if provided
            if (request.ExpiryDate.HasValue)
            {
                variant.ExpiryDate = request.ExpiryDate.Value;
            }
            else
            {
                variant.ExpiryDate = null;
            }

            // Set audit fields
            AuditHelper.SetModifiedAudit(variant, User);

            // Update and save
            _dbContext.ProductVariants.Update(variant);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Variant updated successfully" });
        }

        [HttpPost]
        public IActionResult UploadVariantImage(int variantId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            // Get variant directly from context to ensure proper tracking
            var variant = _dbContext.ProductVariants.FirstOrDefault(v => v.Id == variantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Variant not found" });
            }

            string WWWRootPath = _webHostEnvironment.WebRootPath;
            string ProductPath = Path.Combine(WWWRootPath, @"Images\Products\Variants");

            if (!Directory.Exists(ProductPath))
            {
                Directory.CreateDirectory(ProductPath);
            }

            // Delete old image if exists
            if (!string.IsNullOrEmpty(variant.ImageUrl))
            {
                var oldImagePath = Path.Combine(WWWRootPath, variant.ImageUrl.TrimStart('\\', '/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            string FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(ProductPath, FileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            variant.ImageUrl = @"\Images\Products\Variants\" + FileName;
            
            // Set audit fields
            AuditHelper.SetModifiedAudit(variant, User);
            
            // Update and save
            _dbContext.ProductVariants.Update(variant);
            _dbContext.SaveChanges();

            return Json(new { success = true, imageUrl = variant.ImageUrl, message = "Image uploaded successfully" });
        }

        private List<List<int>> GenerateCombinations(List<OptionWithValues> optionsWithValues)
        {
            if (optionsWithValues == null || !optionsWithValues.Any())
                return new List<List<int>>();

            var result = new List<List<int>>();
            GenerateCombinationsRecursive(optionsWithValues, 0, new List<int>(), result);
            return result;
        }

        private void GenerateCombinationsRecursive(List<OptionWithValues> optionsWithValues, int index, List<int> current, List<List<int>> result)
        {
            if (index >= optionsWithValues.Count)
            {
                result.Add(new List<int>(current));
                return;
            }

            var currentOption = optionsWithValues[index];
            foreach (var value in currentOption.Values)
            {
                current.Add(value.Id);
                GenerateCombinationsRecursive(optionsWithValues, index + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private class OptionWithValues
        {
            public int OptionId { get; set; }
            public string OptionName { get; set; }
            public List<OptionValueInfo> Values { get; set; }
        }

        private class OptionValueInfo
        {
            public int Id { get; set; }
            public string Value { get; set; }
        }

        public class AddOptionRequest
        {
            public int ProductId { get; set; }
            public string OptionName { get; set; }
            public string OptionNameAr { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class AddOptionValueRequest
        {
            public int OptionId { get; set; }
            public string Value { get; set; }
            public string ValueAr { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class UpdateVariantRequest
        {
            public int VariantId { get; set; }
            public decimal Price { get; set; }
            public decimal? ListPrice { get; set; }
            public int StockQuantity { get; set; }
            public int MinimumStockAlert { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }

        #endregion

        #region API Call
        [HttpGet]
        public IActionResult GetAll(
            string filter = "all",
            string searchValue = "",
            int start = 0,
            int length = 10,
            string sortColumn = "Id",
            string sortDirection = "desc")
        {
            try
            {
                // Start with base query
                IQueryable<Product> query = _dbContext.Products
                    .Include(p => p.categry)
                    .AsQueryable();

                // Apply filter
                if (!string.IsNullOrEmpty(filter) && filter != "all")
                {
                    switch (filter.ToLower())
                    {
                        case "active":
                            query = query.Where(p => !p.IsDeleted);
                            break;
                        case "deleted":
                            query = query.Where(p => p.IsDeleted);
                            break;
                        case "lowstock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity <= p.MinimumStockAlert);
                            break;
                        case "outofstock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity <= 0);
                            break;
                        case "instock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity > 0);
                            break;
                        case "new":
                            query = query.Where(p => !p.IsDeleted && p.IsNew);
                            break;
                        case "trending":
                            query = query.Where(p => !p.IsDeleted && p.IsTrending);
                            break;
                    }
                }

                // Apply search filter (search across multiple fields)
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(p =>
                        p.Id.ToString().Contains(searchValue) ||
                        (p.Title != null && p.Title.ToLower().Contains(searchValue)) ||
                        (p.TitleAr != null && p.TitleAr.ToLower().Contains(searchValue)) ||
                        (p.ISBN != null && p.ISBN.ToLower().Contains(searchValue)) ||
                        (p.Author != null && p.Author.ToLower().Contains(searchValue)) ||
                        (p.categry != null && p.categry.Name != null && p.categry.Name.ToLower().Contains(searchValue)) ||
                        p.Price.ToString().Contains(searchValue)
                    );
                }

                // Get total count before pagination
                var totalRecords = query.Count();

                // Apply sorting
                query = sortColumn.ToLower() switch
                {
                    "id" => sortDirection == "asc" ? query.OrderBy(p => p.Id) : query.OrderByDescending(p => p.Id),
                    "title" => sortDirection == "asc" ? query.OrderBy(p => p.Title) : query.OrderByDescending(p => p.Title),
                    "price" => sortDirection == "asc" ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price),
                    "stockquantity" => sortDirection == "asc" ? query.OrderBy(p => p.StockQuantity) : query.OrderByDescending(p => p.StockQuantity),
                    "createddate" => sortDirection == "asc" ? query.OrderBy(p => p.CreatedDate) : query.OrderByDescending(p => p.CreatedDate),
                    _ => query.OrderByDescending(p => p.Id)
                };

                // Apply pagination
                var products = query
                    .Skip(start)
                    .Take(length)
                    .ToList();

                // Map to lowercase properties for Tabulator
                var productData = products.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    titleAr = p.TitleAr,
                    isbn = p.ISBN ?? "",
                    price = p.Price,
                    listPrice = p.ListPrice,
                    storeCost = p.StoreCost ?? 0,
                    profit = CalculateProfit(p.Price, p.StoreCost),
                    profitPercentage = CalculateProfitPercentage(p.Price, p.StoreCost),
                    author = p.Author ?? "",
                    category = p.categry != null ? p.categry.Name : "",
                    stockQuantity = p.StockQuantity,
                    minimumStockAlert = p.MinimumStockAlert,
                    isDeleted = p.IsDeleted,
                    isNew = p.IsNew,
                    isTrending = p.IsTrending,
                    createdDate = p.CreatedDate
                }).ToList();

                // Return data in Tabulator format
                return Json(new
                {
                    last_page = (int)Math.Ceiling((double)totalRecords / length),
                    data = productData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error loading products: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProductStatistics()
        {
            try
            {
                var allProducts = _dbContext.Products.ToList();

                var stats = new
                {
                    all = allProducts.Count,
                    active = allProducts.Count(p => !p.IsDeleted),
                    deleted = allProducts.Count(p => p.IsDeleted),
                    lowStock = allProducts.Count(p => !p.IsDeleted && p.StockQuantity <= p.MinimumStockAlert),
                    outOfStock = allProducts.Count(p => !p.IsDeleted && p.StockQuantity <= 0),
                    inStock = allProducts.Count(p => !p.IsDeleted && p.StockQuantity > 0),
                    newProducts = allProducts.Count(p => !p.IsDeleted && p.IsNew),
                    trending = allProducts.Count(p => !p.IsDeleted && p.IsTrending)
                };

                return Json(new { success = true, stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        /// <summary>
        /// Calculate profit (Price - StoreCost)
        /// </summary>
        private double CalculateProfit(double price, double? storeCost)
        {
            if (!storeCost.HasValue || storeCost.Value <= 0)
                return 0;
            return price - storeCost.Value;
        }

        /// <summary>
        /// Calculate profit percentage ((Profit / Price) * 100)
        /// </summary>
        private double CalculateProfitPercentage(double price, double? storeCost)
        {
            if (!storeCost.HasValue || storeCost.Value <= 0 || price <= 0)
                return 0;
            var profit = price - storeCost.Value;
            return (profit / price) * 100;
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult Export(
            string filter = "all",
            string searchValue = "")
        {
            try
            {
                // Start with base query (same as GetAll)
                IQueryable<Product> query = _dbContext.Products
                    .Include(p => p.categry)
                    .AsQueryable();

                // Apply filter
                if (!string.IsNullOrEmpty(filter) && filter != "all")
                {
                    switch (filter.ToLower())
                    {
                        case "active":
                            query = query.Where(p => !p.IsDeleted);
                            break;
                        case "deleted":
                            query = query.Where(p => p.IsDeleted);
                            break;
                        case "lowstock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity <= p.MinimumStockAlert);
                            break;
                        case "outofstock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity <= 0);
                            break;
                        case "instock":
                            query = query.Where(p => !p.IsDeleted && p.StockQuantity > 0);
                            break;
                        case "new":
                            query = query.Where(p => !p.IsDeleted && p.IsNew);
                            break;
                        case "trending":
                            query = query.Where(p => !p.IsDeleted && p.IsTrending);
                            break;
                    }
                }

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    query = query.Where(p =>
                        p.Id.ToString().Contains(searchValue) ||
                        (p.Title != null && p.Title.ToLower().Contains(searchValue)) ||
                        (p.TitleAr != null && p.TitleAr.ToLower().Contains(searchValue)) ||
                        (p.ISBN != null && p.ISBN.ToLower().Contains(searchValue)) ||
                        (p.Author != null && p.Author.ToLower().Contains(searchValue)) ||
                        (p.categry != null && p.categry.Name != null && p.categry.Name.ToLower().Contains(searchValue)) ||
                        p.Price.ToString().Contains(searchValue)
                    );
                }

                // Get all products (no pagination for export)
                var products = query.OrderByDescending(p => p.Id).ToList();

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Product ID,Title (EN),Title (AR),Category,Price,List Price,Store Cost,Profit,Profit %,Stock Quantity,ISBN,Author,Is New,Is Trending,Is Deleted,Created Date");

                foreach (var product in products)
                {
                    var storeCost = product.StoreCost ?? 0;
                    var profit = CalculateProfit(product.Price, product.StoreCost);
                    var profitPercentage = CalculateProfitPercentage(product.Price, product.StoreCost);
                    var category = product.categry != null ? product.categry.Name : "";
                    
                    csv.AppendLine($"{product.Id}," +
                        $"\"{product.Title?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{product.TitleAr?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{category.Replace("\"", "\"\"")}\"," +
                        $"{product.Price:F2}," +
                        $"{product.ListPrice:F2}," +
                        $"{storeCost:F2}," +
                        $"{profit:F2}," +
                        $"{profitPercentage:F2}," +
                        $"{product.StockQuantity}," +
                        $"\"{product.ISBN?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"\"{product.Author?.Replace("\"", "\"\"") ?? ""}\"," +
                        $"{(product.IsNew ? "Yes" : "No")}," +
                        $"{(product.IsTrending ? "Yes" : "No")}," +
                        $"{(product.IsDeleted ? "Yes" : "No")}," +
                        $"{product.CreatedDate:yyyy-MM-dd HH:mm:ss}");
                }

                var fileName = $"Products_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["error"] = "Error exporting products";
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        #region Slug Regeneration
        /// <summary>
        /// Regenerates slugs for all products (useful for fixing existing products with bad slugs)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegenerateAllSlugs()
        {
            try
            {
                var allProducts = _dbContext.Products.ToList();
                var updatedCount = 0;

                foreach (var product in allProducts)
                {
                    var originalSlugEn = product.SlugEn;

                    // Get all other products' slugs for uniqueness check
                    var existingSlugsEn = allProducts
                        .Where(p => p.Id != product.Id && !string.IsNullOrEmpty(p.SlugEn))
                        .Select(p => p.SlugEn)
                        .ToList();

                    // Regenerate slug
                    if (!string.IsNullOrWhiteSpace(product.Title))
                    {
                        var baseSlugEn = UrlSlugHelper.GenerateSlug(product.Title);
                        product.SlugEn = UrlSlugHelper.GenerateUniqueSlug(baseSlugEn, existingSlugsEn);
                    }

                    // Only update if slug changed
                    if (product.SlugEn != originalSlugEn)
                    {
                        _dbContext.Products.Update(product);
                        updatedCount++;
                    }
                }

                _dbContext.SaveChanges();

                return Json(new 
                { 
                    success = true, 
                    message = $"Successfully regenerated slugs for {updatedCount} out of {allProducts.Count} products.",
                    updatedCount = updatedCount,
                    totalProducts = allProducts.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new 
                { 
                    success = false, 
                    message = $"Error regenerating slugs: {ex.Message}" 
                });
            }
        }
        #endregion
    }
}
 