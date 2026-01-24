using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ShopController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ApplicationDBContext _dbContext;

        public ShopController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
        }

        // GET: Shop - Products listing with category filtering
        public async Task<IActionResult> Index(int? categoryId = null, string sortBy = null, string availability = null)
        {
            // Get all active categories
            var categories = await _dbContext.Categries
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Id)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Get cart product IDs for authenticated users
            var cartProductIds = new HashSet<int>();
            var wishlistProductIds = new HashSet<int>();
            
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    cartProductIds = (await _dbContext.ShoppingCarts
                        .Where(c => c.ApplicationUserId == userId)
                        .Select(c => c.ProductId)
                        .ToListAsync())
                        .ToHashSet();
                        
                    wishlistProductIds = (await _dbContext.Wishlists
                        .Where(w => w.ApplicationUserId == userId)
                        .Select(w => w.ProductId)
                        .ToListAsync())
                        .ToHashSet();
                }
            }

            // Get products query
            var query = _dbContext.Products
                .Include(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(p => p.categry)
                .Include(p => p.ProductVariants)
                .Where(p => !p.IsDeleted);

            // Apply category filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }

            // Apply availability filter
            if (!string.IsNullOrEmpty(availability))
            {
                switch (availability.ToLowerInvariant())
                {
                    case "instock":
                        query = query.Where(p => p.StockQuantity > 0);
                        break;
                    case "outofstock":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                }
            }

            // Apply sorting
            query = sortBy?.ToLowerInvariant() switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.Id),
                "name" => query.OrderBy(p => p.Title),
                "discount" => query.OrderByDescending(p => p.ListPrice > 0 ? ((p.ListPrice - p.Price) / p.ListPrice * 100) : 0),
                _ => query.OrderByDescending(p => p.IsTrending).ThenByDescending(p => p.IsNew).ThenBy(p => p.Title)
            };

            var products = await query.Take(50).ToListAsync();

            // Get active flash sales for price calculation
            var now = DateTimeHelper.Now;
            var activeFlashSales = await _dbContext.FlashSales
                .Include(fs => fs.FlashSaleItems)
                .Where(fs => fs.IsActive && fs.StartDate <= now && fs.EndDate >= now && !fs.IsDeleted)
                .ToListAsync();

            // Calculate min prices for variable products
            var minPrices = new Dictionary<int, double>();
            var showPriceFlags = new Dictionary<int, bool>();
            var allVariantsOutOfStockFlags = new Dictionary<int, bool>();

            foreach (var product in products.Where(p => p.ProductType == ProductType.Variable))
            {
                var variants = product.ProductVariants?.Where(v => !v.IsDeleted).ToList() ?? new List<ProductVariant>();
                if (variants.Any())
                {
                    var inStockVariants = variants.Where(v => v.StockQuantity > 0).ToList();
                    if (inStockVariants.Any())
                    {
                        minPrices[product.Id] = (double)inStockVariants.Min(v => v.Price);
                        showPriceFlags[product.Id] = true;
                        allVariantsOutOfStockFlags[product.Id] = false;
                    }
                    else
                    {
                        minPrices[product.Id] = (double)variants.Min(v => v.Price);
                        showPriceFlags[product.Id] = false;
                        allVariantsOutOfStockFlags[product.Id] = true;
                    }
                }
            }

            // Get selected category
            var selectedCategory = categoryId.HasValue 
                ? categories.FirstOrDefault(c => c.Id == categoryId.Value) 
                : null;

            ViewBag.Categories = categories;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedCategory = selectedCategory;
            ViewBag.SortBy = sortBy;
            ViewBag.Availability = availability;
            ViewBag.CartProductIds = cartProductIds;
            ViewBag.WishlistProductIds = wishlistProductIds;
            ViewBag.MinPrices = minPrices;
            ViewBag.ShowPriceFlags = showPriceFlags;
            ViewBag.AllVariantsOutOfStockFlags = allVariantsOutOfStockFlags;
            ViewBag.ActiveFlashSales = activeFlashSales;
            ViewBag.TotalProducts = products.Count;

            return View(products);
        }

        // AJAX endpoint for loading more products
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            int page = 1,
            int pageSize = 20,
            int? categoryId = null,
            string sortBy = null,
            string availability = null)
        {
            var skip = (page - 1) * pageSize;

            // Get products query
            var query = _dbContext.Products
                .Include(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(p => p.categry)
                .Include(p => p.ProductVariants)
                .Where(p => !p.IsDeleted);

            // Apply category filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }

            // Apply availability filter
            if (!string.IsNullOrEmpty(availability))
            {
                switch (availability.ToLowerInvariant())
                {
                    case "instock":
                        query = query.Where(p => p.StockQuantity > 0);
                        break;
                    case "outofstock":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                }
            }

            // Apply sorting
            query = sortBy?.ToLowerInvariant() switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.Id),
                "name" => query.OrderBy(p => p.Title),
                "discount" => query.OrderByDescending(p => p.ListPrice > 0 ? ((p.ListPrice - p.Price) / p.ListPrice * 100) : 0),
                _ => query.OrderByDescending(p => p.IsTrending).ThenByDescending(p => p.IsNew).ThenBy(p => p.Title)
            };

            var totalCount = await query.CountAsync();
            var products = await query.Skip(skip).Take(pageSize).ToListAsync();

            // Get cart product IDs for authenticated users
            var cartProductIds = new HashSet<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    cartProductIds = (await _dbContext.ShoppingCarts
                        .Where(c => c.ApplicationUserId == userId)
                        .Select(c => c.ProductId)
                        .ToListAsync())
                        .ToHashSet();
                }
            }

            // Get active flash sales
            var now = DateTimeHelper.Now;
            var activeFlashSaleItems = await _dbContext.FlashSaleItems
                .Include(fsi => fsi.FlashSale)
                .Where(fsi => !fsi.IsDeleted 
                    && fsi.FlashSaleQuantity > 0
                    && fsi.FlashSale != null
                    && fsi.FlashSale.IsActive
                    && fsi.FlashSale.StartDate <= now
                    && fsi.FlashSale.EndDate >= now)
                .ToListAsync();

            var productsJson = products.Select(p => {
                var images = p.ProductImages?.OrderBy(pi => pi.DisplayOrder).Select(pi => pi.ImageUrl).ToList() ?? new List<string>();
                if (!images.Any() && !string.IsNullOrEmpty(p.ImageUrl))
                {
                    images.Add(p.ImageUrl);
                }

                var displayPrice = p.Price;
                var displayListPrice = p.ListPrice;

                // Check for flash sale price
                var flashSaleItem = activeFlashSaleItems.FirstOrDefault(fsi => fsi.ProductId == p.Id && fsi.ProductVariantId == null);
                if (flashSaleItem != null && (double)flashSaleItem.FlashSalePrice < displayPrice)
                {
                    displayListPrice = displayPrice > 0 ? displayPrice : displayListPrice;
                    displayPrice = (double)flashSaleItem.FlashSalePrice;
                }

                // Calculate discount percentage
                var discountPercent = displayListPrice > 0 && displayListPrice > displayPrice
                    ? (int)((displayListPrice - displayPrice) / displayListPrice * 100)
                    : 0;

                return new
                {
                    id = p.Id,
                    title = p.Title,
                    titleAr = p.TitleAr,
                    slug = p.GetSlug(),
                    price = displayPrice,
                    listPrice = displayListPrice,
                    discountPercent = discountPercent,
                    stockQuantity = p.StockQuantity,
                    imageUrl = images.FirstOrDefault() ?? "/images/no-image.png",
                    productImages = images,
                    productType = p.ProductType.ToString(),
                    hasVariants = p.ProductType == ProductType.Variable,
                    isNew = p.IsNew,
                    isTrending = p.IsTrending,
                    isInCart = cartProductIds.Contains(p.Id),
                    categoryName = p.categry?.Name ?? "",
                    categoryNameAr = p.categry?.NameAr ?? ""
                };
            }).ToList();

            return Json(new
            {
                products = productsJson,
                hasMore = skip + pageSize < totalCount,
                totalCount = totalCount,
                currentPage = page
            });
        }
    }
}
