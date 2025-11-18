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
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		private readonly BulkyBook.Services.INotificationService _notificationService;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, BulkyBook.Services.INotificationService notificationService) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_notificationService = notificationService;
        }
        public IActionResult Index()
        {
            IEnumerable<ShoppingCart> cartList;

            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - load from database
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                cartList = _unitOfWork.shoppingCart.GetAll(a=>a.ApplicationUserId==UserId,
                    includeProperties: "product");
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartList = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry")
                }).ToList();
            }

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = cartList
            };

            foreach(var cart  in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price=GetPriceBasedOnQty(cart);
                ShoppingCartVM.OrderTotal +=(cart.Price*cart.Count);
            }
            return View(ShoppingCartVM);
        }

        [HttpGet]
        public IActionResult GetCartItems()
        {
            IEnumerable<ShoppingCart> cartItems;

            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "product");
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartItems = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry")
                }).ToList();
            }
            
            var items = cartItems.Select(cart => new
            {
                productId = cart.ProductId,
                title = cart.product.Title,
                imageUrl = cart.product.ImageUrl,
                price = GetPriceBasedOnQty(cart),
                count = cart.Count,
                cartId = cart.Id
            }).ToList();

            var subtotal = cartItems.Sum(cart => GetPriceBasedOnQty(cart) * cart.Count);

            return Json(new { items = items, subtotal = subtotal });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int productId, int count)
        {
            if (User.Identity.IsAuthenticated)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                var cartItem = _unitOfWork.shoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == productId);
                
                if (cartItem != null)
                {
                    cartItem.Count = count;
                    _unitOfWork.shoppingCart.update(cartItem);
                    _unitOfWork.save();
                    
                    return Json(new { success = true, message = "Quantity updated successfully!" });
                }
            }
            else
            {
                // Guest user - update session
                BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, productId, count);
                return Json(new { success = true, message = "Quantity updated successfully!" });
            }

            return Json(new { success = false, message = "Item not found in cart" });
        }

        public IActionResult Pluse(int CartId, int? ProductId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartFromDD=_unitOfWork.shoppingCart.Get(a=>a.Id==CartId);
                cartFromDD.Count += 1;
                _unitOfWork.shoppingCart.update(cartFromDD);
                _unitOfWork.save();
            }
            else if (ProductId.HasValue)
            {
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                if (item != null)
                {
                    BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, item.Count + 1);
                }
            }
            return RedirectToAction(nameof(Index));    
        }
        
        public IActionResult Minus(int CartId, int? ProductId)
        {
            if (User.Identity.IsAuthenticated)
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
            }
            else if (ProductId.HasValue)
            {
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                if (item != null)
                {
                    if (item.Count <= 1)
                    {
                        BulkyBook.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
                    }
                    else
                    {
                        BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, item.Count - 1);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        public IActionResult Remove(int CartId, int? ProductId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
                _unitOfWork.shoppingCart.remove(cartFromDD);
                _unitOfWork.save();
            }
            else if (ProductId.HasValue)
            {
                BulkyBook.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
            }
            return RedirectToAction(nameof(Index));
        }
		public IActionResult Summary()
		{
			IEnumerable<ShoppingCart> cartList;

			ShoppingCartVM = new()
			{
				OrderHeader = new()
			};

			if (User.Identity.IsAuthenticated)
			{
				// Authenticated user
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product");

				ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
				ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
				ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
				ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
				ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
				ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
				ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			}
			else
			{
				// Guest user - load from session
				var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
				if (guestCart.Count == 0)
				{
					TempData["error"] = "Your cart is empty";
					return RedirectToAction("Index", "Home");
				}

				cartList = guestCart.Select(gc => new ShoppingCart
				{
					ProductId = gc.ProductId,
					Count = gc.Count,
					product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry")
				}).ToList();

				// Initialize empty fields for guest
				ShoppingCartVM.OrderHeader.IsGuestOrder = true;
			}

			ShoppingCartVM.ShoppingCartList = cartList;

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
			IEnumerable<ShoppingCart> cartList;
			bool isGuest = !User.Identity.IsAuthenticated;
			string userId = null;
			ApplicationUser applicationUser = null;

			if (!isGuest)
			{
				// Authenticated user
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product");
				applicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
			}
			else
			{
				// Guest user - load from session
				var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
				if (guestCart.Count == 0)
				{
					TempData["error"] = "Your cart is empty";
					return RedirectToAction("Index", "Home");
				}

				cartList = guestCart.Select(gc => new ShoppingCart
				{
					ProductId = gc.ProductId,
					Count = gc.Count,
					product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry")
				}).ToList();
			}

			ShoppingCartVM.ShoppingCartList = cartList;

			if (ShoppingCartVM.ShoppingCartList.Count() > 0)
			{
				ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
				
				if (isGuest)
				{
					ShoppingCartVM.OrderHeader.IsGuestOrder = true;
					ShoppingCartVM.OrderHeader.ApplicationUserId = null;
				}
				else
				{
					ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
				}


				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetPriceBasedOnQuantity(cart);
					ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
				}

				if (isGuest || applicationUser.CompanyId.GetValueOrDefault() == 0)
				{
					//it is a guest or regular customer 
					ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
					ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
				}
				else
				{
					//it is a company user
					ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
					ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPaid;
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

				if (isGuest || applicationUser.CompanyId.GetValueOrDefault() == 0)
				{
					//it is a guest or regular customer account and we need to capture payment
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
								Currency = "AED",
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


		public async Task<IActionResult> OrderConfirmation(int id)
		{
			// Get order header - don't include ApplicationUser for guest orders
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
			
			if (orderHeader == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}

			// Load ApplicationUser only if it's not a guest order
			if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
			{
				orderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
			}

			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				//this is an order by customer

				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusPaid, SD.PaymentStatusPaid);
					_unitOfWork.save();
				}
				//HttpContext.Session.Clear();

			}

			// Send notifications to all admins
			await _notificationService.SendOrderNotificationToAdmins(orderHeader);

			// Send order confirmation to customer
			if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
			{
				var customer = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
				if (customer != null)
				{
					await _notificationService.SendOrderConfirmationToCustomer(orderHeader, customer);
				}

				// Clear cart from database for authenticated users
				List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
					.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

				_unitOfWork.shoppingCart.removeRage(shoppingCarts);
				_unitOfWork.save();
			}
			else
			{
				// Clear cart from session for guest users
				BulkyBook.Utility.GuestCartHelper.ClearCart(HttpContext.Session);
				
				// TODO: Send order confirmation email to guest user's email
				// You can implement email sending here using orderHeader.Email
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

}
