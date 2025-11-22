using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
		private readonly IStringLocalizer<SharedResources> _localizer;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, BulkyBook.Services.INotificationService notificationService, BulkyBook.Services.IStockService stockService, IOptions<TappySettings> tappySettings, IOptions<TamaraSettings> tamaraSettings, IStringLocalizer<SharedResources> localizer) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_notificationService = notificationService;
			_stockService = stockService;
			_tappySettings = tappySettings.Value;
			_tamaraSettings = tamaraSettings.Value;
			_localizer = localizer;
        }
        public IActionResult Index()
        {
            IEnumerable<ShoppingCart> cartList;

            if (User.Identity.IsAuthenticated)
            {
                // Authenticated user - load from database
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                // 🔥 Include FlashSaleItem and ProductImages to check for flash sale prices and display images
                cartList = _unitOfWork.shoppingCart.GetAll(a=>a.ApplicationUserId==UserId,
                    includeProperties: "product,FlashSaleItem,product.ProductImages");
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartList = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
                    FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry,ProductImages")
                }).ToList();
            }

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = cartList
            };

            foreach(var cart  in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetCartItemPrice(cart); // 🔥 Use new method that checks flash sale price
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
                // 🔥 Include FlashSaleItem to check for flash sale prices
                cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,product.ProductImages");
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartItems = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
                    FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry,ProductImages")
                }).ToList();
            }
            
            var items = cartItems.Select(cart => new
            {
                productId = cart.ProductId,
                title = cart.product.Title,
                imageUrl = GetProductImageUrl(cart.product), // 🔥 Use helper method to get correct image
                price = GetCartItemPrice(cart), // 🔥 Use new method that checks flash sale price
                count = cart.Count,
                cartId = cart.Id,
                isFlashSale = cart.FlashSaleItemId.HasValue // 🔥 Indicate if it's a flash sale item
            }).ToList();

            var subtotal = cartItems.Sum(cart => GetCartItemPrice(cart) * cart.Count);

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

                var cartItem = _unitOfWork.shoppingCart.Get(u => u.ApplicationUserId == userId && u.ProductId == productId, 
                    includeProperties: "product,FlashSaleItem");
                
                if (cartItem != null)
                {
                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(productId, cartItem.FlashSaleItemId, count);
                    
                    if (!validationResult.isValid)
                    {
                        return Json(new { success = false, message = validationResult.message });
                    }

                    cartItem.Count = count;
                    _unitOfWork.shoppingCart.update(cartItem);
                    _unitOfWork.save();
                    
                    return Json(new { success = true, message = "Quantity updated successfully!" });
                }
            }
            else
            {
                // Guest user - get flash sale info from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var guestItem = guestCart.FirstOrDefault(c => c.ProductId == productId);
                
                if (guestItem != null)
                {
                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(productId, guestItem.FlashSaleItemId, count);
                    
                    if (!validationResult.isValid)
                    {
                        return Json(new { success = false, message = validationResult.message });
                    }

                    BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, productId, count);
                    return Json(new { success = true, message = "Quantity updated successfully!" });
                }
            }

            return Json(new { success = false, message = "Item not found in cart" });
        }

        // Add Flash Sale Item to Cart
        [HttpPost]
        public IActionResult AddFlashSaleToCart(int productId, int flashSaleItemId, decimal flashSalePrice, int count = 1)
        {
            try
            {
                // Validate flash sale item
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(
                    f => f.Id == flashSaleItemId,
                    includeProperties: "FlashSale,Product");

                if (flashSaleItem == null)
                {
                    return Json(new { success = false, message = "Flash sale item not found" });
                }

                // Check if flash sale is active
                var now = DateTime.Now;
                if (!flashSaleItem.FlashSale.IsActive || 
                    now < flashSaleItem.FlashSale.StartDate || 
                    now > flashSaleItem.FlashSale.EndDate)
                {
                    return Json(new { success = false, message = "This flash sale is no longer active" });
                }

                // Check if item has stock
                if (flashSaleItem.FlashSaleQuantity <= 0)
                {
                    return Json(new { success = false, message = "This item is sold out" });
                }

                // Check if requested quantity is available
                if (count > flashSaleItem.FlashSaleQuantity)
                {
                    return Json(new { success = false, message = $"Only {flashSaleItem.FlashSaleQuantity} units available" });
                }

                if (User.Identity.IsAuthenticated)
                {
                    // Authenticated user - add to database
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                    // Check if item already in cart
                    var cartFromDb = _unitOfWork.shoppingCart.Get(
                        u => u.ApplicationUserId == userId && 
                             u.ProductId == productId && 
                             u.FlashSaleItemId == flashSaleItemId);

                    if (cartFromDb != null)
                    {
                        // Item exists, update quantity
                        var newCount = cartFromDb.Count + count;
                        if (newCount > flashSaleItem.FlashSaleQuantity)
                        {
                            return Json(new { success = false, message = $"Only {flashSaleItem.FlashSaleQuantity} units available" });
                        }
                        cartFromDb.Count = newCount;
                        _unitOfWork.shoppingCart.update(cartFromDb);
                    }
                    else
                    {
                        // Add new item
                        ShoppingCart cart = new()
                        {
                            ProductId = productId,
                            Count = count,
                            ApplicationUserId = userId,
                            FlashSaleItemId = flashSaleItemId,
                            FlashSalePrice = flashSalePrice
                        };
                        _unitOfWork.shoppingCart.Add(cart);
                    }

                    _unitOfWork.save();

                    // Get cart count
                    var cartCount = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId).Count();

                    return Json(new { 
                        success = true, 
                        message = "Flash sale item added to cart!",
                        cartCount = cartCount
                    });
                }
                else
                {
                    // Guest user - add to session
                    var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    
                    // Check if item already in cart (as flash sale)
                    var existingItem = guestCart.FirstOrDefault(c => c.ProductId == productId && c.FlashSaleItemId == flashSaleItemId);
                    
                    if (existingItem != null)
                    {
                        var newCount = existingItem.Count + count;
                        if (newCount > flashSaleItem.FlashSaleQuantity)
                        {
                            return Json(new { success = false, message = $"Only {flashSaleItem.FlashSaleQuantity} units available" });
                        }
                        existingItem.Count = newCount;
                    }
                    else
                    {
                        // Add new item
                        guestCart.Add(new GuestCartItem
                        {
                            ProductId = productId,
                            Count = count,
                            FlashSaleItemId = flashSaleItemId,
                            FlashSalePrice = (double)flashSalePrice,
                            ProductTitle = flashSaleItem.Product?.Title,
                            ProductPrice = flashSaleItem.Product?.Price ?? 0
                        });
                    }

                    BulkyBook.Utility.GuestCartHelper.SaveGuestCart(HttpContext.Session, guestCart);

                    return Json(new { 
                        success = true, 
                        message = "Flash sale item added to cart!",
                        cartCount = guestCart.Count
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        public IActionResult Pluse(int CartId, int? ProductId)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Load cart item with product and flash sale info
                var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId, includeProperties: "product,FlashSaleItem");
                var newQuantity = cartFromDD.Count + 1;

                // 🔥 Validate quantity limits
                var validationResult = ValidateQuantityUpdate(cartFromDD.ProductId, cartFromDD.FlashSaleItemId, newQuantity);
                
                if (!validationResult.isValid)
                {
                    TempData["error"] = validationResult.message;
                    return RedirectToAction(nameof(Index));
                }

                // Update quantity
                cartFromDD.Count = newQuantity;
                _unitOfWork.shoppingCart.update(cartFromDD);
                _unitOfWork.save();
            }
            else if (ProductId.HasValue)
            {
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                
                if (item != null)
                {
                    var newQuantity = item.Count + 1;

                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(item.ProductId, item.FlashSaleItemId, newQuantity);
                    
                    if (!validationResult.isValid)
                    {
                        TempData["error"] = validationResult.message;
                        return RedirectToAction(nameof(Index));
                    }

                    BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, newQuantity);
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
		// Validate and apply promo code
		[HttpPost]
		public IActionResult ValidatePromoCode(string promoCode)
		{
			if (string.IsNullOrWhiteSpace(promoCode))
			{
				return Json(new { success = false, message = _localizer["PleaseEnterPromoCode"].Value });
			}

			var promo = _unitOfWork.PromoCode.GetByCode(promoCode.Trim());
			
			if (promo == null)
			{
				return Json(new { success = false, message = _localizer["InvalidPromoCode"].Value });
			}

			var now = DateTime.Now;
			
			// Check if promo code is active
			if (!promo.IsActive)
			{
				return Json(new { success = false, message = _localizer["PromoCodeNoLongerActive"].Value });
			}

			// Check validity period
			if (now < promo.StartDate)
			{
				return Json(new { success = false, message = _localizer["PromoCodeNotYetValid"].Value });
			}

			if (now > promo.EndDate)
			{
				return Json(new { success = false, message = _localizer["PromoCodeExpired"].Value });
			}

			// Check usage limit
			if (promo.UsageLimit.HasValue && promo.TimesUsed >= promo.UsageLimit.Value)
			{
				return Json(new { success = false, message = _localizer["PromoCodeUsageLimitReached"].Value });
			}

			// Check per-user usage limit (only for authenticated users)
			if (User.Identity.IsAuthenticated && promo.UsageLimitPerUser.HasValue)
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				
				if (!_unitOfWork.PromoCode.CanUserUsePromoCode(promo.Id, userId))
				{
					return Json(new { success = false, message = _localizer["PromoCodeUserLimitReached"].Value });
				}
			}

			// Calculate cart subtotal to check minimum order amount
			IEnumerable<ShoppingCart> cartList;
			if (User.Identity.IsAuthenticated)
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem");
			}
			else
			{
				var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
				cartList = guestCart.Select(gc => new ShoppingCart
				{
					ProductId = gc.ProductId,
					Count = gc.Count,
					FlashSaleItemId = gc.FlashSaleItemId,
					FlashSalePrice = (decimal?)gc.FlashSalePrice,
					product = _unitOfWork.product.Get(p => p.Id == gc.ProductId)
				}).ToList();
			}

			double subtotal = 0;
			foreach (var cart in cartList)
			{
				cart.Price = GetCartItemPrice(cart);
				subtotal += (cart.Price * cart.Count);
			}

			// Check minimum order amount
			if (promo.MinimumOrderAmount.HasValue && (decimal)subtotal < promo.MinimumOrderAmount.Value)
			{
				var minAmountMessage = string.Format(_localizer["MinimumOrderAmountRequired"].Value, promo.MinimumOrderAmount.Value.ToString("C"));
				return Json(new 
				{ 
					success = false, 
					message = minAmountMessage
				});
			}

			// Calculate discount
			double discountAmount = 0;
			if (promo.DiscountType == BulkyBook.Models.DiscountType.Percentage)
			{
				discountAmount = subtotal * ((double)promo.DiscountValue / 100);
				
				// Apply maximum discount limit if set
				if (promo.MaximumDiscountAmount.HasValue && (decimal)discountAmount > promo.MaximumDiscountAmount.Value)
				{
					discountAmount = (double)promo.MaximumDiscountAmount.Value;
				}
			}
			else
			{
				discountAmount = (double)promo.DiscountValue;
			}

			// Ensure discount doesn't exceed subtotal
			if (discountAmount > subtotal)
			{
				discountAmount = subtotal;
			}

			double finalTotal = subtotal - discountAmount;

			return Json(new 
			{ 
				success = true, 
				message = _localizer["PromoCodeAppliedSuccessfully"].Value,
				promoCodeId = promo.Id,
				promoCode = promo.Code,
				discountAmount = discountAmount,
				subtotal = subtotal,
				finalTotal = finalTotal,
				discountText = promo.DiscountType == BulkyBook.Models.DiscountType.Percentage 
					? $"{promo.DiscountValue}% off" 
					: $"{promo.DiscountValue:C} off"
			});
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

			// 🔥 Include FlashSaleItem to check for flash sale prices
			cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "product,FlashSaleItem");

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
				FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
				FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
				product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry")
			}).ToList();

			// Initialize empty fields for guest
			ShoppingCartVM.OrderHeader.IsGuestOrder = true;
		}

		ShoppingCartVM.ShoppingCartList = cartList;

		foreach (var cart in ShoppingCartVM.ShoppingCartList)
		{
			cart.Price = GetCartItemPrice(cart); // 🔥 Use new method that checks flash sale price
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
				// 🔥 Include FlashSaleItem to check for flash sale prices
				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product,FlashSaleItem");
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
					FlashSaleItemId = gc.FlashSaleItemId,
					FlashSalePrice= (decimal?)gc.FlashSalePrice,
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

				// Calculate subtotal - 🔥 Use GetCartItemPrice to include flash sale prices
				double subtotal = 0;
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetCartItemPrice(cart); // 🔥 Fixed: Use GetCartItemPrice instead of GetPriceBasedOnQuantity
					subtotal += (cart.Price * cart.Count);
				}

				ShoppingCartVM.OrderHeader.OrderSubtotal = subtotal;
				ShoppingCartVM.OrderHeader.OrderTotal = subtotal;

				// Apply promo code if provided
				if (ShoppingCartVM.OrderHeader.PromoCodeId.HasValue && ShoppingCartVM.OrderHeader.PromoCodeId.Value > 0)
				{
					var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == ShoppingCartVM.OrderHeader.PromoCodeId.Value);
					
					if (promoCode != null && promoCode.IsActive)
					{
						var now = DateTime.Now;
						
						// Validate promo code is still valid
						if (now >= promoCode.StartDate && now <= promoCode.EndDate)
						{
							// Check usage limits
							bool canUse = true;
							
							if (promoCode.UsageLimit.HasValue && promoCode.TimesUsed >= promoCode.UsageLimit.Value)
							{
								canUse = false;
							}
							
							if (canUse && !isGuest && promoCode.UsageLimitPerUser.HasValue)
							{
								canUse = _unitOfWork.PromoCode.CanUserUsePromoCode(promoCode.Id, userId);
							}
							
							if (canUse)
							{
								// Calculate discount
								double discountAmount = 0;
								
								if (promoCode.DiscountType == BulkyBook.Models.DiscountType.Percentage)
								{
									discountAmount = subtotal * ((double)promoCode.DiscountValue / 100);
									
									if (promoCode.MaximumDiscountAmount.HasValue && (decimal)discountAmount > promoCode.MaximumDiscountAmount.Value)
									{
										discountAmount = (double)promoCode.MaximumDiscountAmount.Value;
									}
								}
								else
								{
									discountAmount = (double)promoCode.DiscountValue;
								}
								
								if (discountAmount > subtotal)
								{
									discountAmount = subtotal;
								}
								
								ShoppingCartVM.OrderHeader.DiscountAmount = discountAmount;
								ShoppingCartVM.OrderHeader.PromoCodeText = promoCode.Code;
								ShoppingCartVM.OrderHeader.OrderTotal = subtotal - discountAmount;
							}
						}
					}
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
				
				// Record promo code usage if applied
				if (ShoppingCartVM.OrderHeader.PromoCodeId.HasValue && !isGuest)
				{
					_unitOfWork.PromoCodeUsage.RecordUsage(
						ShoppingCartVM.OrderHeader.PromoCodeId.Value, 
						userId, 
						ShoppingCartVM.OrderHeader.Id);
					_unitOfWork.PromoCode.IncrementUsage(ShoppingCartVM.OrderHeader.PromoCodeId.Value);
					_unitOfWork.save();
				}
				
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					OrderDetail orderDetail = new()
					{
						ProductId = cart.ProductId,
						OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
						Price = cart.Price,
						Count = cart.Count,
						FlashSaleItemId = cart.FlashSaleItemId // Copy flash sale item ID if exists
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

						// Calculate the total from line items (subtotal before discount)
						double lineItemsSubtotal = 0;
						foreach (var item in ShoppingCartVM.ShoppingCartList)
						{
							lineItemsSubtotal += (item.Price * item.Count);
						}

						// Calculate discount ratio if promo code was applied
						// This ensures line items total matches the discounted OrderTotal
						double discountRatio = 1.0;
						if (ShoppingCartVM.OrderHeader.DiscountAmount > 0 && lineItemsSubtotal > 0)
						{
							discountRatio = (double)ShoppingCartVM.OrderHeader.OrderTotal / lineItemsSubtotal;
						}

						// Add line items with adjusted prices to match the discounted total
						foreach (var item in ShoppingCartVM.ShoppingCartList)
						{
							// Calculate adjusted price per unit to account for discount proportionally
							double adjustedUnitPrice = item.Price * discountRatio;
							
							var sessionLineItem = new SessionLineItemOptions
							{
								PriceData = new SessionLineItemPriceDataOptions
								{
									UnitAmount = (long)Math.Round(adjustedUnitPrice * 100), // Convert to cents, ensure non-negative
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

        // 🔥 NEW: Get cart item price - uses flash sale price if available, otherwise quantity-based pricing
        private double GetCartItemPrice(ShoppingCart shoppingCart)
        {
            // If this item is from a flash sale, use the flash sale price
            if (shoppingCart.FlashSaleItemId.HasValue && shoppingCart.FlashSalePrice.HasValue)
            {
                return (double)shoppingCart.FlashSalePrice.Value;
            }

            // Otherwise, use the regular quantity-based pricing
            return GetPriceBasedOnQty(shoppingCart);
        }

        // 🔥 NEW: Validate quantity update against product stock and flash sale limits
        private (bool isValid, string message) ValidateQuantityUpdate(int productId, int? flashSaleItemId, int requestedQuantity)
        {
            if (requestedQuantity <= 0)
            {
                return (false, "Quantity must be at least 1");
            }

            // Get product information
            var product = _unitOfWork.product.Get(p => p.Id == productId);
            
            if (product == null)
            {
                return (false, "Product not found");
            }

            // If it's a flash sale item, check flash sale quantity first
            if (flashSaleItemId.HasValue)
            {
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(f => f.Id == flashSaleItemId.Value, includeProperties: "FlashSale");
                
                if (flashSaleItem != null)
                {
                    // Check if flash sale is still active
                    var now = DateTime.Now;
                    if (!flashSaleItem.FlashSale.IsActive || 
                        now < flashSaleItem.FlashSale.StartDate || 
                        now > flashSaleItem.FlashSale.EndDate)
                    {
                        return (false, "This flash sale has ended");
                    }

                    // Check flash sale quantity limit
                    if (requestedQuantity > flashSaleItem.FlashSaleQuantity)
                    {
                        if (flashSaleItem.FlashSaleQuantity == 0)
                        {
                            return (false, "Flash sale item is sold out");
                        }
                        return (false, $"Only {flashSaleItem.FlashSaleQuantity} units available for this flash sale");
                    }
                }
            }

            // Check product stock quantity
            if (requestedQuantity > product.StockQuantity)
            {
                if (product.StockQuantity == 0)
                {
                    return (false, "This product is out of stock");
                }
                return (false, $"Only {product.StockQuantity} units available in stock");
            }

            return (true, "Valid");
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

        // 🔥 Helper method to get product image URL (checks ProductImages first, then falls back to ImageUrl)
        private string GetProductImageUrl(Product product)
        {
            if (product == null)
            {
                return "/images/no-image.png"; // Default placeholder image
            }

            // Check if product has ProductImages
            if (product.ProductImages != null && product.ProductImages.Any())
            {
                // Get the first image ordered by DisplayOrder
                var firstImage = product.ProductImages.OrderBy(pi => pi.DisplayOrder).FirstOrDefault();
                if (firstImage != null && !string.IsNullOrEmpty(firstImage.ImageUrl))
                {
                    return firstImage.ImageUrl;
                }
            }

            // Fallback to ImageUrl if no ProductImages
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                return product.ImageUrl;
            }

            // Return placeholder if no image found
            return "/images/no-image.png";
        }
    }

}
