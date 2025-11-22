using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBookWeb.ViewComponents {
    public class ShoppingCartViewComponent : ViewComponent {

        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartViewComponent(IUnitOfWork unitOfWork) {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync() 
        {
            int count = 0;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                if (claim != null)
                {
                    // Authenticated user - get count of unique products (not sum of quantities)
                    var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == claim.Value);
                    count = cartItems.Count(); // Count unique products, not sum of quantities
                }
            }
            else
            {
                // Guest user - get count of unique products from session
                var guestCart = GuestCartHelper.GetGuestCart(HttpContext.Session);
                count = guestCart.Count; // Count unique products, not sum of quantities
            }
            
            return View(count);
        }

    }
}
