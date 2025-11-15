using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;
using Newtonsoft.Json;

namespace BulkyBook.Areas.Customer.Controllers
{
	[Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		public ShoppingCartVM  ShoppingCartVM { get; set; }
        private const string SessionCartKey = "SessionCart";

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
        }
        public IActionResult Index()
        {
            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = new List<ShoppingCart>(),
                OrderHeader = new()
            };

            if (User.Identity.IsAuthenticated)
            {
                // Get cart from database for authenticated users
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                ShoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == UserId,
                    includeProperties: "product").ToList();
            }
            else
            {
                // Get cart from session for anonymous users
                var sessionCart = GetSessionCart();
                foreach (var item in sessionCart)
                {
                    var product = _unitOfWork.product.Get(p => p.Id == item.ProductId, includeProperties: "categry");
                    if (product != null)
                    {
                        ShoppingCartVM.ShoppingCartList.Add(new ShoppingCart
                        {
                            ProductId = item.ProductId,
                            product = product,
                            Count = item.Count
                        });
                    }
                }
            }

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQty(cart);
                ShoppingCartVM.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
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

        public IActionResult Pluse(int CartId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
                cartFromDD.Count += 1;
                _unitOfWork.shoppingCart.update(cartFromDD);
                _unitOfWork.save();
            }
            else
            {
                // Handle session cart - CartId is actually ProductId for session
                var sessionCart = GetSessionCart();
                var item = sessionCart.FirstOrDefault(c => c.ProductId == CartId);
                if (item != null)
                {
                    item.Count += 1;
                    SaveSessionCart(sessionCart);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Minus(int CartId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
                if (cartFromDD.Count <= 1)
                {
                    _unitOfWork.shoppingCart.remove(cartFromDD);
                }
                else
                {
                    cartFromDD.Count -= 1;
                    _unitOfWork.shoppingCart.update(cartFromDD);
                }

                _unitOfWork.save();
            }
            else
            {
                // Handle session cart - CartId is actually ProductId for session
                var sessionCart = GetSessionCart();
                var item = sessionCart.FirstOrDefault(c => c.ProductId == CartId);
                if (item != null)
                {
                    if (item.Count <= 1)
                    {
                        sessionCart.Remove(item);
                    }
                    else
                    {
                        item.Count -= 1;
                    }
                    SaveSessionCart(sessionCart);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Remove(int CartId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
                _unitOfWork.shoppingCart.remove(cartFromDD);
                _unitOfWork.save();
            }
            else
            {
                // Handle session cart - CartId is actually ProductId for session
                var sessionCart = GetSessionCart();
                var item = sessionCart.FirstOrDefault(c => c.ProductId == CartId);
                if (item != null)
                {
                    sessionCart.Remove(item);
                    SaveSessionCart(sessionCart);
                }
            }
            return RedirectToAction(nameof(Index));
        }
		public IActionResult Summary()
		{
			ShoppingCartVM = new()
			{
				ShoppingCartList = new List<ShoppingCart>(),
				OrderHeader = new()
			};

			if (User.Identity.IsAuthenticated)
			{
				// Authenticated user - get cart from database
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				ShoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product").ToList();

				ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);

				// Pre-fill user details
				ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
				ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
				ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
				ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
				ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
				ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			}
			else
			{
				// Anonymous user - get cart from session
				var sessionCart = GetSessionCart();
				foreach (var item in sessionCart)
				{
					var product = _unitOfWork.product.Get(p => p.Id == item.ProductId, includeProperties: "categry");
					if (product != null)
					{
						ShoppingCartVM.ShoppingCartList.Add(new ShoppingCart
						{
							ProductId = item.ProductId,
							product = product,
							Count = item.Count
						});
					}
				}
				// Leave customer details empty for guest to fill
			}

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
			bool isGuest = false;
			string userId = null;
			ApplicationUser applicationUser = null;

			if (User.Identity.IsAuthenticated)
			{
				// Authenticated user
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				
				ShoppingCartVM.ShoppingCartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product").ToList();
				
				applicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
			}
			else
			{
				// Guest user
				isGuest = true;
				var sessionCart = GetSessionCart();
				ShoppingCartVM.ShoppingCartList = new List<ShoppingCart>();
				
				foreach (var item in sessionCart)
				{
					var product = _unitOfWork.product.Get(p => p.Id == item.ProductId, includeProperties: "categry");
					if (product != null)
					{
						ShoppingCartVM.ShoppingCartList.Add(new ShoppingCart
						{
							ProductId = item.ProductId,
							product = product,
							Count = item.Count
						});
					}
				}
			}

			if (ShoppingCartVM.ShoppingCartList.Count() > 0)
			{
				ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
				ShoppingCartVM.OrderHeader.ApplicationUserId = userId; // Will be null for guest

				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetPriceBasedOnQuantity(cart);
					ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
				}

				// Set payment status - guest and regular users need immediate payment
				if (isGuest || applicationUser?.CompanyId.GetValueOrDefault() == 0)
				{
					ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
					ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
				}
				else
				{
					// Company user with delayed payment
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

				// Process payment for guest and regular customers
				if (isGuest || applicationUser?.CompanyId.GetValueOrDefault() == 0)
				{
					// Stripe payment logic
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
								UnitAmount = (long)(item.Price * 100),
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
					
					// Clear session cart for guest users
					if (isGuest)
					{
						HttpContext.Session.Remove(SessionCartKey);
					}
					
					Response.Headers.Add("Location", session.Url);
					return new StatusCodeResult(303);
				}

				// Clear cart for authenticated users (DB)
				if (!isGuest)
				{
					List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
						.GetAll(u => u.ApplicationUserId == userId).ToList();
					_unitOfWork.shoppingCart.removeRage(shoppingCarts);
					_unitOfWork.save();
				}

				return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
			}
			else
			{
				TempData["error"] = "You Need to Add Items in the Cart";
			}
			return RedirectToAction(nameof(Index), "Home");
		}


		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//this is an order by customer or guest

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.save();
				}
			}

			// Send email only if user has an email (authenticated users)
			if (orderHeader.ApplicationUser != null && !string.IsNullOrEmpty(orderHeader.ApplicationUser.Email))
			{
				_emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order - Bulky Book",
					$"<p>New Order Created - {orderHeader.Id}</p>");
			}

			// Clear cart for authenticated users only
			if (!string.IsNullOrEmpty(orderHeader.ApplicationUserId))
			{
				List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
					.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

				_unitOfWork.shoppingCart.removeRage(shoppingCarts);
				_unitOfWork.save();
			}

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

    // Helper class for session-based cart
    public class SessionCartItem
    {
        public int ProductId { get; set; }
        public int Count { get; set; }
    }
}
