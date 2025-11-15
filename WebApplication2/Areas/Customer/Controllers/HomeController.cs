using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using Newtonsoft.Json;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
 
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private const string SessionCartKey = "SessionCart";

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> ProductList =_unitOfWork.product.GetAll(includeProperties: "categry");
            return View(ProductList);
        }
        
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                product = _unitOfWork.product.Get(U => U.Id == productId, includeProperties: "categry"),
                Count=1,
                ProductId=productId

            };
        
            return View(cart);
        }
        
        [HttpPost]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            // Check if user is authenticated
            if (User.Identity.IsAuthenticated)
            {
                // Handle authenticated user - save to database
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                shoppingCart.ApplicationUserId = UserId;

                ShoppingCart shoppingCartFromDB = _unitOfWork.shoppingCart.Get(a => a.ProductId == shoppingCart.ProductId && a.ApplicationUserId == UserId);

                if (shoppingCartFromDB != null)
                {
                    shoppingCartFromDB.Count += shoppingCart.Count;
                    _unitOfWork.shoppingCart.update(shoppingCartFromDB);
                }
                else
                {
                    _unitOfWork.shoppingCart.add(shoppingCart);
                }

                _unitOfWork.save();
                TempData["success"] = "Cart Updated Successfully";
            }
            else
            {
                // Handle anonymous user - save to session
                var sessionCart = GetSessionCart();
                
                var existingItem = sessionCart.FirstOrDefault(c => c.ProductId == shoppingCart.ProductId);
                if (existingItem != null)
                {
                    existingItem.Count += shoppingCart.Count;
                }
                else
                {
                    sessionCart.Add(new SessionCartItem
                    {
                        ProductId = shoppingCart.ProductId,
                        Count = shoppingCart.Count
                    });
                }
                
                SaveSessionCart(sessionCart);
                TempData["success"] = "Cart Updated Successfully";
            }

            return RedirectToAction("Index");
        }

        // Helper methods for session cart
        private List<SessionCartItem> GetSessionCart()
        {
            var cartJson = HttpContext.Session.GetString(SessionCartKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<SessionCartItem>();
            }
            return JsonConvert.DeserializeObject<List<SessionCartItem>>(cartJson);
        }

        private void SaveSessionCart(List<SessionCartItem> cart)
        {
            var cartJson = JsonConvert.SerializeObject(cart);
            HttpContext.Session.SetString(SessionCartKey, cartJson);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Helper class for session-based cart
    public class SessionCartItem
    {
        public int ProductId { get; set; }
        public int Count { get; set; }
    }
}