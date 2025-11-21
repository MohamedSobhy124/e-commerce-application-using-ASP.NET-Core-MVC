using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class FlashSaleController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FlashSaleController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1)
            };
            return View(flashSale);
        }

        // POST: Create Flash Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(FlashSale flashSale)
        {
            // Validate dates
            if (flashSale.EndDate <= flashSale.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.FlashSale.Add(flashSale);
                _unitOfWork.save();
                TempData["success"] = "Flash sale created successfully! Now add products to it.";
                return RedirectToAction(nameof(AddProducts), new { id = flashSale.Id });
            }

            return View(flashSale);
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

            var products = allProducts.Select(p => new SelectListItem
            {
                Text = conflictingProductIds.Contains(p.Id) 
                    ? $"{p.Title} (Stock: {p.StockQuantity}) ⚠️ IN ANOTHER FLASH SALE"
                    : $"{p.Title} (Stock: {p.StockQuantity})",
                Value = p.Id.ToString(),
                Disabled = conflictingProductIds.Contains(p.Id) // Disable conflicting products
            }).ToList();

            ViewBag.Products = products;
            ViewBag.FlashSale = flashSale;

            return View();
        }

        // POST: Add Product to Flash Sale
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProductToSale(int flashSaleId, int productId, int quantity, decimal price)
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

            // Validation
            if (quantity > product.StockQuantity)
            {
                return Json(new { success = false, message = $"Quantity cannot exceed stock quantity ({product.StockQuantity})" });
            }
             
            if ((double)price >= product.Price)
            {
                return Json(new { success = false, message = $"Price cannot exceed or equal original ({product.Price})" });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Quantity must be greater than 0" });
            }

            if (price <= 0)
            {
                return Json(new { success = false, message = "Price must be greater than 0" });
            }

            // Check if product already in this flash sale
            var existingItem = _unitOfWork.FlashSaleItem.Get(
                i => i.FlashSaleId == flashSaleId && i.ProductId == productId);

            if (existingItem != null)
            {
                return Json(new { success = false, message = "Product is already in this flash sale" });
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
                FlashSaleQuantity = quantity,
                FlashSaleQuantityCreated = quantity,
                FlashSalePrice = price,
                AddedDate = DateTime.Now
            };

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
        public IActionResult Edit(FlashSale flashSale)
        {
            // Validate dates
            if (flashSale.EndDate <= flashSale.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.FlashSale.Update(flashSale);
                _unitOfWork.save();
                TempData["success"] = "Flash sale updated successfully";
                return RedirectToAction(nameof(Index));
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

            // Remove all items first
            if (flashSale.FlashSaleItems != null && flashSale.FlashSaleItems.Any())
            {
                foreach (var item in flashSale.FlashSaleItems.ToList())
                {
                    _unitOfWork.FlashSaleItem.Remove(item);
                }
            }

            // Remove flash sale
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
            var product = _unitOfWork.product.Get(p => p.Id == productId);
            
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            return Json(new { 
                success = true, 
                stockQuantity = product.StockQuantity,
                price = product.Price,
                title = product.Title
            });
        }
    }
}

