using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OfferController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;
        private readonly IdealWeightNutrition.DataAccess.Data.ApplicationDBContext _dbContext;

        public OfferController(IUnitOfWork unitOfWork, IStringLocalizer<IdealWeightNutrition.SharedResources> localizer, IdealWeightNutrition.DataAccess.Data.ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
        }

        // GET: All Offers (Flash Sales + Combo Offers + Discounted Products)
        public async Task<IActionResult> Index()
        {
            var activeFlashSales = _unitOfWork.FlashSale.GetActiveFlashSales();
            var activeComboOffers = _unitOfWork.ComboOffer.GetActiveComboOffers();

            // Use the same method as home screen to get discounted products
            var discountedProducts = await GetDiscountedProductsAsync();

            // Get cart product IDs for authenticated users
            var cartProductIds = new List<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    cartProductIds = await _dbContext.ShoppingCarts
                        .AsNoTracking()
                        .Where(u => u.ApplicationUserId == userId)
                        .Select(c => c.ProductId)
                        .ToListAsync();
                }
            }

            ViewBag.FlashSales = activeFlashSales;
            ViewBag.ComboOffers = activeComboOffers;
            ViewBag.DiscountedProducts = discountedProducts;
            ViewBag.HasFlashSales = activeFlashSales != null && activeFlashSales.Any();
            ViewBag.HasComboOffers = activeComboOffers != null && activeComboOffers.Any();
            ViewBag.HasDiscountedProducts = discountedProducts != null && discountedProducts.Any();
            ViewBag.CartProductIds = cartProductIds;

            return View();
        }

        // Same method as HomeController to get discounted products
        private async Task<List<Product>> GetDiscountedProductsAsync()
        {
            // First get products with main product discounts (limit early)
            var mainProductDiscounts = await _dbContext.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.StockQuantity > 0 && p.ListPrice > p.Price && p.ListPrice > 0)
                .Include(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(p => p.ProductVariants.Where(v => !v.IsDeleted))
                .OrderByDescending(p => ((p.ListPrice - p.Price) / p.ListPrice) * 100)
                .Take(20)
                .Select(p => new { 
                    Product = p, 
                    Discount = (double)((p.ListPrice - p.Price) / p.ListPrice) * 100 
                })
                .ToListAsync();
            
            var discountedProductsList = mainProductDiscounts
                .Select(item => (item.Product, item.Discount))
                .ToList();
            
            // Get products with variant discounts (optimized query with better filtering)
            if (discountedProductsList.Count < 20)
            {
                var variantDiscountProducts = await _dbContext.Products
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && p.StockQuantity > 0 && p.ProductType == IdealWeightNutrition.Models.ProductType.Variable)
                    .Include(p => p.ProductImages.Where(img => img.ImageInfo == null))
                    .Include(p => p.ProductVariants.Where(v => !v.IsDeleted && v.StockQuantity > 0 && v.ListPrice.HasValue && v.ListPrice > v.Price))
                    .Take(50) // Limit early to reduce memory usage
                    .ToListAsync();
                
                var variantProductsWithDiscounts = variantDiscountProducts
                    .Where(p => p.ProductVariants != null && p.ProductVariants.Any(v => !v.IsDeleted && v.StockQuantity > 0 && v.ListPrice.HasValue && v.ListPrice > v.Price))
                    .Select(p => new {
                        Product = p,
                        Discount = (double)(p.ProductVariants
                            .Where(v => !v.IsDeleted && v.StockQuantity > 0 && v.ListPrice.HasValue && v.ListPrice > v.Price)
                            .Select(v => ((v.ListPrice.Value - v.Price) / v.ListPrice.Value) * 100)
                            .DefaultIfEmpty(0)
                            .Max())
                    })
                    .Where(x => x.Discount > 0)
                    .OrderByDescending(x => x.Discount)
                    .Take(20 - discountedProductsList.Count)
                    .ToList();
                
                foreach (var item in variantProductsWithDiscounts)
                {
                    discountedProductsList.Add((item.Product, item.Discount));
                }
            }
            
            // Get top 20 by discount (already sorted)
            return discountedProductsList
                .OrderByDescending(x => x.Discount)
                .Take(20)
                .Select(x => x.Product)
                .ToList();
        }
    }
}

