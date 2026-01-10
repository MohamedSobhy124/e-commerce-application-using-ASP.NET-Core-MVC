using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class FlashSaleController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public FlashSaleController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: All Flash Sales Page
        public IActionResult Index()
        {
            var activeFlashSales = _unitOfWork.FlashSale.GetActiveFlashSales();
            return View(activeFlashSales);
        }

        // GET: Single Flash Sale Details
        public IActionResult Details(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                TempData["error"] = "Flash sale not found";
                return RedirectToAction(nameof(Index));
            }

            // Check if flash sale is active
            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            if (!flashSale.IsActive || now < flashSale.StartDate || now > flashSale.EndDate)
            {
                TempData["error"] = "This flash sale is not currently active";
                return RedirectToAction(nameof(Index));
            }

            return View(flashSale);
        }

        // GET: Get Flash Sale Item Info (AJAX)
        [HttpGet]
        public IActionResult GetFlashSaleItemInfo(int itemId)
        {
            var item = _unitOfWork.FlashSaleItem.Get(
                i => i.Id == itemId,
                includeProperties: "Product,Product.ProductImages,FlashSale");

            if (item == null)
            {
                return Json(new { success = false, message = "Flash sale item not found" });
            }

            // Check if flash sale is active
            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            if (!item.FlashSale.IsActive || now < item.FlashSale.StartDate || now > item.FlashSale.EndDate)
            {
                return Json(new { success = false, message = "This flash sale is no longer active" });
            }

            // Check if item has stock
            if (item.FlashSaleQuantity <= 0)
            {
                return Json(new { success = false, message = "This item is sold out" });
            }

            return Json(new
            {
                success = true,
                productId = item.ProductId,
                flashSaleItemId = item.Id,
                title = item.Product?.Title,
                flashSalePrice = item.FlashSalePrice,
                normalPrice = item.Product?.Price,
                availableQuantity = item.FlashSaleQuantity,
                discount = item.DiscountPercentage,
                imageUrl = item.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
            });
        }

        // GET: Get Flash Sale Products HTML (AJAX for modal)
        [HttpGet]
        public IActionResult GetFlashSaleProducts(int id)
        {
            var flashSale = _unitOfWork.FlashSale.GetFlashSaleWithItems(id);
            
            if (flashSale == null)
            {
                return BadRequest("Flash sale not found");
            }

            // Check if flash sale is active
            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            if (!flashSale.IsActive || now < flashSale.StartDate || now > flashSale.EndDate)
            {
                return BadRequest("This flash sale is not currently active");
            }

            // Store flash sale name and end date in ViewBag for JavaScript
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            var flashSaleName = currentCulture == "ar" && !string.IsNullOrEmpty(flashSale.NameAr) 
                ? flashSale.NameAr 
                : flashSale.Name;
            
            ViewBag.FlashSaleName = flashSaleName;
            ViewBag.FlashSaleEndDate = flashSale.EndDate.ToString("o");
            
            // Return partial view with products
            return PartialView("_FlashSaleProducts", flashSale);
        }
    }
}




