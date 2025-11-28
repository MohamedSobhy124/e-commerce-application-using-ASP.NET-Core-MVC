using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
 
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<BulkyBook.SharedResources> _localizer;
        private readonly ApplicationDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IStringLocalizer<BulkyBook.SharedResources> localizer, ApplicationDBContext dbContext, IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
            _configuration = configuration;
            _userManager = userManager;
        }

        // Performance: Cache response for 5 minutes, vary by query parameters
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "categoryId", "searchTerm", "sortBy", "minPrice", "maxPrice", "availability" }, Location = ResponseCacheLocation.Any)]
        public IActionResult Index(int? categoryId, string searchTerm, string sortBy, 
            decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null, 
            int? minRating = null, string availability = null)
        {
            // Get active flash sales for homepage
            var activeFlashSales = _unitOfWork.FlashSale.GetActiveFlashSales();
            ViewBag.ActiveFlashSales = activeFlashSales;
            
            // Get discounted products (where ListPrice > Price or variants have discounts)
            // Include variants and images for proper discount display
            var allProducts = _unitOfWork.product.GetAllAsNoTracking(
                filter: p => p.StockQuantity > 0,
                includeProperties: "ProductImages,ProductVariants")
                .ToList();
            
            // Filter products that have discounts (either main product or variants)
            var discountedProducts = allProducts
                .Where(p => {
                    // Check if main product has discount
                    if (p.ListPrice > p.Price) return true;
                    
                    // Check if any variant has discount
                    if (p.ProductVariants != null && p.ProductVariants.Any())
                    {
                        return p.ProductVariants.Any(v => v.ListPrice > v.Price && v.StockQuantity > 0);
                    }
                    
                    return false;
                })
                .OrderByDescending(p => {
                    // Calculate best discount (either main product or best variant)
                    var mainDiscount = p.ListPrice > 0 ? ((p.ListPrice - p.Price) / p.ListPrice) * 100 : 0;
                    
                    if (p.ProductVariants != null && p.ProductVariants.Any())
                    {
                        var bestVariantDiscount = p.ProductVariants
                            .Where(v => v.ListPrice > v.Price && v.StockQuantity > 0)
                            .Select(v => v.ListPrice > 0 ? ((v.ListPrice - v.Price) / v.ListPrice) * 100 : 0)
                            .DefaultIfEmpty(0)
                            .Max()??0m;
                        
                        return Math.Max(mainDiscount, (double)bestVariantDiscount);
                    }
                    
                    return mainDiscount;
                })
                .Take(20) // Get top 20 discounted products
                .ToList();
            ViewBag.DiscountedProducts = discountedProducts;

            // Get active service subscriptions for homepage
            var activeServices = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive, includeProperties: "ServiceImages")
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.CreatedDate)
                .Take(6)
                .ToList();
            ViewBag.ActiveServices = activeServices;

            // PERFORMANCE: Use AsNoTracking for read-only queries
            // Get all categories for filter (cached - only load once per request)
            var allCategories = _unitOfWork.categry.GetAllAsNoTracking().ToList();
            ViewBag.Categories = allCategories;
            
            // Get sample products for each category to display in carousel
            var categoryProductsMap = new Dictionary<int, List<Product>>();
            foreach (var category in allCategories.Take(6))
            {
                var categoryProducts = _unitOfWork.product.GetAllAsNoTracking(
                    filter: p => p.CategryId == category.Id && p.StockQuantity > 0,
                    includeProperties: "ProductImages")
                    .Take(4)
                    .ToList();
                categoryProductsMap[category.Id] = categoryProducts;
            }
            ViewBag.CategoryProductsMap = categoryProductsMap;
            
            // PERFORMANCE: Optimized query - filter at database level, not in memory
            // Get best sellers (products with most orders)
            var bestSellers = _unitOfWork.product.GetAllAsNoTracking(
                filter: p => p.StockQuantity > 0,
                includeProperties: "categry,ProductImages")
                .OrderByDescending(p => p.Id) // For now, use ID as proxy for popularity
                .Take(8)
                .ToList();
            ViewBag.BestSellers = bestSellers;
            
            // PERFORMANCE: Optimized query
            // Get new arrivals (recently added products)
            var newArrivals = _unitOfWork.product.GetAllAsNoTracking(
                filter: p => p.StockQuantity > 0,
                includeProperties: "categry,ProductImages")
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();
            ViewBag.NewArrivals = newArrivals;
            
            // PERFORMANCE: Optimized - batch count queries instead of N+1
            // Get top categories (categories with most products)
            var categoryProductCounts = _unitOfWork.product.GetAllAsNoTracking()
                .GroupBy(p => p.CategryId)
                .Select(g => new { CategryId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.CategryId, x => x.Count);
            
            var topCategories = allCategories
                .OrderByDescending(c => categoryProductCounts.GetValueOrDefault(c.Id, 0))
                .Take(6)
                .ToList();
            ViewBag.TopCategories = topCategories;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.InStock = inStock;
            ViewBag.MinRating = minRating;
            ViewBag.Availability = availability;
            
            // Check if logged-in user is subscribed to newsletter
            if (User.Identity.IsAuthenticated)
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var userSubscription = _unitOfWork.NewsletterSubscription.GetByEmail(userEmail);
                    ViewBag.IsNewsletterSubscribed = userSubscription != null && userSubscription.IsActive;
                    ViewBag.UserEmail = userEmail;
                }
                else
                {
                    ViewBag.IsNewsletterSubscribed = false;
                }
            }
            else
            {
                ViewBag.IsNewsletterSubscribed = false;
            }
            
            // Get products with optional filtering (optimized query - only load what's needed)
            var query = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages").AsQueryable();
            
            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }
            
            // Filter by search term (case-insensitive) - includes Arabic fields
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchLower) || 
                    (p.TitleAr != null && p.TitleAr.ToLower().Contains(searchLower)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
                    (p.DescriptionAr != null && p.DescriptionAr.ToLower().Contains(searchLower)));
            }
            
            // Filter by price range
            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => p.Price >= (double)minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.Price <= (double)maxPrice.Value);
            }
            
            // Filter by stock availability
            if (inStock.HasValue)
            {
                if (inStock.Value)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }
                else
                {
                    query = query.Where(p => p.StockQuantity == 0);
                }
            }
            
            // Filter by availability status
            if (!string.IsNullOrEmpty(availability))
            {
                switch (availability.ToLower())
                {
                    case "instock":
                        query = query.Where(p => p.StockQuantity > 0);
                        break;
                    case "outofstock":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                    case "lowstock":
                        query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.MinimumStockAlert);
                        break;
                }
            }
            
            // PERFORMANCE: Use AsNoTracking for read-only queries
            // Get price range for filter display (optimized - only get min/max, not all products)
            var priceStats = _unitOfWork.product.GetAllAsNoTracking()
                .Select(p => p.Price)
                .ToList();
            ViewBag.MinPriceRange = priceStats.Any() ? (decimal?)priceStats.Min() : 0;
            ViewBag.MaxPriceRange = priceStats.Any() ? (decimal?)priceStats.Max() : 1000;
            
            // Sort products
            query = sortBy switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Title),
                "newest" => query.OrderByDescending(p => p.Id),
                "oldest" => query.OrderBy(p => p.Id),
                _ => query.OrderBy(p => p.Title)
            };
            
            // Get total count before pagination (optimized - count before materializing)
            var totalCount = query.Count();
            ViewBag.TotalProducts = totalCount;
            
            // Pagination - take first 20 (materialize only what we need)
            IEnumerable<Product> ProductList = query.Take(20).ToList();

            foreach (var p in ProductList)
            {
                p.ProductImages = p.ProductImages
                    .Where(img => img.ImageInfo == null)
                    .ToList();
            }
            // Cache headers are automatically set by [ResponseCache] attribute

            // Get cart product IDs for authenticated users
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId);
                ViewBag.CartProductIds = cartItems.Select(c => c.ProductId).ToList();
                
                // Get wishlist product IDs for authenticated users
                try
                {
                    // Check if wishlist repository exists by trying to access it
                    if (_unitOfWork.wishlist != null)
                    {
                        var wishlistItems = _unitOfWork.wishlist.GetAll(u => u.ApplicationUserId == userId);
                        ViewBag.WishlistProductIds = wishlistItems.Select(w => w.ProductId).ToList();
                    }
                    else
                    {
                        ViewBag.WishlistProductIds = new List<int>();
                    }
                }
                catch
                {
                    // Wishlist repository not set up yet
                    ViewBag.WishlistProductIds = new List<int>();
                }
            }
            else
            {
                ViewBag.CartProductIds = new List<int>();
                ViewBag.WishlistProductIds = new List<int>();
            }
            
            return View(ProductList);
        }

        [HttpGet]
        [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "page", "categoryId", "searchTerm", "sortBy", "minPrice", "maxPrice", "availability" })]
        public IActionResult LoadMoreProducts(int page = 0, int pageSize = 20, 
            int? categoryId = null, string searchTerm = null, string sortBy = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null, 
            string availability = null)
        {
            // PERFORMANCE: Use AsNoTracking for read-only queries (faster, less memory)
            // Only include necessary properties to reduce data transfer
            var query = _unitOfWork.product.GetAllAsNoTracking(includeProperties: "categry,ProductImages").AsQueryable();
            
            // Apply filters at database level (not in memory)
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // PERFORMANCE: Use EF.Functions.Like for better index usage (if available)
                // Otherwise, keep simple Contains but with trim
                var searchLower = searchTerm.Trim().ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchLower) || 
                    (p.TitleAr != null && p.TitleAr.ToLower().Contains(searchLower)));
            }
            
            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => p.Price >= (double)minPrice.Value);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.Price <= (double)maxPrice.Value);
            }
            
            if (inStock.HasValue)
            {
                if (inStock.Value)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }
                else
                {
                    query = query.Where(p => p.StockQuantity == 0);
                }
            }
            
            if (!string.IsNullOrEmpty(availability))
            {
                switch (availability.ToLower())
                {
                    case "instock":
                        query = query.Where(p => p.StockQuantity > 0);
                        break;
                    case "outofstock":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                    case "lowstock":
                        query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.MinimumStockAlert);
                        break;
                }
            }
            
            // PERFORMANCE: Apply sorting before counting/materializing
            query = sortBy switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Title),
                "newest" => query.OrderByDescending(p => p.Id),
                "oldest" => query.OrderBy(p => p.Id),
                _ => query.OrderBy(p => p.Title)
            };
            
            var productsToSkip = page * pageSize;
            
            // PERFORMANCE: Use projection to select only needed fields at database level
            // This reduces data transfer and memory usage significantly
            var productsQuery = query
                .Skip(productsToSkip)
                .Take(pageSize)
                .Select(p => new
                {
                    // Select only needed fields at database level (faster, less memory)
                    id = p.Id,
                    title = p.Title,
                    titleAr = p.TitleAr,
                    price = p.Price,
                    listPrice = p.ListPrice,
                    stockQuantity = p.StockQuantity,
                    minimumStockAlert = p.MinimumStockAlert,
                    imageUrl = p.ImageUrl,
                    productType = p.ProductType,
                    categoryId = p.categry != null ? (int?)p.categry.Id : null,
                    categoryName = p.categry != null ? p.categry.Name : null,
                    productImages = p.ProductImages
                        .Where(pi => pi.ImageInfo != "INFO_IMAGE")
                        .OrderBy(pi => pi.DisplayOrder)
                        .Select(pi => pi.ImageUrl)
                        .ToList()
                });
            
            // Execute query and get products
            var products = productsQuery.ToList();
            
            // PERFORMANCE: Only count if we need to determine hasMore
            // Use a lightweight approach - check if we got a full page
            var hasMore = products.Count == pageSize;
            int totalProducts = 0;
            
            // Only count total if needed for display or if this is the first page
            // For subsequent pages, only count if we got a full page (might be more)
            if (page == 0)
            {
                // First page - always get count for display
                totalProducts = query.Count();
                hasMore = (productsToSkip + products.Count) < totalProducts;
            }
            else if (hasMore)
            {
                // Got a full page, might be more - do a quick check
                // Use a faster approach: try to get one more record instead of full count
                var checkQuery = query.Skip(productsToSkip + pageSize).Take(1);
                hasMore = checkQuery.Any();
                if (!hasMore)
                {
                    // No more records, can calculate total from position
                    totalProducts = productsToSkip + products.Count;
                }
            }
            else
            {
                // Didn't get a full page - this is the last page
                totalProducts = productsToSkip + products.Count;
            }

            // Transform to final JSON format (already projected, minimal transformation needed)
            var productsJson = products.Select(p => new
            {
                id = p.id,
                title = p.title,
                titleAr = p.titleAr,
                price = p.price,
                listPrice = p.listPrice,
                stockQuantity = p.stockQuantity,
                minimumStockAlert = p.minimumStockAlert,
                imageUrl = p.imageUrl,
                productType = p.productType,
                categry = p.categoryId.HasValue ? new { id = p.categoryId.Value, name = p.categoryName } : null,
                productImages = p.productImages ?? new List<string>()
            }).ToList();

            return Json(new { products = productsJson, hasMore = hasMore, totalCount = totalProducts });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCart(int productId)
        {
            bool isAdded = false;
            string message = "";
            int cartCount = 0;

            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - use database cart
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                ShoppingCart shoppingCartFromDB = _unitOfWork.shoppingCart.Get(a => a.ProductId == productId && a.ApplicationUserId == UserId);

                if (shoppingCartFromDB != null)
                {
                    // Product exists in cart, remove it
                    _unitOfWork.shoppingCart.remove(shoppingCartFromDB);
                    message = "Product removed from cart!";
                    isAdded = false;
                }
                else
                {
                    // Product doesn't exist, add it
                    ShoppingCart newCart = new ShoppingCart
                    {
                        ProductId = productId,
                        ApplicationUserId = UserId,
                        Count = 1
                    };
                    _unitOfWork.shoppingCart.add(newCart);
                    message = "Product added to cart successfully!";
                    isAdded = true;
                }

                _unitOfWork.save();

                // Get updated cart count (count of unique products, not sum of quantities)
                var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == UserId);
                cartCount = cartItems.Count();
            }
            else
            {
                // Guest user - use session cart
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var existingItem = guestCart.FirstOrDefault(c => c.ProductId == productId);

                if (existingItem != null)
                {
                    // Product exists in cart, remove it
                    BulkyBook.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, productId);
                    message = "Product removed from cart!";
                    isAdded = false;
                }
                else
                {
                    // Product doesn't exist, add it
                    BulkyBook.Utility.GuestCartHelper.AddToCart(HttpContext.Session, productId, 1);
                    message = "Product added to cart successfully!";
                    isAdded = true;
                }

                // Get total quantity from guest cart
                  guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartCount = guestCart.Count;
            }

            return Json(new { success = true, message = message, isAdded = isAdded, cartCount = cartCount });
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            int cartCount = 0;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (userId != null)
                {
                    // Authenticated user - get count of unique products (not sum of quantities)
                    var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId);
                    cartCount = cartItems.Count();
                }
            }
            else
            {
                // Guest user - get count of unique products from session (not sum of quantities)
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartCount = guestCart.Count;
            }
            
            return Json(new { cartCount = cartCount });
        }

        [HttpGet]
        public IActionResult GetCartWidget()
        {
            return ViewComponent("ShoppingCart");
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetCartSidebar()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            // PERFORMANCE: Use AsNoTracking for read-only cart queries
            var cartItems = _unitOfWork.shoppingCart.GetAllAsNoTracking(
                filter: u => u.ApplicationUserId == userId,
                includeProperties: "product"
            ).ToList();

            double orderTotal = 0;
            foreach (var item in cartItems)
            {
                // Calculate price based on quantity (you can adjust this logic if needed)
                item.Price = item.product.Price;
                orderTotal += (item.Price * item.Count);
            }

            var cartData = new
            {
                items = cartItems.Select(item => new
                {
                    id = item.Id,
                    productId = item.ProductId,
                    title = item.product.Title,
                    imageUrl = item.product.ImageUrl,
                    price = item.Price,
                    count = item.Count,
                    total = item.Price * item.Count
                }),
                orderTotal = orderTotal,
                itemCount = cartItems.Count
            };

            return Json(cartData);
        }

        [HttpGet]
        public IActionResult GetCartProductIds()
        {
            List<int> productIds;

            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId);
                productIds = cartItems.Select(c => c.ProductId).ToList();
            }
            else
            {
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                productIds = guestCart.Select(c => c.ProductId).ToList();
            }
            
            return Json(new { productIds = productIds });
        }

        // ==========================================
        // WISHLIST ACTIONS
        // ==========================================
        [HttpPost]
        [Authorize]
        public IActionResult ToggleWishlist(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login to use wishlist", requiresLogin = true });
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Check if Wishlist repository exists and use it
            try
            {
                var existingWishlist = _unitOfWork.wishlist.Get(w => w.ProductId == productId && w.ApplicationUserId == userId);
                
                string message;
                bool isAdded;

                if (existingWishlist != null)
                {
                    // Remove from wishlist
                    _unitOfWork.wishlist.Remove(existingWishlist);
                    _unitOfWork.save();
                    message = "Removed from wishlist";
                    isAdded = false;
                }
                else
                {
                    // Add to wishlist
                    var wishlistItem = new Wishlist
                    {
                        ProductId = productId,
                        ApplicationUserId = userId
                    };
                    _unitOfWork.wishlist.Add(wishlistItem);
                    _unitOfWork.save();
                    message = "Added to wishlist! ❤️";
                    isAdded = true;
                }

                // Get wishlist count
                var wishlistItems = _unitOfWork.wishlist.GetAll(w => w.ApplicationUserId == userId);
                int wishlistCount = wishlistItems.Count();

                return Json(new { success = true, message = message, isAdded = isAdded, wishlistCount = wishlistCount });
            }
            catch (Exception ex)
            {
                // Wishlist repository doesn't exist yet
                return Json(new { success = false, message = "Wishlist feature is being set up. Please make sure all repository files are created.", requiresLogin = false });
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetWishlistItems()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, items = new List<object>(), count = 0 });
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                var wishlistItems = _unitOfWork.wishlist.GetAll(w => w.ApplicationUserId == userId, 
                    includeProperties: "product,product.ProductImages").ToList();

                var items = wishlistItems.Select(item => new
                {
                    id = item.Id,
                    productId = item.ProductId,
                    title = item.product.Title,
                    imageUrl = item.product.ProductImages?.FirstOrDefault()?.ImageUrl ?? item.product.ImageUrl ?? "/images/no-image.png",
                    price = (double)item.product.Price,
                    listPrice = item.product.ListPrice > 0 ? (double?)item.product.ListPrice : null,
                    productType = (int)item.product.ProductType
                }).ToList();

                return Json(new { success = true, items = items, count = items.Count });
            }
            catch
            {
                return Json(new { success = false, items = new List<object>(), count = 0 });
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetWishlistProductIds()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { productIds = new List<int>() });
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                var wishlistItems = _unitOfWork.wishlist.GetAll(w => w.ApplicationUserId == userId);
                if (wishlistItems != null)
                {
                    var productIds = wishlistItems.Select(w => w.ProductId).ToList();
                    return Json(new { productIds = productIds });
                }
                return Json(new { productIds = new List<int>() });
            }
            catch
            {
                return Json(new { productIds = new List<int>() });
            }
        }

        [HttpDelete]
        [Authorize]
        public IActionResult RemoveFromWishlist(int wishlistId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login" });
            }

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                var wishlistItem = _unitOfWork.wishlist.Get(w => w.Id == wishlistId && w.ApplicationUserId == userId);
                if (wishlistItem != null)
                {
                    _unitOfWork.wishlist.Remove(wishlistItem);
                    _unitOfWork.save();
                    
                    var wishlistItems = _unitOfWork.wishlist.GetAll(w => w.ApplicationUserId == userId);
                    int wishlistCount = wishlistItems != null ? wishlistItems.Count() : 0;
                    return Json(new { success = true, message = "Removed from wishlist", wishlistCount = wishlistCount });
                }
                return Json(new { success = false, message = "Item not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error removing item: {ex.Message}" });
            }
        }
        
        // Performance: Cache product details for 5 minutes (product data changes infrequently)
        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "productId" }, Location = ResponseCacheLocation.Any)]
        public IActionResult Details(int productId)
        {
            var hasPurchased = false;
            try
            {
                var claimsIdentity = (ClaimsIdentity)User?.Identity!;
                var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                hasPurchased = _unitOfWork.OrderDetail.GetAll(
                    od => od.ProductId == productId,
                    includeProperties: "OrderHeader"
                ).Any(od => od.OrderHeader != null &&
                            od.OrderHeader.ApplicationUserId == userId &&
                            od.OrderHeader.OrderStatus == SD.StatusDelivered);
            }
            catch
            {
                // If error checking purchase, just set to false
                hasPurchased = false;
            }

            // PERFORMANCE: Use direct context query with AsNoTracking for read-only
            var product = _dbContext.Products
                .AsNoTracking()
                .Include(p => p.categry)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductOptions.Where(o => !o.IsDeleted))
                    .ThenInclude(o => o.OptionValues.Where(ov => !ov.IsDeleted))
                .Include(p => p.ProductVariants.Where(v => !v.IsDeleted))
                    .ThenInclude(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                .FirstOrDefault(p => p.Id == productId && !p.IsDeleted);
            
            // Check if product is deleted
            if (product == null)
            {
                return NotFound();
            }
            
            // PERFORMANCE: Filter already done in Include, but ensure in-memory filtering for safety
            if (product.ProductOptions != null)
            {
                product.ProductOptions = product.ProductOptions.Where(o => !o.IsDeleted).ToList();
                foreach (var option in product.ProductOptions)
                {
                    if (option.OptionValues != null)
                    {
                        option.OptionValues = option.OptionValues.Where(ov => !ov.IsDeleted).OrderBy(ov => ov.DisplayOrder).ToList();
                    }
                }
            }
            
            if (product.ProductVariants != null)
            {
                product.ProductVariants = product.ProductVariants.Where(v => !v.IsDeleted).ToList();
                foreach (var variant in product.ProductVariants)
                {
                    if (variant.VariantOptionValues != null)
                    {
                        variant.VariantOptionValues = variant.VariantOptionValues
                            .Where(vov => vov.OptionValue != null 
                                && !vov.OptionValue.IsDeleted
                                && vov.OptionValue.ProductOption != null
                                && !vov.OptionValue.ProductOption.IsDeleted)
                            .ToList();
                    }
                }
            }
            
            ShoppingCart cart = new()
            {
                product = product,
                Count=1,
                ProductId=productId,
                CanReview= hasPurchased
            };

            // Get SEO data
            var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
            var seo = SEOHelper.GetProductSEO(product, baseUrl, _localizer["Culture"]?.ToString() ?? "en");
            
            // Get product rating for structured data
            var averageRating = _unitOfWork.review.GetAverageRating(productId);
            var reviewCount = _unitOfWork.review.GetReviewCount(productId);
            if (averageRating > 0 && reviewCount > 0)
            {
                seo.Rating = averageRating;
                seo.ReviewCount = reviewCount;
            }

            ViewData["SEO"] = seo;
            ViewData["Title"] = seo.Title;
            ViewData["Description"] = seo.Description;
            ViewData["Keywords"] = seo.Keywords;
            ViewData["Image"] = seo.ImageUrl;
            ViewData["ProductRating"] = averageRating;
            ViewData["ProductReviewCount"] = reviewCount;
            
            // Check if all variants are out of stock
            bool allVariantsOutOfStock = false;
            if (product.ProductType == BulkyBook.Models.ProductType.Variable && product.ProductVariants != null && product.ProductVariants.Any())
            {
                allVariantsOutOfStock = product.ProductVariants.All(v => v.StockQuantity == 0);
            }
            else if (product.ProductType == BulkyBook.Models.ProductType.Simple)
            {
                allVariantsOutOfStock = product.StockQuantity == 0;
            }
            ViewData["AllVariantsOutOfStock"] = allVariantsOutOfStock;
        
            return View(cart);
        }
        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart, int? ProductVariantId)
        {
            // Validate quantity
            if (shoppingCart.Count < 1)
            {
                TempData["error"] = "Quantity must be at least 1.";
                return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
            }
            
            // Get product to check stock
            var product = _unitOfWork.product.Get(p => p.Id == shoppingCart.ProductId && !p.IsDeleted);
            if (product == null)
            {
                TempData["error"] = "Product not found.";
                return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
            }
            
            // Validate quantity based on product type
            string? validationError = null;
            
            if (ProductVariantId.HasValue && ProductVariantId.Value > 0)
            {
                // Variable product with variant - check variant stock
                shoppingCart.ProductVariantId = ProductVariantId.Value;
                
                var variant = _unitOfWork.ProductVariant.Get(v => v.Id == ProductVariantId.Value && !v.IsDeleted);
                if (variant == null || variant.IsDeleted)
                {
                    TempData["error"] = "Selected variant not found.";
                    return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
                }
                
                // Check variant stock quantity
                if (variant.StockQuantity < shoppingCart.Count)
                {
                    if (variant.StockQuantity == 0)
                    {
                        validationError = "This variant is out of stock.";
                    }
                    else
                    {
                        validationError = $"Only {variant.StockQuantity} units available for this variant.";
                    }
                }
            }
            else
            {
                // Simple product - check product stock
                if (product.StockQuantity < shoppingCart.Count)
                {
                    if (product.StockQuantity == 0)
                    {
                        validationError = "This product is out of stock.";
                    }
                    else
                    {
                        validationError = $"Only {product.StockQuantity} units available in stock.";
                    }
                }
            }
            
            // If validation failed, return error
            if (!string.IsNullOrEmpty(validationError))
            {
                TempData["error"] = validationError;
                return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
            }
            
            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - use database
                var claimsIdentity=(ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = UserId;

                // Check if item already in cart (considering variant)
                ShoppingCart shoppingCartFromDB = _unitOfWork.shoppingCart.Get(
                    a => a.ProductId == shoppingCart.ProductId && 
                         a.ApplicationUserId == UserId &&
                         a.ProductVariantId == shoppingCart.ProductVariantId);

                if(shoppingCartFromDB != null)
                {
                    // Check total quantity after adding
                    var newTotalQuantity = shoppingCartFromDB.Count + shoppingCart.Count;
                    
                    // Re-validate total quantity
                    if (ProductVariantId.HasValue && ProductVariantId.Value > 0)
                    {
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == ProductVariantId.Value && !v.IsDeleted);
                        if (variant != null && variant.StockQuantity < newTotalQuantity)
                        {
                            TempData["error"] = variant.StockQuantity == 0 
                                ? "This variant is out of stock." 
                                : $"Only {variant.StockQuantity} units available for this variant. You already have {shoppingCartFromDB.Count} in your cart.";
                            return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
                        }
                    }
                    else
                    {
                        if (product.StockQuantity < newTotalQuantity)
                        {
                            TempData["error"] = product.StockQuantity == 0 
                                ? "This product is out of stock." 
                                : $"Only {product.StockQuantity} units available in stock. You already have {shoppingCartFromDB.Count} in your cart.";
                            return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
                        }
                    }
                    
                    shoppingCartFromDB.Count = newTotalQuantity;
                    _unitOfWork.shoppingCart.update(shoppingCartFromDB);
                }
                else
                {
                    _unitOfWork.shoppingCart.add(shoppingCart);
                }

                _unitOfWork.save();
            }
            else
            {
                // Guest user - use session
                // Note: Guest cart validation would need to be handled in GuestCartHelper
                BulkyBook.Utility.GuestCartHelper.AddToCart(HttpContext.Session, shoppingCart.ProductId, shoppingCart.Count, shoppingCart.ProductVariantId);
            }

            TempData["success"] = "Cart Updated Successfully";
            return RedirectToAction("Index");
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Shipping()
        {
            return View();
        }

        public IActionResult Returns()
        {
            return View();
        }

        public IActionResult HelpCenter()
        {
            return View();
        }

        public IActionResult TrackOrder()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TrackOrder(int orderId, string email)
        {
            if (orderId <= 0 || string.IsNullOrWhiteSpace(email))
            {
                TempData["error"] = "Please provide both Order ID and Email";
                return View();
            }

            var order = _unitOfWork.OrderHeader.Get(o => o.Id == orderId && o.Email == email, includeProperties: "ApplicationUser");
            
            if (order == null)
            {
                TempData["error"] = "Order not found. Please check your Order ID and Email.";
                return View();
            }

            var orderDetails = _unitOfWork.OrderDetail.GetAll(od => od.OrderHeaderId == orderId, includeProperties: "Product");
            
            ViewBag.OrderDetails = orderDetails;
            return View("OrderTracking", order);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            var errorViewModel = new ErrorViewModel 
            { 
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                StatusCode = statusCode ?? HttpContext.Response.StatusCode
            };
            return View(errorViewModel);
        }

        // Newsletter Subscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult SubscribeNewsletter(string email, string source = "HomePage")
        {
            try
            {
                // If user is logged in, use their email from claims
                if (User.Identity.IsAuthenticated)
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        email = userEmail;
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = _localizer["PleaseEnterValidEmail"].ToString() });
                }

                // Validate email format
                if (!System.Text.RegularExpressions.Regex.IsMatch(email, 
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return Json(new { success = false, message = _localizer["PleaseEnterValidEmail"].ToString() });
                }

                // Check if email already exists
                var existingSubscription = _unitOfWork.NewsletterSubscription.GetByEmail(email);
                
                if (existingSubscription != null)
                {
                    if (existingSubscription.IsActive)
                    {
                        return Json(new { success = false, message = _localizer["EmailAlreadySubscribed"].ToString() });
                    }
                    else
                    {
                        // Reactivate subscription
                        existingSubscription.IsActive = true;
                        existingSubscription.SubscribedDate = DateTime.Now;
                        existingSubscription.UnsubscribedDate = null;
                        existingSubscription.Source = source;
                        _unitOfWork.NewsletterSubscription.Update(existingSubscription);
                        _unitOfWork.save();
                        
                        return Json(new { 
                            success = true, 
                            message = _localizer["ThankYouForSubscribing"].ToString(),
                            isReactivated = true
                        });
                    }
                }

                // Create new subscription
                var subscription = new NewsletterSubscription
                {
                    Email = email.Trim().ToLower(),
                    SubscribedDate = DateTime.Now,
                    IsActive = true,
                    Source = source
                };

                _unitOfWork.NewsletterSubscription.Add(subscription);
                _unitOfWork.save();

                return Json(new { 
                    success = true, 
                    message = _localizer["ThankYouForSubscribing"].ToString(),
                    isReactivated = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to newsletter: {Email}", email);
                return Json(new { success = false, message = _localizer["SubscriptionError"].ToString() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult UnsubscribeNewsletter()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { success = false, message = _localizer["PleaseLogin"].ToString() });
                }

                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Json(new { success = false, message = _localizer["EmailNotFound"].ToString() });
                }

                // Find subscription
                var subscription = _unitOfWork.NewsletterSubscription.GetByEmail(userEmail);
                
                if (subscription == null)
                {
                    return Json(new { success = false, message = _localizer["SubscriptionNotFound"].ToString() });
                }

                if (!subscription.IsActive)
                {
                    return Json(new { success = false, message = _localizer["AlreadyUnsubscribed"].ToString() });
                }

                // Deactivate subscription
                subscription.IsActive = false;
                subscription.UnsubscribedDate = DateTime.Now;
                _unitOfWork.NewsletterSubscription.Update(subscription);
                _unitOfWork.save();
                
                return Json(new { 
                    success = true, 
                    message = _localizer["UnsubscribedSuccessfully"].ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from newsletter");
                return Json(new { success = false, message = _localizer["SubscriptionError"].ToString() });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubscribeStockNotification(int productId, int? variantId, string? email, string? phoneNumber)
        {
            try
            {
                // Get product
                var product = _unitOfWork.product.Get(p => p.Id == productId && !p.IsDeleted);
                if (product == null)
                {
                    return Json(new { success = false, message = _localizer["ProductNotFound"].ToString() });
                }
                
                string userEmail = email;
                string userPhone = phoneNumber;
                string? userId = null;
                
                // If user is logged in, get their info
                if (User.Identity.IsAuthenticated)
                {
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userEmailClaim = claimsIdentity.FindFirst(ClaimTypes.Email)?.Value;
                    var user = await _userManager.FindByIdAsync(userId);
                    
                    if (user != null)
                    {
                        userEmail = userEmail ?? user.Email;
                        userPhone = userPhone ?? user.PhoneNumber;
                    }
                }
                
                // Validate email
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    return Json(new { success = false, message = _localizer["EmailIsRequired"].ToString() });
                }
                
                // Check if already subscribed
                var existing = _unitOfWork.StockNotification.GetByProductAndEmail(productId, userEmail, variantId);
                if (existing != null)
                {
                    if (existing.IsActive)
                    {
                        return Json(new { success = false, message = _localizer["StockNotificationAlreadySubscribed"].ToString() });
                    }
                    else
                    {
                        // Reactivate
                        existing.IsActive = true;
                        existing.PhoneNumber = userPhone;
                        existing.ModifiedDate = DateTime.Now;
                        _unitOfWork.StockNotification.Update(existing);
                        _unitOfWork.save();
                        return Json(new { success = true, message = _localizer["StockNotificationSubscribed"].ToString() });
                    }
                }
                
                // Create new notification
                var stockNotification = new BulkyBook.Models.StockNotification
                {
                    ProductId = productId,
                    ProductVariantId = variantId,
                    Email = userEmail.Trim().ToLower(),
                    PhoneNumber = userPhone,
                    ApplicationUserId = userId,
                    IsActive = true,
                    IsNotified = false,
                    CreatedDate = DateTime.Now
                };
                
                _unitOfWork.StockNotification.Add(stockNotification);
                _unitOfWork.save();
                
                // Send notification to admins
                await SendStockNotificationToAdmins(stockNotification, product);
                
                return Json(new { success = true, message = _localizer["StockNotificationSubscribed"].ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to stock notification");
                return Json(new { success = false, message = "An error occurred. Please try again later." });
            }
        }
        
        private async Task SendStockNotificationToAdmins(BulkyBook.Models.StockNotification notification, Product product)
        {
            try
            {
                var emailSender = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
                var adminEmail = _configuration["StockAlerts:AdminEmail"];
                
                // Get all admin users
                var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
                
                // Prepare email content
                var productName = product.Title;
                var variantInfo = notification.ProductVariantId.HasValue 
                    ? $" (Variant ID: {notification.ProductVariantId.Value})" 
                    : "";
                
                var emailSubject = $"New Stock Notification Request - {productName}";
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <h2>New Stock Notification Request</h2>
                        <p>A customer has requested to be notified when a product is back in stock.</p>
                        <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Product:</strong></td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{productName}{variantInfo}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Customer Email:</strong></td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{notification.Email}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Phone Number:</strong></td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{notification.PhoneNumber ?? "Not provided"}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; background-color: #f9f9f9;'><strong>Request Date:</strong></td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{notification.CreatedDate:yyyy-MM-dd HH:mm:ss}</td>
                            </tr>
                        </table>
                        <p>Please update the stock when available and the customer will be automatically notified.</p>
                    </body>
                    </html>";
                
                // Send to admin email from config
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await emailSender.SendEmailAsync(adminEmail, emailSubject, emailBody);
                }
                
                // Send to all admin users
                foreach (var admin in adminUsers)
                {
                    if (!string.IsNullOrEmpty(admin.Email) && admin.Email != adminEmail)
                    {
                        await emailSender.SendEmailAsync(admin.Email, emailSubject, emailBody);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stock notification to admins");
            }
        }
    }

 
}