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
                    // Authenticated user - get total count from database
                    var cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == claim.Value);
                    count = cartItems.Sum(c => c.Count);
                }
            }
            else
            {
                // Guest user - get total count from session
                var guestCart = GuestCartHelper.GetGuestCart(HttpContext.Session);
                count = guestCart.Sum(gc => gc.Count);
            }
            
            return View(count);
        }

    }
}
