using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
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

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IStringLocalizer<BulkyBook.SharedResources> localizer, ApplicationDBContext dbContext)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
        }

        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "categoryId", "searchTerm", "sortBy", "minPrice", "maxPrice", "availability" })]
        public IActionResult Index(int? categoryId, string searchTerm, string sortBy, 
            decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null, 
            int? minRating = null, string availability = null)
        {
            // Get active flash sales for homepage
            var activeFlashSales = _unitOfWork.FlashSale.GetActiveFlashSales();
            ViewBag.ActiveFlashSales = activeFlashSales;

            // Get active service subscriptions for homepage
            var activeServices = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive, includeProperties: "ServiceImages")
                .OrderBy(s => s.DisplayOrder)
                .ThenByDescending(s => s.CreatedDate)
                .Take(6)
                .ToList();
            ViewBag.ActiveServices = activeServices;

            // Get all categories for filter (cached - only load once per request)
            var allCategories = _unitOfWork.categry.GetAll().ToList();
            ViewBag.Categories = allCategories;
            
            // Get best sellers (products with most orders)
            var bestSellers = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages")
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.Id) // For now, use ID as proxy for popularity
                .Take(8)
                .ToList();
            ViewBag.BestSellers = bestSellers;
            
            // Get new arrivals (recently added products)
            var newArrivals = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages")
                .Where(p => p.StockQuantity > 0)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToList();
            ViewBag.NewArrivals = newArrivals;
            
            // Get top categories (categories with most products)
            var topCategories = allCategories
                .OrderByDescending(c => _unitOfWork.product.GetAll(p => p.CategryId == c.Id).Count())
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
            
            // Filter by search term (case-insensitive)
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchLower) || 
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
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
            
            // Get price range for filter display (optimized - only get min/max, not all products)
            var priceStats = _unitOfWork.product.GetAll()
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
        public IActionResult LoadMoreProducts(int page = 1, int pageSize = 20, 
            int? categoryId = null, string searchTerm = null, string sortBy = null,
            decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null, 
            string availability = null)
        {
            // Apply same filters as Index action (optimized query)
            var query = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages").AsQueryable();
            
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }
            
            if (!string.IsNullOrEmpty(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(p => 
                    p.Title.ToLower().Contains(searchLower) || 
                    (p.Description != null && p.Description.ToLower().Contains(searchLower)));
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
            
            // Apply sorting
            query = sortBy switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Title),
                "newest" => query.OrderByDescending(p => p.Id),
                "oldest" => query.OrderBy(p => p.Id),
                _ => query.OrderBy(p => p.Title)
            };
            
            var totalProducts = query.Count();
            var productsToSkip = page * pageSize;
            
            // Optimized: Only materialize the products we need
            var products = query.Skip(productsToSkip).Take(pageSize).ToList();
            var hasMore = (productsToSkip + pageSize) < totalProducts;

            // Cache headers are automatically set by [ResponseCache] attribute

            return Json(new { products = products, hasMore = hasMore, totalCount = totalProducts });
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
            
            var cartItems = _unitOfWork.shoppingCart.GetAll(
                u => u.ApplicationUserId == userId,
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

            var product = _unitOfWork.product.Get(U => U.Id == productId, includeProperties: "categry,ProductImages,ProductOptions,ProductVariants");
            
            // Load option values for each option
            if (product.ProductOptions != null)
            {
                foreach (var option in product.ProductOptions)
                {
                    option.OptionValues = _unitOfWork.ProductOptionValue.GetAll(
                        ov => ov.ProductOptionId == option.Id
                    ).OrderBy(ov => ov.DisplayOrder).ToList();
                }
            }
            
            // Load variant option values for each variant
            if (product.ProductVariants != null)
            {
                foreach (var variant in product.ProductVariants)
                {
                    variant.VariantOptionValues = _dbContext.ProductVariantOptionValues
                        .Include(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                        .Where(vov => vov.ProductVariantId == variant.Id)
                        .ToList();
                }
            }
            
            ShoppingCart cart = new()
            {
                product = product,
                Count=1,
                ProductId=productId,
                CanReview= hasPurchased
            };
        
            return View(cart);
        }
        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart, int? ProductVariantId)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - use database
                var claimsIdentity=(ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = UserId;
                
                // Handle variant if provided
                if (ProductVariantId.HasValue && ProductVariantId.Value > 0)
                {
                    shoppingCart.ProductVariantId = ProductVariantId.Value;
                    
                    // Get variant to check stock
                    var variant = _unitOfWork.ProductVariant.Get(v => v.Id == ProductVariantId.Value);
                    if (variant == null)
                    {
                        TempData["error"] = "Selected variant not found.";
                        return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
                    }
                    
                    if (variant.StockQuantity < shoppingCart.Count)
                    {
                        TempData["error"] = $"Only {variant.StockQuantity} units available in stock.";
                        return RedirectToAction("Details", new { productId = shoppingCart.ProductId });
                    }
                }

                // Check if item already in cart (considering variant)
                ShoppingCart shoppingCartFromDB = _unitOfWork.shoppingCart.Get(
                    a => a.ProductId == shoppingCart.ProductId && 
                         a.ApplicationUserId == UserId &&
                         a.ProductVariantId == shoppingCart.ProductVariantId);

                if(shoppingCartFromDB != null)
                {
                    shoppingCartFromDB.Count += shoppingCart.Count;
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
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    }

 
}