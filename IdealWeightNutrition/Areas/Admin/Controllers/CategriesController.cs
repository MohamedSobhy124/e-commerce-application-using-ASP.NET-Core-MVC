using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize (Roles =SD.Role_Admin)]
    public class CategriesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategriesController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Categries
        public  IActionResult Index()
        {
            return
                        View(_unitOfWork.categry.GetAll().ToList());

        }

        // GET: Categries/Details/5
        public IActionResult Details(int? id)
        {

            var categry = _unitOfWork.categry.Get(m => m.Id == id);

            if (categry == null)
            {
                return NotFound();
            }

            return View(categry);
        }

        // GET: Categries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Categry categry)
        {
            // Remove Id from validation for Create action (it will be set by database)
            ModelState.Remove("Id");
            
            // Remove BaseEntity audit fields from validation since they're set programmatically
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifiedDate");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("IsDeleted");
            
            // Validate image is required
            if (categry.ImageFile == null || categry.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Image is required.");
            }

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
                    string categoryPath = Path.Combine(wwwRootPath, @"images\categories");

                    if (!Directory.Exists(categoryPath))
                    {
                        Directory.CreateDirectory(categoryPath);
                    }

                    if (categry.ImageFile != null && categry.ImageFile.Length > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(categry.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                        {
                            categry.ImageFile.CopyTo(fileStream);
                        }
                        categry.ImageUrl = @"\images\categories\" + fileName;
                    }
                    else
                    {
                        // This shouldn't happen if validation passed, but just in case
                        ModelState.AddModelError("ImageFile", "Image is required.");
                        return View(categry);
                    }

                    // Set audit fields
                    AuditHelper.SetCreatedAudit(categry, User);
                    _unitOfWork.categry.add(categry);
                    _unitOfWork.save();
                    TempData["success"] = "Category Created Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception for debugging
                    System.Diagnostics.Debug.WriteLine($"Error creating category: {ex.Message}");
                    ModelState.AddModelError("", $"An error occurred while creating the category: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            
            // Return view with errors
            return View(categry);
        }

        // GET: Categries/Edit/5
        public IActionResult Edit(int? id)
        {


            var categry = _unitOfWork.categry.Get(m => m.Id == id);
            if (categry == null)
            {
                return NotFound();
            }
            return View(categry);
        }

        // POST: Categries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Categry categry)
        {
            if (id != categry.Id)
            {
                return NotFound();
            }

            // Get existing category to preserve image if not changed
            var existingCategory = _unitOfWork.categry.Get(m => m.Id == id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Preserve existing ImageUrl if no new image is uploaded
            if (categry.ImageFile == null || categry.ImageFile.Length == 0)
            {
                categry.ImageUrl = existingCategory.ImageUrl;
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
                    if (categry.ImageFile != null && categry.ImageFile.Length > 0)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string categoryPath = Path.Combine(wwwRootPath, @"images\categories");

                        if (!Directory.Exists(categoryPath))
                        {
                            Directory.CreateDirectory(categoryPath);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingCategory.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, existingCategory.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Upload new image
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(categry.ImageFile.FileName);
                        using (var fileStream = new FileStream(Path.Combine(categoryPath, fileName), FileMode.Create))
                        {
                            categry.ImageFile.CopyTo(fileStream);
                        }
                        categry.ImageUrl = @"\images\categories\" + fileName;
                    }
                    // If no new image, ImageUrl already set from existingCategory above

                    // Set audit fields
                    AuditHelper.SetModifiedAudit(categry, User);
                    _unitOfWork.categry.update(categry);
                    _unitOfWork.save();
                    TempData["success"] = "Category Modified Successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategryExists(categry.Id))
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
                    System.Diagnostics.Debug.WriteLine($"Error updating category: {ex.Message}");
                    ModelState.AddModelError("", $"An error occurred while updating the category: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"Inner exception: {ex.InnerException.Message}");
                    }
                }
            }
            
            return View(categry);
        }

        // GET: Categries/Delete/5
        public IActionResult Delete(int? id)
        {
            Categry? categry = _unitOfWork.categry.Get(m => m.Id == id);
            if (categry == null)
            {
                return NotFound();
            }



            return View(categry);
        }

        // POST: Categries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Categry? categry = _unitOfWork.categry.Get(m => m.Id == id);

            if (categry != null)
            {
                // Soft delete - handled by repository, but set audit fields
                AuditHelper.SetDeletedAudit(categry, User);
                _unitOfWork.categry.remove(categry);
                _unitOfWork.save();
                TempData["success"] = "Categray Deleted Successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategryExists(int id)
        {
            return (_unitOfWork.categry.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
