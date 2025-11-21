using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
		private readonly BulkyBook.Services.IStockService _stockService;
		private readonly TappySettings _tappySettings;
		private readonly TamaraSettings _tamaraSettings;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, BulkyBook.Services.INotificationService notificationService, BulkyBook.Services.IStockService stockService, IOptions<TappySettings> tappySettings, IOptions<TamaraSettings> tamaraSettings) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_notificationService = notificationService;
			_stockService = stockService;
			_tappySettings = tappySettings.Value;
			_tamaraSettings = tamaraSettings.Value;
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
		public async Task<IActionResult> SummaryPOST(ShoppingCartVM ShoppingCartVM)
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
					var domain = Request.Scheme + "://" + Request.Host.Value + "/";
					
					// Check payment method
					if (ShoppingCartVM.OrderHeader.PaymentMethod == SD.PaymentMethodTappy)
					{
						// Tappy payment logic
						if (_tappySettings.Enabled)
						{
							// Store payment method
							var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == ShoppingCartVM.OrderHeader.Id);
							orderHeader.PaymentMethod = SD.PaymentMethodTappy;
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							// Create Tappy payment
							var tappyHelper = new TappyHelper(_tappySettings);
							var tappyRequest = new TappyPaymentRequest
							{
								MerchantId = _tappySettings.MerchantId,
								Amount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal,
								Currency = "AED",
								OrderId = ShoppingCartVM.OrderHeader.Id.ToString(),
								CustomerName = ShoppingCartVM.OrderHeader.Name,
								CustomerEmail = ShoppingCartVM.OrderHeader.Email ?? "",
								CustomerPhone = ShoppingCartVM.OrderHeader.PhoneNumber,
								ReturnUrl = domain + $"customer/cart/TappyCallback?orderId={ShoppingCartVM.OrderHeader.Id}",
								CancelUrl = domain + "customer/cart/index",
								Description = $"Order #{ShoppingCartVM.OrderHeader.Id} - {ShoppingCartVM.ShoppingCartList.Count()} items"
							};

							var tappyResponse = await tappyHelper.CreatePaymentAsync(tappyRequest);
							
							if (tappyResponse.Success && !string.IsNullOrEmpty(tappyResponse.PaymentUrl))
							{
								// Store Tappy transaction ID
								orderHeader.SessionId = tappyResponse.TransactionId;
								_unitOfWork.OrderHeader.Update(orderHeader);
								_unitOfWork.save();
								
								// Redirect to Tappy payment page
								Response.Headers.Add("Location", tappyResponse.PaymentUrl);
								return new StatusCodeResult(303);
							}
							else
							{
								TempData["error"] = "Failed to create Tappy payment: " + tappyResponse.Message;
								return RedirectToAction("Summary");
							}
						}
						else
						{
							TempData["error"] = "Tappy payment is currently unavailable";
							return RedirectToAction("Summary");
						}
					}
					else if (ShoppingCartVM.OrderHeader.PaymentMethod == SD.PaymentMethodTamara)
					{
						// Tamara payment logic (Buy Now, Pay Later)
						if (_tamaraSettings.Enabled)
						{
							// Store payment method
							var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == ShoppingCartVM.OrderHeader.Id);
							orderHeader.PaymentMethod = SD.PaymentMethodTamara;
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							// Create Tamara checkout
							var tamaraHelper = new TamaraHelper(_tamaraSettings);
							
							// Split customer name
							var nameParts = ShoppingCartVM.OrderHeader.Name.Split(' ', 2);
							var firstName = nameParts[0];
							var lastName = nameParts.Length > 1 ? nameParts[1] : "";
							
							var tamaraRequest = new TamaraPaymentRequest
							{
								OrderReferenceId = ShoppingCartVM.OrderHeader.Id.ToString(),
								TotalAmount = new TamaraAmount
								{
									Amount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal,
									Currency = "AED"
								},
								Description = $"Order #{ShoppingCartVM.OrderHeader.Id}",
								CountryCode = "AE",
								PaymentType = "PAY_BY_INSTALMENTS",
								Locale = "en_US",
								MerchantUrl = new TamaraMerchantUrl
								{
									Success = domain + $"customer/cart/TamaraCallback?orderId={ShoppingCartVM.OrderHeader.Id}&status=success",
									Failure = domain + $"customer/cart/TamaraCallback?orderId={ShoppingCartVM.OrderHeader.Id}&status=failure",
									Cancel = domain + "customer/cart/index",
									Notification = domain + $"customer/cart/TamaraNotification"
								},
								Consumer = new TamaraConsumer
								{
									FirstName = firstName,
									LastName = lastName,
									PhoneNumber = ShoppingCartVM.OrderHeader.PhoneNumber,
									Email = ShoppingCartVM.OrderHeader.Email ?? ""
								},
								Items = ShoppingCartVM.ShoppingCartList.Select(item => new TamaraItem
								{
									ReferenceId = item.ProductId.ToString(),
									Name = item.product.Title,
									Quantity = item.Count,
									TotalAmount = new TamaraAmount
									{
										Amount = (decimal)(item.Price * item.Count),
										Currency = "AED"
									}
								}).ToList()
							};

							var tamaraResponse = await tamaraHelper.CreateCheckoutAsync(tamaraRequest);
							
							if (tamaraResponse.Success && !string.IsNullOrEmpty(tamaraResponse.CheckoutUrl))
							{
								// Store Tamara checkout ID
								orderHeader.SessionId = tamaraResponse.CheckoutId;
								_unitOfWork.OrderHeader.Update(orderHeader);
								_unitOfWork.save();
								
								// Redirect to Tamara checkout page
								Response.Headers.Add("Location", tamaraResponse.CheckoutUrl);
								return new StatusCodeResult(303);
							}
							else
							{
								TempData["error"] = "Failed to create Tamara checkout: " + tamaraResponse.Message;
								return RedirectToAction("Summary");
							}
						}
						else
						{
							TempData["error"] = "Tamara payment is currently unavailable";
							return RedirectToAction("Summary");
						}
					}
					else
					{
						// Stripe payment logic (default)
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
						
						// Store payment method and Stripe info
						_unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
						var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == ShoppingCartVM.OrderHeader.Id);
						orderHeader.PaymentMethod = SD.PaymentMethodStripe;
						_unitOfWork.OrderHeader.Update(orderHeader);
						_unitOfWork.save();
						
						Response.Headers.Add("Location", session.Url);
						return new StatusCodeResult(303);
					}
				}

				return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
			}
			else
				TempData["success"] = "You Need to Add Items in the Cart";
			return RedirectToAction(nameof(Index),"Home");
		}


		// Tappy Payment Callback
		public async Task<IActionResult> TappyCallback(int orderId, string status = "")
		{
			var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId);
			
			if (orderHeader == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}

			// Verify payment with Tappy API
			if (!string.IsNullOrEmpty(orderHeader.SessionId))
			{
				var tappyHelper = new TappyHelper(_tappySettings);
				var verificationResponse = await tappyHelper.VerifyPaymentAsync(orderHeader.SessionId);
				
				if (verificationResponse.Success && verificationResponse.IsPaid)
				{
					// Payment successful
					_unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusPaid, SD.PaymentStatusPaid);
					orderHeader.PaymentDate = DateTime.Now;
					_unitOfWork.OrderHeader.Update(orderHeader);
					_unitOfWork.save();
					
					// ⚡ PROCESS STOCK DEDUCTION AFTER TAPPY PAYMENT
					await _stockService.ProcessOrderStockDeduction(orderId);

					return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
				}
				else
				{
					// Payment failed or pending
					TempData["error"] = "Payment verification failed. Please contact support with your order ID: " + orderId;
					return RedirectToAction("Index", "Home");
				}
			}
			else
			{
				TempData["error"] = "Invalid payment session";
				return RedirectToAction("Index", "Home");
			}
		}

		// Tamara Payment Callback
		public async Task<IActionResult> TamaraCallback(int orderId, string status = "")
		{
			var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId);
			
			if (orderHeader == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}

			if (status == "success" && !string.IsNullOrEmpty(orderHeader.SessionId))
			{
				// Authorize the order with Tamara
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				var authResponse = await tamaraHelper.AuthorizeOrderAsync(orderHeader.SessionId);
				
				if (authResponse.Success)
				{
					// Get order details to verify
					var orderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.SessionId);
					
					if (orderDetails.Success && orderDetails.PaymentStatus?.ToLower() == "approved")
					{
						// Payment successful
						_unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusPaid, SD.PaymentStatusPaid);
						orderHeader.PaymentDate = DateTime.Now;
						_unitOfWork.OrderHeader.Update(orderHeader);
						_unitOfWork.save();
						
						// ⚡ PROCESS STOCK DEDUCTION AFTER TAMARA PAYMENT
						await _stockService.ProcessOrderStockDeduction(orderId);
						
						return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
					}
				}
				
				// Payment not authorized
				TempData["error"] = "Payment authorization failed. Please contact support with your order ID: " + orderId;
				return RedirectToAction("Index", "Home");
			}
			else if (status == "failure")
			{
				TempData["error"] = "Payment was declined. Please try again with a different payment method.";
				return RedirectToAction("Summary");
			}
			else
			{
				TempData["info"] = "Payment was cancelled";
				return RedirectToAction("Index", "Cart");
			}
		}

		// Tamara Notification Webhook (for async notifications)
		[HttpPost]
		public async Task<IActionResult> TamaraNotification()
		{
			// Handle Tamara webhook notification
			// This is called by Tamara to notify about payment status changes
			return Ok();
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
				
				// Handle Stripe payment verification
				if (orderHeader.PaymentMethod == SD.PaymentMethodStripe)
				{
					var service = new SessionService();
					Session session = service.Get(orderHeader.SessionId);

					if (session.PaymentStatus.ToLower() == "paid")
					{
						_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
						_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusPaid, SD.PaymentStatusPaid);
						_unitOfWork.save();
					}
				}
				// Tappy and Tamara payments are already verified in their respective callbacks
				//HttpContext.Session.Clear();

			}

			// ⚡ PROCESS STOCK DEDUCTION AFTER PAYMENT CONFIRMED
			await _stockService.ProcessOrderStockDeduction(id);

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
