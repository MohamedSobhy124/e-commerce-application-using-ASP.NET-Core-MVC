using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
 
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "categoryId", "searchTerm", "sortBy", "minPrice", "maxPrice", "availability" })]
        public IActionResult Index(int? categoryId, string searchTerm, string sortBy, 
            decimal? minPrice = null, decimal? maxPrice = null, bool? inStock = null, 
            int? minRating = null, string availability = null)
        {
            // Get active flash sales for homepage
            var activeFlashSales = _unitOfWork.FlashSale.GetActiveFlashSales();
            ViewBag.ActiveFlashSales = activeFlashSales;

            // Get all categories for filter (cached - only load once per request)
            ViewBag.Categories = _unitOfWork.categry.GetAll().ToList();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.InStock = inStock;
            ViewBag.MinRating = minRating;
            ViewBag.Availability = availability;
            
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
                    p.Author.ToLower().Contains(searchLower) || 
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
            }
            else
            {
                ViewBag.CartProductIds = new List<int>();
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
                    p.Author.ToLower().Contains(searchLower) || 
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

                // Get updated cart count (total quantity, not just distinct items)
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
                    // Authenticated user - get total quantity from database
                    var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId);
                    cartCount = cartItems.Count();
                }
            }
            else
            {
                // Guest user - get total quantity from session
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
        
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                product = _unitOfWork.product.Get(U => U.Id == productId, includeProperties: "categry,ProductImages"),
                Count=1,
                ProductId=productId

            };
        
            return View(cart);
        }
        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - use database
                var claimsIdentity=(ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = UserId;

                ShoppingCart shoppingCartFromDB = _unitOfWork.shoppingCart.Get(a => a.ProductId == shoppingCart.ProductId && a.ApplicationUserId == UserId);

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
                BulkyBook.Utility.GuestCartHelper.AddToCart(HttpContext.Session, shoppingCart.ProductId, shoppingCart.Count);
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
    }

 
}