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

        public IActionResult Index(int? categoryId, string searchTerm, string sortBy)
        {
            // Get all categories for filter
            ViewBag.Categories = _unitOfWork.categry.GetAll().ToList();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SortBy = sortBy;
            
            // Get products with optional filtering
            var query = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages").AsQueryable();
            
            // Filter by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategryId == categoryId.Value);
            }
            
            // Filter by search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => 
                    p.Title.Contains(searchTerm) || 
                    p.Author.Contains(searchTerm) || 
                    p.Description.Contains(searchTerm));
            }
            
            // Sort products
            query = sortBy switch
            {
                "price_low" => query.OrderBy(p => p.Price),
                "price_high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Title),
                "newest" => query.OrderByDescending(p => p.Id),
                _ => query.OrderBy(p => p.Title)
            };
            
            IEnumerable<Product> ProductList = query.Take(20).ToList();
            
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
        public IActionResult LoadMoreProducts(int page = 1, int pageSize = 20)
        {
            var allProducts = _unitOfWork.product.GetAll(includeProperties: "categry,ProductImages").ToList();
            var totalProducts = allProducts.Count;
            var productsToSkip = page * pageSize;
            
            var products = allProducts.Skip(productsToSkip).Take(pageSize).ToList();
            
            var hasMore = (productsToSkip + pageSize) < totalProducts;

            return Json(new { products = products, hasMore = hasMore });
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

                // Get updated cart count
                cartCount = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == UserId).Count();
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

                cartCount = BulkyBook.Utility.GuestCartHelper.GetCartCount(HttpContext.Session);
            }

            return Json(new { success = true, message = message, isAdded = isAdded, cartCount = cartCount });
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetCartCount()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            var cartCount = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == UserId).Count();
            
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