using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork) 
        {
         _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList=_unitOfWork.shoppingCart.GetAll(a=>a.ApplicationUserId==UserId,
                includeProperties: "product"

                )
                
            };
            foreach(var cart  in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price=GetPriceBasedOnQty(cart);
                ShoppingCartVM.OrderTotal +=(cart.Price*cart.Count);
            }
            return View(ShoppingCartVM);
        }

        public IActionResult Pluse( int CartId)
        {
            var cartFromDD=_unitOfWork.shoppingCart.Get(a=>a.Id==CartId);
            cartFromDD.Count += 1;
            _unitOfWork.shoppingCart.update(cartFromDD);
            _unitOfWork.save();
            return RedirectToAction(nameof(Index));    
        }
        public IActionResult Minus(int CartId)
        {
            var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
            if(cartFromDD.Count <= 1) 
            { 
                _unitOfWork.shoppingCart.remove(cartFromDD);

            }
            else
            {
                cartFromDD.Count -= 1;
                _unitOfWork.shoppingCart.update(cartFromDD);
            }
          
            _unitOfWork.save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int CartId)
        {
            var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
            
                _unitOfWork.shoppingCart.remove(cartFromDD);

            _unitOfWork.save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            return View();
        }
        private double  GetPriceBasedOnQty(ShoppingCart  shoppingCart) 
        { 
            if(shoppingCart.Count<=50)
            {
                return shoppingCart.product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.product.Price50;
                }
                else
                {
                    return shoppingCart.product.Price100;  

                }
            }
        
        }
    }

}
