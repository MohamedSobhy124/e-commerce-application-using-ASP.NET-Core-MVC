using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Newtonsoft.Json;
using BulkyBook.Areas.Customer.Controllers;

namespace BulkyBookWeb.ViewComponents {
    public class ShoppingCartViewComponent : ViewComponent {

        private readonly IUnitOfWork _unitOfWork;
        private const string SessionCartKey = "SessionCart";
        
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync() 
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null) {
                // Authenticated user - get count from database
                var count = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).Count();
                return View(count);
            }
            else {
                // Anonymous user - get count from session
                var cartJson = HttpContext.Session.GetString(SessionCartKey);
                if (!string.IsNullOrEmpty(cartJson))
                {
                    var sessionCart = JsonConvert.DeserializeObject<List<SessionCartItem>>(cartJson);
                    return View(sessionCart?.Count ?? 0);
                }
                return View(0);
            }
        }

    }
}
