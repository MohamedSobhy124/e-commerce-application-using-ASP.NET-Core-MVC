using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace BulkyBook.Areas.Admin.Controllers
{
    public class DeleteImageRequest
    {
        public int imageId { get; set; }
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
        public IActionResult UpSert(ProductVM productVM, List<IFormFile>? files, string? saveOnly)
        {
            // Check if this is an AJAX request (saveOnly flag)
            bool isAjaxRequest = !string.IsNullOrEmpty(saveOnly) && saveOnly == "true";
            
            // For AJAX requests, skip image validation (images can be added later)
            if (!isAjaxRequest)
            {
                // Validate at least one image is required
                if (productVM.product.Id == 0 && (files == null || files.Count == 0))
                {
                    ModelState.AddModelError("", "At least one image is required.");
                }
            }

            if (ModelState.IsValid)
            {
                string WWWRootPath = _webHostEnvironment.WebRootPath;
                string ProductPath = Path.Combine(WWWRootPath, @"Images\Products");

                // Handle multiple image uploads
                if (files != null && files.Count > 0)
                {
                    // Get existing product if updating
                    Product existingProduct = null;
                    if (productVM.product.Id != 0)
                    {
                        existingProduct = _unitOfWork.product.Get(a => a.Id == productVM.product.Id, includeProperties: "ProductImages");
                    }

                    int displayOrder = 0;
                    if (existingProduct != null && existingProduct.ProductImages != null)
                    {
                        displayOrder = existingProduct.ProductImages.Any() 
                            ? existingProduct.ProductImages.Max(pi => pi.DisplayOrder) + 1 
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

                            // Add ProductImage
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

                if (productVM.product.Id == 0)
                {
                    // Set audit fields for new product
                    AuditHelper.SetCreatedAudit(productVM.product, User);
                    _unitOfWork.product.add(productVM.product);
                    _unitOfWork.save(); // This automatically saves ProductImages too!
                    
                    if (isAjaxRequest)
                    {
                        return Json(new { success = true, productId = productVM.product.Id, message = "Product Created Successfully" });
                    }
                    TempData["success"] = "Product Created Successfully";
                }
                else
                {
                    // Set audit fields for updated product
                    AuditHelper.SetModifiedAudit(productVM.product, User);
                    _unitOfWork.product.update(productVM.product);
                    _unitOfWork.save();
                    
                    if (isAjaxRequest)
                    {
                        return Json(new { success = true, productId = productVM.product.Id, message = "Product Updated Successfully" });
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
                var product = _unitOfWork.product.Get(p => p.Id == productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (productType.HasValue)
                {
                    product.ProductType = (ProductType)productType.Value;
                    _unitOfWork.product.update(product);
                    _unitOfWork.save();
                }

                return Json(new { success = true, message = "Step saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

        // GET: Categries/Delete/5
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
           var Product = _unitOfWork.product.Get(m => m.Id == id, includeProperties: "ProductImages");
            if (Product == null)
            {
                return Json(new { success = false, massage = "Error While Deleting" });
            }

            // Delete main image if exists
            if (!string.IsNullOrEmpty(Product.ImageUrl))
            {
                var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, Product.ImageUrl.Trim('\\'));
                if (System.IO.File.Exists(OldImagePath))
                {
                    System.IO.File.Delete(OldImagePath);
                }
            }

            // Delete all product images
            if (Product.ProductImages != null && Product.ProductImages.Any())
            {
                foreach (var productImage in Product.ProductImages)
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productImage.ImageUrl.Trim('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _dbContext.ProductImages.RemoveRange(Product.ProductImages);
            }

            // Soft delete - handled by repository, but set audit fields
            AuditHelper.SetDeletedAudit(Product, User);
            _unitOfWork.product.remove(Product);
            _unitOfWork.save();
            return Json(new { success = true, massage = "Success To Delete Product" });

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
                    .Select(ov => new { ov.Id, ov.Value, ov.DisplayOrder })
                    .ToList();

                options.Add(new
                {
                    id = option.Id,
                    name = option.Name,
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
                return Json(new { success = false, message = "Invalid request" });
            }

            var option = new ProductOption
            {
                ProductId = request.ProductId,
                Name = request.OptionName,
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
                return Json(new { success = false, message = "Invalid request" });
            }

            var optionValue = new ProductOptionValue
            {
                ProductOptionId = request.OptionId,
                Value = request.Value,
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
                // Get product with all options (including deleted) to check count
                var product = _dbContext.Products
                    .Include(p => p.ProductOptions)
                    .FirstOrDefault(p => p.Id == productId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Filter to only non-deleted options for processing
                var nonDeletedOptions = product.ProductOptions?.Where(o => !o.IsDeleted).ToList() ?? new List<ProductOption>();
                if (!nonDeletedOptions.Any())
                {
                    return Json(new { success = false, message = "No options defined for this product" });
                }

                // Load all option values (only non-deleted options and values)
                var optionsWithValues = new List<OptionWithValues>();
                foreach (var option in nonDeletedOptions.OrderBy(o => o.DisplayOrder))
                {
                    var values = _unitOfWork.ProductOptionValue.GetAll(ov => ov.ProductOptionId == option.Id && !ov.IsDeleted)
                        .OrderBy(ov => ov.DisplayOrder)
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

                // Get existing variants (only non-deleted ones) with their option values to check for duplicates
                var existingVariants = _unitOfWork.ProductVariant.GetAll(v => v.ProductId == productId && !v.IsDeleted).ToList();
                
                // Count ProductOptions from existing variants by checking unique ProductOptionIds in ProductVariantOptionValues
                var existingOptionsCount = 0;
                if (existingVariants.Any())
                {
                    var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
                    existingOptionsCount = _dbContext.ProductVariantOptionValues
                        .Include(vov => vov.OptionValue)
                        .Where(vov => existingVariantIds.Contains(vov.ProductVariantId))
                        .Select(vov => vov.OptionValue.ProductOptionId)
                        .Distinct()
                        .Count();
                }
                
                // If ProductOptions count has changed, mark all old variants as deleted
                if (existingOptionsCount > 0 && existingOptionsCount != currentOptionsCount)
                {
                    foreach (var variant in existingVariants)
                    {
                        variant.IsDeleted = true;
                        _unitOfWork.ProductVariant.Update(variant);
                    }
                    _unitOfWork.save();
                    
                    // Clear existing variants list since they're now marked as deleted
                    existingVariants.Clear();
                }
                
                // Load option values for existing variants (only non-deleted ones)
                var existingVariantCombinations = new HashSet<string>();
                foreach (var variant in existingVariants)
                {
                    try
                    {
                        var variantOptionValues = _dbContext.ProductVariantOptionValues
                            .Where(vov => vov.ProductVariantId == variant.Id)
                            .Select(vov => vov.ProductOptionValueId)
                            .OrderBy(id => id)
                            .ToList();
                        
                        // Create a unique key for this combination
                        if (variantOptionValues.Any())
                        {
                            var combinationKey = string.Join(",", variantOptionValues);
                            existingVariantCombinations.Add(combinationKey);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue - variant might not have option values yet
                        // This shouldn't stop the generation process
                        continue;
                    }
                }

                // Create new variants (only for combinations that don't exist)
                var basePrice = (decimal)product.Price;
                var variants = new List<object>();
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
                    
                    // Create new variant
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

                    _unitOfWork.ProductVariant.add(variant);
                    _unitOfWork.save();

                    // Add variant option values
                    foreach (var valueId in combination)
                    {
                        var variantOptionValue = new ProductVariantOptionValue
                        {
                            ProductVariantId = variant.Id,
                            ProductOptionValueId = valueId
                        };
                        _dbContext.ProductVariantOptionValues.Add(variantOptionValue);
                    }
                    _unitOfWork.save();
                    newVariantsCount++;

                    // Get variant name for response
                    var variantName = string.Join(" / ", combination.Select(valueId =>
                    {
                        var value = _unitOfWork.ProductOptionValue.Get(ov => ov.Id == valueId);
                        if (value != null)
                        {
                            var option = _unitOfWork.ProductOption.Get(o => o.Id == value.ProductOptionId);
                            return $"{option?.Name}: {value.Value}";
                        }
                        return "";
                    }).Where(s => !string.IsNullOrEmpty(s)));

                    variants.Add(new
                    {
                        id = variant.Id,
                        name = variantName,
                        variantName = variantName,
                        price = variant.Price,
                        listPrice = variant.ListPrice,
                        stockQuantity = variant.StockQuantity,
                        minimumStockAlert = variant.MinimumStockAlert,
                        imageUrl = variant.ImageUrl ?? ""
                    });
                }
            
                // Also add existing variants to the response so they show in the table
                foreach (var existingVariant in existingVariants)
                {
                try
                {
                    // Load variant option values to get the name
                    var variantOptionValues = _dbContext.ProductVariantOptionValues
                        .Include(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                        .Where(vov => vov.ProductVariantId == existingVariant.Id)
                        .ToList();
                    
                    string variantName = "Default";
                    if (variantOptionValues.Any())
                    {
                        var orderedValues = variantOptionValues
                            .OrderBy(vov => vov.OptionValue?.ProductOption?.DisplayOrder ?? 0)
                            .ThenBy(vov => vov.OptionValue?.DisplayOrder ?? 0)
                            .ToList();
                        
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
                catch (Exception ex)
                {
                    // If we can't load option values, still add the variant with a default name
                    variants.Add(new
                    {
                        id = existingVariant.Id,
                        name = "Variant",
                        variantName = "Variant",
                        price = existingVariant.Price,
                        listPrice = existingVariant.ListPrice,
                        stockQuantity = existingVariant.StockQuantity,
                        minimumStockAlert = existingVariant.MinimumStockAlert,
                        imageUrl = existingVariant.ImageUrl ?? ""
                    });
                }
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

            var variant = _unitOfWork.ProductVariant.Get(v => v.Id == request.VariantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Variant not found" });
            }

            variant.Price = request.Price;
            variant.ListPrice = request.ListPrice;
            variant.StockQuantity = request.StockQuantity;
            variant.MinimumStockAlert = request.MinimumStockAlert;

            _unitOfWork.ProductVariant.Update(variant);
            _unitOfWork.save();

            return Json(new { success = true, message = "Variant updated successfully" });
        }

        [HttpPost]
        public IActionResult UploadVariantImage(int variantId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var variant = _unitOfWork.ProductVariant.Get(v => v.Id == variantId);
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
                var oldImagePath = Path.Combine(WWWRootPath, variant.ImageUrl.Trim('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            string FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(ProductPath, FileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            variant.ImageUrl = @"\Images\Products\Variants\" + FileName;
            _unitOfWork.ProductVariant.Update(variant);
            _unitOfWork.save();

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
            public int DisplayOrder { get; set; }
        }

        public class AddOptionValueRequest
        {
            public int OptionId { get; set; }
            public string Value { get; set; }
            public int DisplayOrder { get; set; }
        }

        public class UpdateVariantRequest
        {
            public int VariantId { get; set; }
            public decimal Price { get; set; }
            public decimal? ListPrice { get; set; }
            public int StockQuantity { get; set; }
            public int MinimumStockAlert { get; set; }
        }

        #endregion

        #region API Call
        [HttpGet]
        public IActionResult GetAll() {
            // Get all products including deleted ones for admin view
            // Order by ID descending so newest products appear first
            var allProducts = _dbContext.Products
                .Include(p => p.categry)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    isbn = p.ISBN ?? "",
                    price = p.Price,
                    author = p.Author ?? "",
                    categry = new { name = p.categry != null ? p.categry.Name : "" },
                    isDeleted = p.IsDeleted
                })
                .ToList();
            
            return Json(new { data = allProducts });
        }
        #endregion
    }
}
 