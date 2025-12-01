using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PromoCodeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ApplicationDBContext _dbContext;

        public PromoCodeController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
        }

        // GET: PromoCode/Index
        public IActionResult Index()
        {
            var promoCodes = _unitOfWork.PromoCode.GetAll().OrderByDescending(p => p.CreatedDate);
            return View(promoCodes);
        }

        // GET: PromoCode/Create
        public IActionResult Create()
        {
            var promoCode = new PromoCode
            {
                StartDate = BulkyBook.Utility.DateTimeHelper.Now,
                EndDate = BulkyBook.Utility.DateTimeHelper.Now.AddDays(30),
                IsActive = true
            };
            return View(promoCode);
        }

        // POST: PromoCode/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PromoCode promoCode)
        {
            // Custom validation
            if (promoCode.EndDate <= promoCode.StartDate)
            {
                ModelState.AddModelError("EndDate", _localizer["EndDateMustBeAfterStartDate"]);
            }

            // Check if code already exists
            if (!_unitOfWork.PromoCode.IsCodeAvailable(promoCode.Code))
            {
                ModelState.AddModelError("Code", _localizer["ThisPromoCodeAlreadyExists"]);
            }

            // Validate discount value based on type
            if (promoCode.DiscountType == DiscountType.Percentage && promoCode.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", _localizer["PercentageDiscountCannotExceed100"]);
            }

            if (ModelState.IsValid)
            {
                // Get current user ID
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                promoCode.CreatedBy = userId;
                promoCode.CreatedDate = BulkyBook.Utility.DateTimeHelper.Now;
                promoCode.TimesUsed = 0;

                _unitOfWork.PromoCode.add(promoCode);
                _unitOfWork.save();

                TempData["success"] = _localizer["PromoCodeCreatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(promoCode);
        }

        // GET: PromoCode/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == id, includeProperties: "ExcludedProducts,ExcludedProducts.Product");
            
            if (promoCode == null)
            {
                return NotFound();
            }

            // Load all products for selection
            var allProducts = _unitOfWork.product.GetAll(p => !p.IsDeleted, includeProperties: "categry")
                .OrderBy(p => p.Title)
                .ToList();
            ViewBag.AllProducts = allProducts;

            return View(promoCode);
        }

        // POST: PromoCode/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(PromoCode promoCode)
        {
            // Custom validation
            if (promoCode.EndDate <= promoCode.StartDate)
            {
                ModelState.AddModelError("EndDate", _localizer["EndDateMustBeAfterStartDate"]);
            }

            // Check if code already exists (excluding current promo code)
            if (!_unitOfWork.PromoCode.IsCodeAvailable(promoCode.Code, promoCode.Id))
            {
                ModelState.AddModelError("Code", _localizer["ThisPromoCodeAlreadyExists"]);
            }

            // Validate discount value based on type
            if (promoCode.DiscountType == DiscountType.Percentage && promoCode.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", _localizer["PercentageDiscountCannotExceed100"]);
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.PromoCode.Update(promoCode);
                _unitOfWork.save();

                TempData["success"] = _localizer["PromoCodeUpdatedSuccessfully"].Value;
                return RedirectToAction(nameof(Index));
            }

            return View(promoCode);
        }

        // GET: PromoCode/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == id, includeProperties: "ExcludedProducts,ExcludedProducts.Product");
            
            if (promoCode == null)
            {
                return NotFound();
            }

            // Get usage statistics
            var usages = _unitOfWork.PromoCodeUsage.GetAll(u => u.PromoCodeId == id, includeProperties: "ApplicationUser,OrderHeader");
            ViewBag.Usages = usages;

            return View(promoCode);
        }

        // GET: PromoCode/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == id);
            
            if (promoCode == null)
            {
                return NotFound();
            }

            return View(promoCode);
        }

        // POST: PromoCode/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == id);
            
            if (promoCode == null)
            {
                return NotFound();
            }

            // Check if promo code has been used
            if (promoCode.TimesUsed > 0)
            {
                TempData["error"] = _localizer["CannotDeleteUsedPromoCode"].Value;
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.PromoCode.remove(promoCode);
            _unitOfWork.save();

            TempData["success"] = _localizer["PromoCodeDeletedSuccessfully"].Value;
            return RedirectToAction(nameof(Index));
        }

        // POST: PromoCode/ToggleActive
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            try
            {
                // Get current value first
                var promoCode = _dbContext.PromoCodes.AsNoTracking().FirstOrDefault(p => p.Id == id);
                
                if (promoCode == null)
                {
                    return Json(new { success = false, message = _localizer["PromoCodeNotFound"].Value });
                }

                // Calculate new value
                var newIsActive = !promoCode.IsActive;
                
                // Get the actual table name from EF Core metadata
                var entityType = _dbContext.Model.FindEntityType(typeof(PromoCode));
                var tableName = entityType?.GetTableName() ?? "PromoCodes";
                
                // Use raw SQL to update directly - bypasses EF Core tracking issues
                var sql = $"UPDATE [{tableName}] SET [IsActive] = {{0}} WHERE [Id] = {{1}}";
                var rowsAffected = _dbContext.Database.ExecuteSqlRaw(sql, newIsActive, id);

                if (rowsAffected == 0)
                {
                    return Json(new { success = false, message = "Failed to update promo code" });
                }

                var message = newIsActive ? _localizer["PromoCodeActivated"].Value : _localizer["PromoCodeDeactivated"].Value;
                
                return Json(new 
                { 
                    success = true, 
                    message = message,
                    isActive = newIsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: PromoCode/GetActivePromoCodes (API endpoint)
        [HttpGet]
        public IActionResult GetActivePromoCodes()
        {
            var promoCodes = _unitOfWork.PromoCode.GetActivePromoCodes();
            return Json(promoCodes);
        }

        // GET: PromoCode/GetExcludedProducts/{id}
        [HttpGet]
        public IActionResult GetExcludedProducts(int id)
        {
            var promoCode = _dbContext.PromoCodes?
                .Include(p => p.ExcludedProducts)
                    .ThenInclude(ep => ep.Product)
                .FirstOrDefault(p => p.Id == id);

            if (promoCode == null)
            {
                return Json(new { success = false, message = "Promo code not found" });
            }

            var excludedProducts = promoCode.ExcludedProducts?
                .Select(ep => new
                {
                    id = ep.Id,
                    productId = ep.ProductId,
                    productTitle = ep.Product?.Title ?? "Unknown",
                    productTitleAr = ep.Product?.TitleAr ?? ""
                })
                .ToList()  ;

            return Json(new { success = true, products = excludedProducts });
        }

        // POST: PromoCode/AddExcludedProduct
        [HttpPost]
        public IActionResult AddExcludedProduct(int promoCodeId, int productId)
        {
            try
            {
                // Check if promo code exists
                var promoCode = _dbContext.PromoCodes.FirstOrDefault(p => p.Id == promoCodeId);
                if (promoCode == null)
                {
                    return Json(new { success = false, message = "Promo code not found" });
                }

                // Check if product exists
                var product = _unitOfWork.product.Get(p => p.Id == productId && !p.IsDeleted);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if already excluded
                var existing = _dbContext.PromoCodeExcludedProducts
                    .FirstOrDefault(ep => ep.PromoCodeId == promoCodeId && ep.ProductId == productId);
                
                if (existing != null)
                {
                    return Json(new { success = false, message = "Product is already excluded" });
                }

                // Add excluded product
                var excludedProduct = new PromoCodeExcludedProduct
                {
                    PromoCodeId = promoCodeId,
                    ProductId = productId
                };

                _dbContext.PromoCodeExcludedProducts.Add(excludedProduct);
                _dbContext.SaveChanges();

                return Json(new 
                { 
                    success = true, 
                    message = "Product excluded successfully",
                    excludedProductId = excludedProduct.Id,
                    productTitle = product.Title,
                    productTitleAr = product.TitleAr ?? ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // POST: PromoCode/RemoveExcludedProduct
        [HttpPost]
        public IActionResult RemoveExcludedProduct(int excludedProductId)
        {
            try
            {
                var excludedProduct = _dbContext.PromoCodeExcludedProducts
                    .FirstOrDefault(ep => ep.Id == excludedProductId);

                if (excludedProduct == null)
                {
                    return Json(new { success = false, message = "Excluded product not found" });
                }

                _dbContext.PromoCodeExcludedProducts.Remove(excludedProduct);
                _dbContext.SaveChanges();

                return Json(new { success = true, message = "Product removed from excluded list" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        // GET: PromoCode/SearchProducts
        [HttpGet]
        public IActionResult SearchProducts(string searchTerm)
        {
            var products = _unitOfWork.product.GetAll(
                p => !p.IsDeleted && 
                (p.Title.Contains(searchTerm) || 
                 (p.TitleAr != null && p.TitleAr.Contains(searchTerm))),
                includeProperties: "categry")
                .OrderBy(p => p.Title)
                .Take(20)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    titleAr = p.TitleAr ?? "",
                    price = p.Price,
                    category = p.categry?.Name ?? "N/A"
                })
                .ToList();

            return Json(products);
        }
    }
}

