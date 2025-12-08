using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BrandController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BrandController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Brands
        public IActionResult Index()
        {
            return View(_unitOfWork.brand.GetAll().ToList());
        }

        // GET: Brands/Details/5
        public IActionResult Details(int? id)
        {
            var brand = _unitOfWork.brand.Get(m => m.Id == id);

            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // GET: Brands/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Brand brand)
        {
            // Remove Id from validation for Create action (it will be set by database)
            ModelState.Remove("Id");
            
            // Remove BaseEntity audit fields from validation since they're set programmatically
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifiedDate");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("IsDeleted");
            
          

            // Debug: Log ModelState errors with more detail
            if (!ModelState.IsValid)
            {
                var errorDetails = new List<string>();
                foreach (var error in ModelState)
                {
                    foreach (var errorMessage in error.Value.Errors)
                    {
                        var errorDetail = $"Field: '{error.Key}', Error: '{errorMessage.ErrorMessage}', AttemptedValue: '{error.Value.AttemptedValue}'";
                        System.Diagnostics.Debug.WriteLine(errorDetail);
                        errorDetails.Add(errorDetail);
                    }
                }
                // Add all errors to TempData for display
                TempData["ValidationErrors"] = string.Join(" | ", errorDetails);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload - this is required for create
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string brandPath = Path.Combine(wwwRootPath, @"images\brands");

                    if (!Directory.Exists(brandPath))
                    {
                        Directory.CreateDirectory(brandPath);
                    }

                    if (brand.ImageFile != null && brand.ImageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(brand.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(brandPath, fileName), FileMode.Create))
                        {
                            brand.ImageFile.CopyTo(fileStream);
                        }
                        brand.ImageUrl = @"\images\brands\" + fileName;
                    }
                   

                    // Set audit fields
                    AuditHelper.SetCreatedAudit(brand, User);
                    _unitOfWork.brand.add(brand);
                    _unitOfWork.save();
                    TempData["success"] = "Brand Created Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    System.Diagnostics.Debug.WriteLine($"Error creating brand: {ex.Message}");
                    ModelState.AddModelError("", $"An error occurred while creating the brand: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            
            // Return view with errors
            return View(brand);
        }

        // GET: Brands/Edit/5
        public IActionResult Edit(int? id)
        {
            var brand = _unitOfWork.brand.Get(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Brand brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            // Get existing brand to preserve image if not changed
            var existingBrand = _unitOfWork.brand.Get(m => m.Id == id);
            if (existingBrand == null)
            {
                return NotFound();
            }

            // Preserve existing ImageUrl if no new image is uploaded
            if (brand.ImageFile == null || brand.ImageFile.Length == 0)
            {
                brand.ImageUrl = existingBrand.ImageUrl;
            }

            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    foreach (var errorMessage in error.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Field: {error.Key}, Error: {errorMessage.ErrorMessage}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload if a new image is provided
                    if (brand.ImageFile != null && brand.ImageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string brandPath = Path.Combine(wwwRootPath, @"images\brands");

                        if (!Directory.Exists(brandPath))
                        {
                            Directory.CreateDirectory(brandPath);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingBrand.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, existingBrand.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Upload new image
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(brand.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(brandPath, fileName), FileMode.Create))
                        {
                            brand.ImageFile.CopyTo(fileStream);
                        }
                        brand.ImageUrl = @"\images\brands\" + fileName;
                    }
                    // If no new image, ImageUrl already set from existingBrand above

                    // Set audit fields
                    AuditHelper.SetModifiedAudit(brand, User);
                    _unitOfWork.brand.update(brand);
                    _unitOfWork.save();
                    TempData["success"] = "Brand Modified Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    System.Diagnostics.Debug.WriteLine($"Error updating brand: {ex.Message}");
                    ModelState.AddModelError("", $"An error occurred while updating the brand: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            
            return View(brand);
        }

        // GET: Brands/Delete/5
        public IActionResult Delete(int? id)
        {
            Brand? brand = _unitOfWork.brand.Get(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Brand? brand = _unitOfWork.brand.Get(m => m.Id == id);

            if (brand != null)
            {
                // Delete image file if exists
                if (!string.IsNullOrEmpty(brand.ImageUrl))
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    var oldImagePath = Path.Combine(wwwRootPath, brand.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Soft delete - handled by repository, but set audit fields
                AuditHelper.SetDeletedAudit(brand, User);
                _unitOfWork.brand.remove(brand);
                _unitOfWork.save();
                TempData["success"] = "Brand Deleted Successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return (_unitOfWork.brand.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
