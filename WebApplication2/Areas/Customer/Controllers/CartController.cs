using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
	[Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
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
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "product"),
				OrderHeader = new()
			};

			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);

			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;



			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}
			return View(ShoppingCartVM);
		}


		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST(ShoppingCartVM ShoppingCartVM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "product");
			if (ShoppingCartVM.ShoppingCartList.Count() > 0)
			{
				ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
				ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

				ApplicationUser applicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);


				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetPriceBasedOnQuantity(cart);
					ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
				}

				if (applicationUser.CompanyId.GetValueOrDefault() == 0)
				{
					//it is a regular customer 
					ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
					ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
				}
				else
				{
					//it is a company user
					ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
					ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
				}
				_unitOfWork.OrderHeader.add(ShoppingCartVM.OrderHeader);
				_unitOfWork.save();
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					OrderDetail orderDetail = new()
					{
						ProductId = cart.ProductId,
						OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
						Price = cart.Price,
						Count = cart.Count
					};
					_unitOfWork.OrderDetail.add(orderDetail);
					_unitOfWork.save();
				}

				if (applicationUser.CompanyId.GetValueOrDefault() == 0)
				{
					//it is a regular customer account and we need to capture payment
					//stripe logic
					var domain = Request.Scheme + "://" + Request.Host.Value + "/";
					var options = new SessionCreateOptions
					{
						SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
						CancelUrl = domain + "customer/cart/index",
						LineItems = new List<SessionLineItemOptions>(),
						Mode = "payment",
					};

					foreach (var item in ShoppingCartVM.ShoppingCartList)
					{
						var sessionLineItem = new SessionLineItemOptions
						{
							PriceData = new SessionLineItemPriceDataOptions
							{
								UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
								Currency = "usd",
								ProductData = new SessionLineItemPriceDataProductDataOptions
								{
									Name = item.product.Title
								}
							},
							Quantity = item.Count
						};
						options.LineItems.Add(sessionLineItem);
					}


					var service = new SessionService();
					Session session = service.Create(options);
					_unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
					_unitOfWork.save();
					Response.Headers.Add("Location", session.Url);
					return new StatusCodeResult(303);

				}

				return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
			}
			else
				TempData["success"] = "You Need to Add Items in the Cart";
			return RedirectToAction(nameof(Index),"Home");
		}


		public IActionResult OrderConfirmation(int id)
		{

			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//this is an order by customer

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.save();
				}
				//HttpContext.Session.Clear();

			}

			_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book",
				$"<p>New Order Created - {orderHeader.Id}</p>");

			List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
				.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			_unitOfWork.shoppingCart.removeRage(shoppingCarts);
			_unitOfWork.save();

			return View(id);
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
        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
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
