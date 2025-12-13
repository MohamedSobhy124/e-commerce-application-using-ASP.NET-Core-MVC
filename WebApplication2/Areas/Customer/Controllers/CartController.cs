using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
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
		private readonly GeideaSettings _geideaSettings;
		private readonly IStringLocalizer<SharedResources> _localizer;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, BulkyBook.Services.INotificationService notificationService, BulkyBook.Services.IStockService stockService, IOptions<TappySettings> tappySettings, IOptions<TamaraSettings> tamaraSettings, IOptions<GeideaSettings> geideaSettings, IStringLocalizer<SharedResources> localizer) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_notificationService = notificationService;
			_stockService = stockService;
			_tappySettings = tappySettings.Value;
			_tamaraSettings = tamaraSettings.Value;
			_geideaSettings = geideaSettings.Value;
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
                // 🔥 Include FlashSaleItem, ProductVariant, ComboOffer, and ProductImages to check for flash sale prices, variant prices, combo offers, and display images
                cartList = _unitOfWork.shoppingCart.GetAll(a=>a.ApplicationUserId==UserId,
                    includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer,product.ProductImages").ToList();
                
                // Load variant option values for each cart item and filter out images with ImageInfo
                foreach (var cart in cartList)
                {
                    // Filter out images with ImageInfo
                    if (cart.product != null && cart.product.ProductImages != null)
                    {
                        cart.product.ProductImages = cart.product.ProductImages.Where(_ => _.ImageInfo == null).ToList();
                    }
                    
                    if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
                    {
                        // Ensure variant option values are loaded
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
                            includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
                        if (variant != null)
                        {
                            cart.ProductVariant = variant;
                        }
                    }
                }
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartList = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    ProductVariantId = gc.ProductVariantId, // 🔥 Include variant info
                    FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
                    FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
                    ComboOfferId = gc.ComboOfferId, // 🔥 Include combo offer info
                    ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null, // 🔥 Load combo offer
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry,ProductImages"),
                    ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value, includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption") : null
                }).ToList();
                
                // Filter out images with ImageInfo for guest cart items
                foreach (var cart in cartList)
                {
                    if (cart.product != null && cart.product.ProductImages != null)
                    {
                        cart.product.ProductImages = cart.product.ProductImages.Where(_ => _.ImageInfo == null).ToList();
                    }
                }
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
                // 🔥 Include FlashSaleItem, ProductVariant to check for flash sale prices and variant prices
                cartItems = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ComboOffer,ProductVariant,product.ProductImages");
                
                // Load variant option values for each cart item
                foreach (var cart in cartItems)
                {
                    cart.product.ProductImages=cart.product.ProductImages.Where(_ => _.ImageInfo is null).ToList();
                    if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
                    {
                        // Ensure variant option values are loaded
                        var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
                            includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
                        if (variant != null)
                        {
                            cart.ProductVariant = variant;
                        }
                    }
                }
            }
            else
            {
                // Guest user - load from session
                var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartItems = guestCart.Select(gc => new ShoppingCart
                {
                    ProductId = gc.ProductId,
                    Count = gc.Count,
                    ProductVariantId = gc.ProductVariantId, // 🔥 Include variant info
                    FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
                    FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
                    ComboOfferId = gc.ComboOfferId, // 🔥 Include combo offer info
                    ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null, // 🔥 Load combo offer
                    product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry,ProductImages"),
                    ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value, includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption") : null
                }).ToList();
                
                // Filter out images with ImageInfo for guest cart items
                foreach (var cart in cartItems)
                {
                    if (cart.product != null && cart.product.ProductImages != null)
                    {
                        cart.product.ProductImages = cart.product.ProductImages.Where(_ => _.ImageInfo == null).ToList();
                    }
                }
            }
            
            var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
            
            var items = cartItems.Select(cart => {
                // Check if this is a combo offer
                bool isComboOffer = cart.ComboOfferId.HasValue && cart.ComboOffer != null;
                
                // Get display name - use combo name if it's a combo, otherwise use product name
                string displayTitle = isComboOffer 
                    ? (currentCulture == "ar" && !string.IsNullOrEmpty(cart.ComboOffer.NameAr) 
                        ? cart.ComboOffer.NameAr 
                        : cart.ComboOffer.Name)
                    : (cart.product != null 
                        ? (currentCulture == "ar" && !string.IsNullOrEmpty(cart.product.TitleAr) 
                            ? cart.product.TitleAr 
                            : cart.product.Title ?? "Product")
                        : "Product");
                
                // Get image - use combo image if it's a combo, otherwise use product image
                string imageUrl = isComboOffer && !string.IsNullOrEmpty(cart.ComboOffer.ImageUrl)
                    ? cart.ComboOffer.ImageUrl
                    : GetProductImageUrl(cart.product);
                
                // Build variant name if variant exists (localized based on current culture)
                string variantName = "";
                if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
                {
                    if (cart.ProductVariant.VariantOptionValues != null && cart.ProductVariant.VariantOptionValues.Any())
                    {
                        var optionValues = cart.ProductVariant.VariantOptionValues
                            .OrderBy(vov => vov.OptionValue?.ProductOption?.DisplayOrder ?? 0)
                            .ThenBy(vov => vov.OptionValue?.DisplayOrder ?? 0)
                            .Select(vov => {
                                var optionName = (currentCulture == "ar" && !string.IsNullOrEmpty(vov.OptionValue?.ProductOption?.NameAr)) 
                                    ? vov.OptionValue.ProductOption.NameAr 
                                    : vov.OptionValue?.ProductOption?.Name;
                                
                                var optionValue = (currentCulture == "ar" && !string.IsNullOrEmpty(vov.OptionValue?.ValueAr)) 
                                    ? vov.OptionValue.ValueAr 
                                    : vov.OptionValue?.Value;
                                
                                return $"{optionName}: {optionValue}";
                            })
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                        
                        if (optionValues.Any())
                        {
                            variantName = string.Join(" / ", optionValues);
                        }
                    }
                    else if (!string.IsNullOrEmpty(cart.ProductVariant.VariantName))
                    {
                        variantName = cart.ProductVariant.VariantName;
                    }
                }
                
                return new
                {
                    productId = cart.ProductId,
                    title = displayTitle,
                    imageUrl = imageUrl,
                    price = GetCartItemPrice(cart), // 🔥 Use new method that checks flash sale price
                    count = cart.Count,
                    cartId = cart.Id,
                    isFlashSale = cart.FlashSaleItemId.HasValue, // 🔥 Indicate if it's a flash sale item
                    isComboOffer = isComboOffer, // 🔥 Indicate if it's a combo offer
                    variantName = variantName // 🔥 Include variant name if exists
                };
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
                    
                    return Json(new { success = true, message = _localizer["QuantityUpdatedSuccessfully"].Value });
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
                    return Json(new { success = true, message = _localizer["QuantityUpdatedSuccessfully"].Value });
                }
            }

            return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
        }

        // Add Flash Sale Item to Cart
        [HttpPost]
        public IActionResult AddFlashSaleToCart(int productId, int flashSaleItemId, decimal flashSalePrice, int count = 1, int? productVariantId = null)
        {
            try
            {
                // Validate flash sale item
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(
                    f => f.Id == flashSaleItemId,
                    includeProperties: "FlashSale,Product");

                if (flashSaleItem == null)
                {
                    return Json(new { success = false, message = _localizer["FlashSaleItemNotFound"].Value });
                }

                // Check if flash sale is active
                var now = BulkyBook.Utility.DateTimeHelper.Now;
                if (!flashSaleItem.FlashSale.IsActive || 
                    now < flashSaleItem.FlashSale.StartDate || 
                    now > flashSaleItem.FlashSale.EndDate)
                {
                    return Json(new { success = false, message = _localizer["FlashSaleNoLongerActive"].Value });
                }

                // Check if item has stock
                if (flashSaleItem.FlashSaleQuantity <= 0)
                {
                    return Json(new { success = false, message = _localizer["FlashSaleItemSoldOut"].Value });
                }

                // Check if requested quantity is available
                if (count > flashSaleItem.FlashSaleQuantity)
                {
                    return Json(new { success = false, message = string.Format(_localizer["OnlyUnitsAvailable"].Value, flashSaleItem.FlashSaleQuantity) });
                }

                if (User.Identity.IsAuthenticated)
                {
                    // Authenticated user - add to database
                    var claimsIdentity = (ClaimsIdentity)User.Identity;
                    var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                    // Check if item already in cart (considering variant)
                    var cartFromDb = _unitOfWork.shoppingCart.Get(
                        u => u.ApplicationUserId == userId && 
                             u.ProductId == productId && 
                             u.FlashSaleItemId == flashSaleItemId &&
                             u.ProductVariantId == productVariantId);

                    if (cartFromDb != null)
                    {
                        // Item exists, update quantity
                        var newCount = cartFromDb.Count + count;
                        if (newCount > flashSaleItem.FlashSaleQuantity)
                        {
                            return Json(new { success = false, message = string.Format(_localizer["OnlyUnitsAvailable"].Value, flashSaleItem.FlashSaleQuantity) });
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
                            ProductVariantId = productVariantId,
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
                        message = _localizer["FlashSaleItemAddedToCart"].Value,
                        cartCount = cartCount
                    });
                }
                else
                {
                    // Guest user - add to session
                    var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    
                    // Check if item already in cart (as flash sale, considering variant)
                    var existingItem = guestCart.FirstOrDefault(c => c.ProductId == productId && 
                                                                     c.FlashSaleItemId == flashSaleItemId &&
                                                                     c.ProductVariantId == productVariantId);
                    
                    if (existingItem != null)
                    {
                        var newCount = existingItem.Count + count;
                        if (newCount > flashSaleItem.FlashSaleQuantity)
                        {
                            return Json(new { success = false, message = string.Format(_localizer["OnlyUnitsAvailable"].Value, flashSaleItem.FlashSaleQuantity) });
                        }
                        existingItem.Count = newCount;
                    }
                    else
                    {
                        // Add new item
                        guestCart.Add(new GuestCartItem
                        {
                            ProductId = productId,
                            ProductVariantId = productVariantId,
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
                        message = _localizer["FlashSaleItemAddedToCart"].Value,
                        cartCount = guestCart.Count
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = string.Format(_localizer["AnErrorOccurredWithDetails"].Value, ex.Message) });
            }
        }

        [HttpPost]
        public IActionResult Pluse(int CartId, int? ProductId)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    // Load cart item with product and flash sale info
                    var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    if (cartFromDD == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    var newQuantity = cartFromDD.Count + 1;

                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(cartFromDD.ProductId, cartFromDD.FlashSaleItemId, newQuantity);
                    
                    if (!validationResult.isValid)
                    {
                        return Json(new { success = false, message = validationResult.message });
                    }

                    // Update quantity
                    cartFromDD.Count = newQuantity;
                    _unitOfWork.shoppingCart.update(cartFromDD);
                    _unitOfWork.save();

                    // Calculate updated prices
                    cartFromDD.Price = GetCartItemPrice(cartFromDD);
                    var unitPrice = cartFromDD.Price;
                    var totalPrice = unitPrice * cartFromDD.Count;
                    decimal? originalPrice = (decimal)(cartFromDD.FlashSaleItemId.HasValue && cartFromDD.product.Price > unitPrice ? cartFromDD.product.Price : 0);

                    // Calculate order total
                    var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                    var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    var orderTotal = allCartItems.Sum(c => GetCartItemPrice(c) * c.Count);

                    return Json(new { 
                        success = true, 
                        count = cartFromDD.Count,
                        unitPrice = unitPrice,
                        totalPrice = totalPrice,
                        originalPrice = originalPrice,
                        orderTotal = orderTotal,
                        message = _localizer["QuantityUpdated"].Value
                    });
                }
                else if (ProductId.HasValue)
                {
                    var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                    
                    if (item == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    var newQuantity = item.Count + 1;

                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(item.ProductId, item.FlashSaleItemId, newQuantity);
                    
                    if (!validationResult.isValid)
                    {
                        return Json(new { success = false, message = validationResult.message });
                    }

                    BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, newQuantity);
                    
                    // Recalculate prices
                    var updatedCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var updatedItem = updatedCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                    var orderTotal = updatedCart.Sum(c => (c.FlashSalePrice ?? c.ProductPrice) * c.Count);

                    return Json(new { 
                        success = true, 
                        count = updatedItem.Count,
                        unitPrice = updatedItem.FlashSalePrice ?? updatedItem.ProductPrice,
                        totalPrice = (updatedItem.FlashSalePrice ?? updatedItem.ProductPrice) * updatedItem.Count,
                        orderTotal = orderTotal,
                        message = _localizer["QuantityUpdated"].Value
                    });
                }
                
                return Json(new { success = false, message = _localizer["InvalidRequest"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        
        [HttpPost]
        public IActionResult Minus(int CartId, int? ProductId)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    if (cartFromDD == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    bool removed = false;
                    if (cartFromDD.Count <= 1)
                    {
                        _unitOfWork.shoppingCart.remove(cartFromDD);
                        removed = true;
                    }
                    else
                    {
                        cartFromDD.Count -= 1;
                        _unitOfWork.shoppingCart.update(cartFromDD);
                    }
                    _unitOfWork.save();

                    if (removed)
                    {
                        // Calculate order total after removal
                        var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                        var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                        var orderTotal = allCartItems.Sum(c => GetCartItemPrice(c) * c.Count);

                        return Json(new { 
                            success = true, 
                            removed = true,
                            orderTotal = orderTotal,
                            message = _localizer["ItemRemovedFromCart"].Value
                        });
                    }
                    else
                    {
                        // Calculate updated prices
                        cartFromDD.Price = GetCartItemPrice(cartFromDD);
                        var unitPrice = cartFromDD.Price;
                        var totalPrice = unitPrice * cartFromDD.Count;
                        decimal? originalPrice = (decimal)(cartFromDD.FlashSaleItemId.HasValue && cartFromDD.product.Price > unitPrice ? cartFromDD.product.Price : 0);

                        // Calculate order total
                        var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                        var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                        var orderTotal = allCartItems.Sum(c => GetCartItemPrice(c) * c.Count);

                        return Json(new { 
                            success = true, 
                            removed = false,
                            count = cartFromDD.Count,
                            unitPrice = unitPrice,
                            totalPrice = totalPrice,
                            originalPrice = originalPrice,
                            orderTotal = orderTotal,
                            message = _localizer["QuantityUpdated"].Value
                        });
                    }
                }
                else if (ProductId.HasValue)
                {
                    var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                    if (item == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    bool removed = false;
                    if (item.Count <= 1)
                    {
                        BulkyBook.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
                        removed = true;
                    }
                    else
                    {
                        BulkyBook.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, item.Count - 1);
                    }

                    if (removed)
                    {
                        var updatedCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                        var orderTotal = updatedCart.Sum(c => (c.FlashSalePrice ?? c.ProductPrice) * c.Count);

                        return Json(new { 
                            success = true, 
                            removed = true,
                            orderTotal = orderTotal,
                            message = _localizer["ItemRemovedFromCart"].Value
                        });
                    }
                    else
                    {
                        var updatedCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                        var updatedItem = updatedCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                        var orderTotal = updatedCart.Sum(c => (c.FlashSalePrice ?? c.ProductPrice) * c.Count);

                        return Json(new { 
                            success = true, 
                            removed = false,
                            count = updatedItem.Count,
                            unitPrice = updatedItem.FlashSalePrice ?? updatedItem.ProductPrice,
                            totalPrice = (updatedItem.FlashSalePrice ?? updatedItem.ProductPrice) * updatedItem.Count,
                            orderTotal = orderTotal,
                            message = _localizer["QuantityUpdated"].Value
                        });
                    }
                }

                return Json(new { success = false, message = _localizer["InvalidRequest"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        
        [HttpPost]
        public IActionResult Remove(int CartId, int? ProductId)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId);
                    if (cartFromDD == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    _unitOfWork.shoppingCart.remove(cartFromDD);
                    _unitOfWork.save();

                    // Calculate order total after removal
                    var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                    var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    var orderTotal = allCartItems.Sum(c => GetCartItemPrice(c) * c.Count);

                    return Json(new { 
                        success = true, 
                        orderTotal = orderTotal,
                        message = "Item removed from cart"
                    });
                }
                else if (ProductId.HasValue)
                {
                    BulkyBook.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
                    
                    var updatedCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var orderTotal = updatedCart.Sum(c => (c.FlashSalePrice ?? c.ProductPrice) * c.Count);

                    return Json(new { 
                        success = true, 
                        orderTotal = orderTotal,
                        message = "Item removed from cart"
                    });
                }

                return Json(new { success = false, message = _localizer["InvalidRequest"].Value });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
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

			var now = BulkyBook.Utility.DateTimeHelper.Now;
			
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

			// Load excluded products and combo offers for the promo code
			var promoWithExclusions = _unitOfWork.PromoCode.Get(p => p.Id == promo.Id, includeProperties: "ExcludedProducts,ExcludedComboOffers");
			if (promoWithExclusions != null)
			{
				promo.ExcludedProducts = promoWithExclusions.ExcludedProducts;
				promo.ExcludedComboOffers = promoWithExclusions.ExcludedComboOffers;
			}

			// Calculate cart subtotal to check minimum order amount
			IEnumerable<ShoppingCart> cartList;
			if (User.Identity.IsAuthenticated)
			{
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
			}
			else
			{
				var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
			cartList = guestCart.Select(gc => new ShoppingCart
			{
				Id = 0, // Guest cart items don't have IDs, use 0 as placeholder
				ProductId = gc.ProductId,
				Count = gc.Count,
				ProductVariantId = gc.ProductVariantId,
				FlashSaleItemId = gc.FlashSaleItemId,
				FlashSalePrice = (decimal?)gc.FlashSalePrice,
				ComboOfferId = gc.ComboOfferId, // 🔥 Include combo offer info
				ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null, // 🔥 Load combo offer
				product = _unitOfWork.product.Get(p => p.Id == gc.ProductId),
				ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) : null
			}).ToList();
			}

			// Calculate total subtotal for display
			double subtotal = 0;
			foreach (var cart in cartList)
			{
				cart.Price = GetCartItemPrice(cart);
				subtotal += (cart.Price * cart.Count);
			}

			// Calculate item-level discounts using the new helper method
			var (totalDiscount, itemDiscounts) = CalculatePromoCodeDiscounts(cartList, promo);

			// Calculate eligible subtotal and count eligible items
			double eligibleSubtotal = 0;
			int eligibleItemCount = 0;
			var eligibleItemsInfo = new List<object>();

			foreach (var cart in cartList)
			{
				cart.Price = GetCartItemPrice(cart);
				bool isEligible = IsItemEligibleForPromoCode(cart, promo);
				
				if (isEligible)
				{
					eligibleSubtotal += (cart.Price * cart.Count);
					eligibleItemCount++;
					
					// Create item key to check if it has discount
					string itemKey = $"{cart.ProductId}_{cart.ProductVariantId?.ToString() ?? "0"}_{cart.FlashSaleItemId?.ToString() ?? "0"}_{cart.ComboOfferId?.ToString() ?? "0"}";
					double itemDiscount = itemDiscounts.ContainsKey(itemKey) ? itemDiscounts[itemKey] : 0;
					
					eligibleItemsInfo.Add(new
					{
						productId = cart.ProductId,
						productVariantId = cart.ProductVariantId,
						productTitle = cart.product?.Title ?? "Unknown",
						itemKey = itemKey,
						discountAmount = itemDiscount,
						originalPrice = cart.Price * cart.Count,
						discountedPrice = (cart.Price * cart.Count) - itemDiscount
					});
				}
			}

			// Check if there are any eligible items
			if (eligibleItemCount == 0)
			{
				return Json(new 
				{ 
					success = false, 
					message = _localizer["NoEligibleProductsForPromoCode"].Value
				});
			}

			// Check minimum order amount on eligible items
			if (promo.MinimumOrderAmount.HasValue)
			{
				if ((decimal)eligibleSubtotal < promo.MinimumOrderAmount.Value)
				{
					var minAmountMessage = string.Format(
						_localizer["MinimumOrderAmountRequiredForEligibleItems"].Value,
						promo.MinimumOrderAmount.Value.ToString("C"),
						eligibleSubtotal.ToString("C")
					);
					return Json(new 
					{ 
						success = false, 
						message = minAmountMessage
					});
				}
			}

			// If discount is 0 but we have eligible items, there might be an issue
			if (totalDiscount == 0 && eligibleItemCount > 0)
			{
				return Json(new 
				{ 
					success = false, 
					message = _localizer["PromoCodeCouldNotBeAppliedToEligibleItems"].Value
				});
			}

			// Ensure discount doesn't exceed subtotal
			if (totalDiscount > subtotal)
			{
				totalDiscount = subtotal;
			}

			double finalTotal = subtotal - totalDiscount;

			return Json(new 
			{ 
				success = true, 
				message = _localizer["PromoCodeAppliedSuccessfully"].Value,
				promoCodeId = promo.Id,
				promoCode = promo.Code,
				discountAmount = totalDiscount,
				subtotal = subtotal,
				finalTotal = finalTotal,
				discountText = promo.DiscountType == BulkyBook.Models.DiscountType.Percentage 
					? $"{promo.DiscountValue}% off" 
					: $"{promo.DiscountValue:C} off",
				itemDiscounts = itemDiscounts, // Include item-level discounts for reference
				eligibleItems = eligibleItemsInfo, // Include eligible items with discount info
				eligibleItemCount = eligibleItemCount
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

			// 🔥 Include FlashSaleItem, ProductVariant, ComboOffer to check for flash sale prices, variant prices, combo offers
			cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");

			// Load variant option values for each cart item
			foreach (var cart in cartList)
			{
				if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
				{
					// Ensure variant option values are loaded
					var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
						includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
					if (variant != null)
					{
						cart.ProductVariant = variant;
					}
				}
			}

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
				ProductVariantId = gc.ProductVariantId,
				FlashSaleItemId = gc.FlashSaleItemId, // 🔥 Include flash sale info
				FlashSalePrice = (decimal?)gc.FlashSalePrice, // 🔥 Include flash sale price
				ComboOfferId = gc.ComboOfferId, // 🔥 Include combo offer info
				ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null, // 🔥 Load combo offer
				product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry"),
				ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value, includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption") : null
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
            var userId = string.Empty;
            
			// Validate required fields manually (primary check)
			bool hasValidationErrors = false;
			var validationErrors = new List<string>();

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.Name))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["Name"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.PhoneNumber))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["Phone"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.StreetAddress))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["StreetAddress"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.City))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["City"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.State))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["State"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.PostalCode))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["PostalCode"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
			}

			// Validate email for guest orders
			if (ShoppingCartVM.OrderHeader.IsGuestOrder || !User.Identity.IsAuthenticated)
			{
				if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.Email))
				{
					hasValidationErrors = true;
					validationErrors.Add(_localizer["EmailAddress"].Value + " " + (_localizer["IsRequired"].Value ?? "is required"));
				}
				else if (!System.Text.RegularExpressions.Regex.IsMatch(ShoppingCartVM.OrderHeader.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
				{
					hasValidationErrors = true;
					validationErrors.Add(_localizer["InvalidEmailFormat"].Value ?? _localizer["PleaseEnterValidEmailAddress"].Value ?? "Please enter a valid email address");
				}
			}

			// Validate payment method
			if (string.IsNullOrWhiteSpace(ShoppingCartVM.OrderHeader.PaymentMethod))
			{
				hasValidationErrors = true;
				validationErrors.Add(_localizer["PleaseSelectPaymentMethod"].Value ?? "Please select a payment method");
			}

			if (hasValidationErrors)
			{
				// Reload cart data for display
				IEnumerable<ShoppingCart> cartListForDisplay;

				if (User.Identity.IsAuthenticated)
				{
					var claimsIdentity = (ClaimsIdentity)User.Identity;
					  userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
					cartListForDisplay = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
						includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
					
					foreach (var cart in cartListForDisplay)
					{
						if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
						{
							var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
								includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
							if (variant != null)
							{
								cart.ProductVariant = variant;
							}
						}
					}
					
					ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
				}
				else
				{
					var guestCart = BulkyBook.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
					cartListForDisplay = guestCart.Select(gc => new ShoppingCart
					{
						ProductId = gc.ProductId,
						Count = gc.Count,
						ProductVariantId = gc.ProductVariantId,
						FlashSaleItemId = gc.FlashSaleItemId,
						FlashSalePrice = (decimal?)gc.FlashSalePrice,
						ComboOfferId = gc.ComboOfferId,
						ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null,
						product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry"),
						ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value, includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption") : null
					}).ToList();
					ShoppingCartVM.OrderHeader.IsGuestOrder = true;
				}

				ShoppingCartVM.ShoppingCartList = cartListForDisplay;
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					cart.Price = GetCartItemPrice(cart);
					ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
				}

				TempData["error"] = string.Join(", ", validationErrors);
				return View(ShoppingCartVM);
			}

			IEnumerable<ShoppingCart> cartList;
			bool isGuest = !User.Identity.IsAuthenticated;
			ApplicationUser applicationUser = null;

			if (!isGuest)
			{
				// Authenticated user
				var claimsIdentity = (ClaimsIdentity)User.Identity;
				userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
				// 🔥 Include FlashSaleItem and ComboOffer to check for flash sale and combo prices
				cartList = _unitOfWork.shoppingCart.GetAll(u => u.ApplicationUserId == userId,
					includeProperties: "product,FlashSaleItem,ComboOffer");
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
					ProductVariantId = gc.ProductVariantId,
					FlashSaleItemId = gc.FlashSaleItemId,
					FlashSalePrice= (decimal?)gc.FlashSalePrice,
					ComboOfferId = gc.ComboOfferId, // 🔥 Include combo offer info
					ComboOffer = gc.ComboOfferId.HasValue ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) : null, // 🔥 Load combo offer
					product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry"),
					ProductVariant = gc.ProductVariantId.HasValue ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) : null
				}).ToList();
			}

			ShoppingCartVM.ShoppingCartList = cartList;

			if (ShoppingCartVM.ShoppingCartList.Count() > 0)
			{
				ShoppingCartVM.OrderHeader.OrderDate = BulkyBook.Utility.DateTimeHelper.Now;
				
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
					var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == ShoppingCartVM.OrderHeader.PromoCodeId.Value, includeProperties: "ExcludedProducts,ExcludedComboOffers");
					
					if (promoCode != null && promoCode.IsActive)
					{
						var now = BulkyBook.Utility.DateTimeHelper.Now;
						
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
								// Calculate item-level discounts using the new helper method
								var (totalDiscount, itemDiscounts) = CalculatePromoCodeDiscounts(ShoppingCartVM.ShoppingCartList, promoCode);
								
								// Store item discounts in TempData so we can access when creating OrderDetail
								TempData["PromoCodeItemDiscounts"] = System.Text.Json.JsonSerializer.Serialize(itemDiscounts);
								
								if (totalDiscount > subtotal)
								{
									totalDiscount = subtotal;
								}
								
								ShoppingCartVM.OrderHeader.DiscountAmount = totalDiscount;
								ShoppingCartVM.OrderHeader.PromoCodeText = promoCode.Code;
								ShoppingCartVM.OrderHeader.OrderTotal = subtotal - totalDiscount;
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
				
				// Get item-level promo code discounts if available
				Dictionary<string, double> promoItemDiscounts = new Dictionary<string, double>();
				if (TempData["PromoCodeItemDiscounts"] != null)
				{
					try
					{
						var discountsJson = TempData["PromoCodeItemDiscounts"].ToString();
						if (!string.IsNullOrEmpty(discountsJson))
						{
							promoItemDiscounts = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(discountsJson) ?? new Dictionary<string, double>();
						}
					}
					catch
					{
						// If deserialization fails, continue without item discounts
						promoItemDiscounts = new Dictionary<string, double>();
					}
				}
				
				foreach (var cart in ShoppingCartVM.ShoppingCartList)
				{
					// Create composite key for this item
					string itemKey = $"{cart.ProductId}_{cart.ProductVariantId?.ToString() ?? "0"}_{cart.FlashSaleItemId?.ToString() ?? "0"}_{cart.ComboOfferId?.ToString() ?? "0"}";
					
					// Get promo code discount for this item (if any)
					decimal? promoDiscount = null;
					if (promoItemDiscounts.ContainsKey(itemKey))
					{
						promoDiscount = (decimal)promoItemDiscounts[itemKey];
					}
					
					OrderDetail orderDetail = new()
					{
						ProductId = cart.ProductId,
						OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
						Price = cart.Price,
						Count = cart.Count,
						FlashSaleItemId = cart.FlashSaleItemId, // Copy flash sale item ID if exists
						ComboOfferId = cart.ComboOfferId, // Copy combo offer ID if exists
						ProductVariantId = cart.ProductVariantId,
						PromoCodeDiscountAmount = promoDiscount // Store item-level promo discount
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
						ShoppingCartVM.OrderHeader.PaymentMethod = SD.PaymentMethodTappy;
						_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
						_unitOfWork.save();
							
							// Create Tabby payment
							var tappyHelper = new TappyHelper(_tappySettings);
							
							// Build order items for Tabby
							var tabbyItems = ShoppingCartVM.ShoppingCartList.Select(cart => 
							{
								var productImageUrl = GetProductImageUrl(cart.product);
								var fullImageUrl = productImageUrl.StartsWith("http") 
									? productImageUrl 
									: domain.TrimEnd('/') + productImageUrl;
								var productUrl = domain + $"customer/home/details?id={cart.ProductId}";
								
								return new TabbyOrderItem
								{
									ReferenceId = cart.ProductId.ToString(),
									Title = cart.product?.Title ?? "Product",
									Description = cart.product?.Description?.Length > 500 
										? cart.product.Description.Substring(0, 500) 
										: cart.product?.Description,
									Quantity = cart.Count,
									UnitPrice = (decimal)cart.Price,
									DiscountAmount = 0,
									ImageUrl = fullImageUrl,
									ProductUrl = productUrl,
									Category = cart.product?.categry?.Name ?? "General"
								};
							}).ToList();
							
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
								Description = $"Order #{ShoppingCartVM.OrderHeader.Id} - {ShoppingCartVM.ShoppingCartList.Count()} items",
								ShippingCity = ShoppingCartVM.OrderHeader.City,
								ShippingAddress = ShoppingCartVM.OrderHeader.StreetAddress,
								ShippingPostalCode = ShoppingCartVM.OrderHeader.PostalCode,
								TaxAmount = 0,
								ShippingAmount = 0,
								DiscountAmount = (decimal?)ShoppingCartVM.OrderHeader.DiscountAmount,
								Language = "en",
								Items = tabbyItems
							};

							var tappyResponse = await tappyHelper.CreatePaymentAsync(tappyRequest);
							
							if (tappyResponse.Success && !string.IsNullOrEmpty(tappyResponse.PaymentUrl))
							{
                                // Store Tappy transaction ID
                                ShoppingCartVM.OrderHeader.SessionId = tappyResponse.TransactionId;
								_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
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
							ShoppingCartVM.OrderHeader.PaymentMethod = SD.PaymentMethodTamara;
							_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
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
                                ShoppingCartVM.OrderHeader.SessionId = tamaraResponse.CheckoutId;
								_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
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
						var geideaHelper = new GeideaHelper(_geideaSettings);
						
						string callbackUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}";
						string returnUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}";
						
						bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
						
						if ((callbackUrl.Contains("localhost") || callbackUrl.Contains("127.0.0.1") || !callbackUrl.StartsWith("https://")) 
							&& string.IsNullOrEmpty(_geideaSettings.CallbackUrlOverride))
						{
							if (isAjax)
							{
								return Json(new 
								{ 
									success = false, 
									error = "Geidea requires a public HTTPS callback URL. " +
											"For local testing, please configure 'CallbackUrlOverride' in appsettings.json with your ngrok URL or public domain. " +
											"Example: 'CallbackUrlOverride': 'https://your-ngrok-url.ngrok.io'"
								});
							}
							else
							{
								TempData["error"] = "Geidea requires a public HTTPS callback URL. " +
													"For local testing, please configure 'CallbackUrlOverride' in appsettings.json with your ngrok URL or public domain.";
								return RedirectToAction("Summary");
							}
						}
						
						if (!string.IsNullOrEmpty(_geideaSettings.CallbackUrlOverride))
						{
							var overrideBase = _geideaSettings.CallbackUrlOverride.TrimEnd('/');
							callbackUrl = $"{overrideBase}/Customer/Cart/GeideaCallback?orderId={ShoppingCartVM.OrderHeader.Id}";
							returnUrl = $"{overrideBase}/Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}";
						}
						else
						{
							callbackUrl = domain + $"/Customer/Cart/GeideaCallback?orderId={ShoppingCartVM.OrderHeader.Id}";
							returnUrl = domain + $"/Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}";
						}

						var geideaRequest = new GeideaPaymentRequest
						{
							Amount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal,
							Currency = "AED",
							OrderId = ShoppingCartVM.OrderHeader.Id.ToString(),
							CustomerName = ShoppingCartVM.OrderHeader.Name,
							CustomerEmail = ShoppingCartVM.OrderHeader.Email ?? (isGuest ? "" : applicationUser?.Email ?? ""),
							CustomerPhone = ShoppingCartVM.OrderHeader.PhoneNumber,
							ReturnUrl = callbackUrl, 
							CancelUrl = domain + "/Customer/Cart/Index",
							BillingAddress = ShoppingCartVM.OrderHeader.StreetAddress,
							BillingCity = ShoppingCartVM.OrderHeader.City,
							BillingState = ShoppingCartVM.OrderHeader.State,
							BillingPostalCode = ShoppingCartVM.OrderHeader.PostalCode,
							BillingCountryCode = "AE"
						};

						var geideaResponse = await geideaHelper.CreatePaymentAsync(geideaRequest);
						
						if (!geideaResponse.Success)
						{
							if (isAjax)
							{
								return Json(new 
								{ 
									success = false, 
									error = "Failed to create Geidea payment: " + geideaResponse.Message
								});
							}
							else
							{
								TempData["error"] = "Failed to create Geidea payment: " + geideaResponse.Message;
								return RedirectToAction("Summary");
							}
						}
						
						// Store payment method and Geidea info
						ShoppingCartVM.OrderHeader.PaymentMethod = SD.PaymentMethodGeidea;
						ShoppingCartVM.OrderHeader.SessionId = geideaResponse.TransactionId;
						ShoppingCartVM.OrderHeader.PaymentIntentId = geideaResponse.TransactionId;
						_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
						_unitOfWork.save();
						
						if (isAjax)
						{
							// Return JSON with sessionId for Geidea v2 HPP JavaScript integration
							return Json(new 
							{ 
								success = true, 
								paymentMethod = SD.PaymentMethodGeidea,
								sessionId = geideaResponse.TransactionId, // This is the sessionId for v2 HPP
								orderId = ShoppingCartVM.OrderHeader.Id, // Order ID for redirect after payment
								redirectUrl = geideaResponse.PaymentUrl // Keep for fallback if needed
							});
						}
						else
						{
							Response.Headers.Add("Location", geideaResponse.PaymentUrl);
							return new StatusCodeResult(303);
						}
					}
				}

				return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
			}
			else
				TempData["success"] = "You Need to Add Items in the Cart";
			return RedirectToAction(nameof(Index),"Home");
		}


		// Geidea Payment Callback (Webhook)
		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> GeideaCallback(int orderId)
		{
			var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId);
			
			if (orderHeader == null)
			{
				return BadRequest("Order not found");
			}

				if (!string.IsNullOrEmpty(orderHeader.SessionId))
				{
					var geideaHelper = new GeideaHelper(_geideaSettings);
					// Use order ID (merchant reference ID) for verification, not session ID
					var verificationResponse = await geideaHelper.VerifyPaymentAsync(orderHeader.Id.ToString());

				if (verificationResponse.Success && verificationResponse.IsPaid)
				{
					orderHeader.PaymentStatus = SD.PaymentStatusPaid;
					orderHeader.OrderStatus = SD.StatusPaid;
					if (!string.IsNullOrEmpty(orderHeader.SessionId))
					{
						orderHeader.PaymentIntentId = orderHeader.SessionId;
					}
					orderHeader.PaymentDate = BulkyBook.Utility.DateTimeHelper.Now;
					
					_unitOfWork.OrderHeader.Update(orderHeader);
					_unitOfWork.save();
					
					return Ok(new { status = "success", message = "Payment verified and order updated" });
				}
			}

			return Ok(new { status = "received", message = "Callback received" });
		}

		// Tappy Payment Callback
		public async Task<IActionResult> TappyCallback(int orderId, string status = "", string payment_id = "")
		{
			var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId);
			
			if (orderHeader == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}

			string paymentStatus = Request.Query["status"].ToString().ToLower();
			string paymentId = Request.Query["payment_id"].ToString();
			
			if (string.IsNullOrEmpty(paymentStatus))
			{
				paymentStatus = status?.ToLower() ?? "";
			}
			
			if (!string.IsNullOrEmpty(paymentId))
			{
				orderHeader.SessionId = paymentId;
				_unitOfWork.OrderHeader.Update(orderHeader);
				_unitOfWork.save();
			}

			bool paymentSuccessful = false;

			if (paymentStatus == "authorized" || paymentStatus == "created" || paymentStatus == "approved" || paymentStatus == "success")
			{
				paymentSuccessful = true;
			}
			else if (paymentStatus == "rejected" || paymentStatus == "declined" || paymentStatus == "failed")
			{
				paymentSuccessful = false;
			}
			else
			{
				if (!string.IsNullOrEmpty(orderHeader.SessionId))
				{
					try
					{
						var tappyHelper = new TappyHelper(_tappySettings);
						var verificationResponse = await tappyHelper.VerifyPaymentAsync(orderHeader.SessionId);
						
						if (verificationResponse.Success && verificationResponse.IsPaid)
						{
							paymentSuccessful = true;
						}
					}
					catch (Exception ex)
					{
					}
				}
				
				if (!paymentSuccessful)
				{
					paymentSuccessful = true;  
				}
			}

			if (paymentSuccessful)
			{
				orderHeader.PaymentDate = BulkyBook.Utility.DateTimeHelper.Now;
				orderHeader.PaymentStatus = SD.StatusPaid;
				orderHeader.OrderStatus = SD.PaymentStatusPaid;
                _unitOfWork.OrderHeader.Update(orderHeader);
				_unitOfWork.save();
				
				await _stockService.ProcessOrderStockDeduction(orderId);

				return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
			}
			else
			{
				TempData["error"] = "Payment verification failed. Please contact support with your order ID: " + orderId;
				return RedirectToAction("Index", "Home");
			}
		}

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
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				var authResponse = await tamaraHelper.AuthorizeOrderAsync(orderHeader.SessionId);
				
				if (authResponse.Success)
				{
					var orderDetails = await tamaraHelper.GetOrderDetailsAsync(orderHeader.SessionId);
					
					if (orderDetails.Success && orderDetails.PaymentStatus?.ToLower() == "approved")
					{
						_unitOfWork.OrderHeader.UpdateStatus(orderId, SD.StatusPaid, SD.PaymentStatusPaid);
						orderHeader.PaymentDate = BulkyBook.Utility.DateTimeHelper.Now;
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

		[HttpPost]
		public async Task<IActionResult> TamaraNotification()
		{
			// Handle Tamara webhook notification
			// This is called by Tamara to notify about payment status changes
			return Ok();
		}

		public async Task<IActionResult> OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
			
			if (orderHeader == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}

			if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
			{
				orderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
			}

			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				if (orderHeader.PaymentMethod == SD.PaymentMethodGeidea)
				{
					// Only verify if payment status is not already paid
					if (orderHeader.PaymentStatus != SD.PaymentStatusPaid)
					{
						var geideaHelper = new GeideaHelper(_geideaSettings);
						// Use order ID (merchant reference ID) for verification, not session ID
						var verificationResponse = await geideaHelper.VerifyPaymentAsync(orderHeader.Id.ToString());

						if (verificationResponse.Success && verificationResponse.IsPaid)
						{
							// Update payment status directly on the tracked entity
							orderHeader.PaymentStatus = SD.PaymentStatusPaid;
							orderHeader.OrderStatus = SD.StatusPaid;
							if (!string.IsNullOrEmpty(orderHeader.SessionId))
							{
								orderHeader.PaymentIntentId = orderHeader.SessionId;
							}
							orderHeader.PaymentDate = BulkyBook.Utility.DateTimeHelper.Now;
							
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
						}
					}
				}
			}

			//  PROCESS STOCK DEDUCTION AFTER PAYMENT CONFIRMED
			
            try
            {
                await _stockService.ProcessOrderStockDeduction(id);
            }
            catch (Exception ex) { }

            try
            {
                await _notificationService.SendOrderNotificationToAdmins(orderHeader);
            }
            catch (Exception ex) { }


			// Send order confirmation to customer
			if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
			{
				var customer = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
				if (customer != null)
				{
                    try
                    {
                        await _notificationService.SendOrderConfirmationToCustomer(orderHeader, customer);

                    }
                    catch (Exception ex) { }

                   
				}

				// Clear cart from database for authenticated users
				List<ShoppingCart> shoppingCarts = _unitOfWork.shoppingCart
					.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

				_unitOfWork.shoppingCart.removeRage(shoppingCarts);
				_unitOfWork.save();
			}
			else
			{
				BulkyBook.Utility.GuestCartHelper.ClearCart(HttpContext.Session);
                try
                {
                    await _notificationService.SendOrderConfirmationToCustomerGuest(orderHeader);

                }
                catch (Exception ex) { }
            }

			return View(id);
		}


		private double  GetPriceBasedOnQty(ShoppingCart  shoppingCart) 
        { 
            // Use base price for all quantities
            return shoppingCart.product.Price;
        }

        // 🔥 NEW: Get cart item price - uses flash sale price if available, then variant price, otherwise quantity-based pricing
        // Helper method to check if a cart item is eligible for promo code discount
        private bool IsItemEligibleForPromoCode(ShoppingCart cartItem, PromoCode promoCode)
        {
            // Check if product is excluded
            if (promoCode.ExcludedProducts != null && promoCode.ExcludedProducts.Any(ep => ep.ProductId == cartItem.ProductId))
            {
                return false;
            }

            // Check if combo offer is excluded
            if (cartItem.ComboOfferId.HasValue && 
                promoCode.ExcludedComboOffers != null && 
                promoCode.ExcludedComboOffers.Any(eco => eco.ComboOfferId == cartItem.ComboOfferId.Value))
            {
                return false;
            }

            // Check if item already has a discount and promo code excludes discounted items
            if (promoCode.ExcludeDiscountedItems)
            {
                // Item has flash sale discount
                if (cartItem.FlashSaleItemId.HasValue)
                {
                    return false;
                }

                // Item has combo offer discount
                if (cartItem.ComboOfferId.HasValue)
                {
                    return false;
                }

                // Check if variant has a discount (ListPrice > Price)
                if (cartItem.ProductVariantId.HasValue && cartItem.ProductVariant != null)
                {
                    if (cartItem.ProductVariant.ListPrice.HasValue && 
                        cartItem.ProductVariant.ListPrice.Value > cartItem.ProductVariant.Price)
                    {
                        return false;
                    }
                }
                else if (cartItem.product != null)
                {
                    // Check if product has a discount (ListPrice > Price)
                    if (cartItem.product.ListPrice > cartItem.product.Price)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // Helper method to calculate promo code discount for a single item
        private double CalculateItemPromoDiscount(ShoppingCart cartItem, PromoCode promoCode, double itemSubtotal)
        {
            if (!IsItemEligibleForPromoCode(cartItem, promoCode))
            {
                return 0;
            }

            double discount = 0;

            if (promoCode.DiscountType == BulkyBook.Models.DiscountType.Percentage)
            {
                discount = itemSubtotal * ((double)promoCode.DiscountValue / 100);
            }
            else
            {
                // For fixed amount, we need to distribute it proportionally across eligible items
                // This will be handled at the cart level
                discount = 0; // Will be calculated at cart level for fixed amounts
            }

            return discount;
        }

        // Helper method to calculate promo code discounts for all eligible items
        // Returns a dictionary keyed by a composite key: "ProductId_ProductVariantId_FlashSaleItemId_ComboOfferId"
        private (double totalDiscount, Dictionary<string, double> itemDiscounts) CalculatePromoCodeDiscounts(
            IEnumerable<ShoppingCart> cartList, PromoCode promoCode)
        {
            var itemDiscounts = new Dictionary<string, double>();
            double eligibleSubtotal = 0;
            var eligibleItems = new List<(ShoppingCart item, double subtotal, string key)>();

            // First pass: identify eligible items and calculate their subtotals
            foreach (var cart in cartList)
            {
                cart.Price = GetCartItemPrice(cart);
                double itemSubtotal = cart.Price * cart.Count;

                if (IsItemEligibleForPromoCode(cart, promoCode))
                {
                    // Create a composite key for this item
                    string itemKey = $"{cart.ProductId}_{cart.ProductVariantId?.ToString() ?? "0"}_{cart.FlashSaleItemId?.ToString() ?? "0"}_{cart.ComboOfferId?.ToString() ?? "0"}";
                    eligibleSubtotal += itemSubtotal;
                    eligibleItems.Add((cart, itemSubtotal, itemKey));
                }
            }

            // Check minimum order amount on eligible items only
            if (promoCode.MinimumOrderAmount.HasValue && (decimal)eligibleSubtotal < promoCode.MinimumOrderAmount.Value)
            {
                return (0, itemDiscounts);
            }

            double totalDiscount = 0;

            if (promoCode.DiscountType == BulkyBook.Models.DiscountType.Percentage)
            {
                // Percentage discount: apply to each eligible item proportionally
                double discountPercentage = (double)promoCode.DiscountValue / 100;
                
                foreach (var (item, subtotal, key) in eligibleItems)
                {
                    double itemDiscount = subtotal * discountPercentage;
                    itemDiscounts[key] = itemDiscount;
                    totalDiscount += itemDiscount;
                }

                // Apply maximum discount limit if set
                if (promoCode.MaximumDiscountAmount.HasValue && (decimal)totalDiscount > promoCode.MaximumDiscountAmount.Value)
                {
                    // Proportionally reduce all discounts to fit within the maximum
                    double reductionRatio = (double)promoCode.MaximumDiscountAmount.Value / totalDiscount;
                    totalDiscount = (double)promoCode.MaximumDiscountAmount.Value;
                    
                    var adjustedItemDiscounts = new Dictionary<string, double>();
                    foreach (var kvp in itemDiscounts)
                    {
                        adjustedItemDiscounts[kvp.Key] = kvp.Value * reductionRatio;
                    }
                    itemDiscounts = adjustedItemDiscounts;
                }
            }
            else
            {
                // Fixed amount discount: distribute proportionally across eligible items
                double fixedDiscount = (double)promoCode.DiscountValue;
                
                // Ensure discount doesn't exceed eligible subtotal
                if (fixedDiscount > eligibleSubtotal)
                {
                    fixedDiscount = eligibleSubtotal;
                }

                // Distribute proportionally
                foreach (var (item, subtotal, key) in eligibleItems)
                {
                    double itemDiscount = (subtotal / eligibleSubtotal) * fixedDiscount;
                    itemDiscounts[key] = itemDiscount;
                    totalDiscount += itemDiscount;
                }
            }

            return (totalDiscount, itemDiscounts);
        }

        private double GetCartItemPrice(ShoppingCart shoppingCart)
        {
            // If this item is from a flash sale, use the flash sale price
            if (shoppingCart.FlashSaleItemId.HasValue)
            {
                // First check if FlashSalePrice is set directly
                if (shoppingCart.FlashSalePrice.HasValue)
                {
                    return (double)shoppingCart.FlashSalePrice.Value;
                }
                
                // If FlashSalePrice is not set but FlashSaleItemId is, load from FlashSaleItem
                if (shoppingCart.FlashSaleItem != null)
                {
                    return (double)shoppingCart.FlashSaleItem.FlashSalePrice;
                }
                
                // If FlashSaleItem is not loaded, load it
                var flashSaleItem = _unitOfWork.FlashSaleItem.Get(f => f.Id == shoppingCart.FlashSaleItemId.Value);
                if (flashSaleItem != null)
                {
                    return (double)flashSaleItem.FlashSalePrice;
                }
            }

            // If this item is from a combo offer, use the combo price directly
            if (shoppingCart.ComboOfferId.HasValue)
            {
                var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(shoppingCart.ComboOfferId.Value);
                if (comboOffer != null)
                {
                    // Return combo price per unit (combo price / count)
                    // Count represents number of combos, so price per combo is comboPrice / count
                    return (double)comboOffer.ComboPrice;
                }
            }

            // If this item has a variant, use the variant price
            if (shoppingCart.ProductVariantId.HasValue && shoppingCart.ProductVariantId.Value > 0)
            {
                // Load variant if not already loaded
                if (shoppingCart.ProductVariant == null)
                {
                    shoppingCart.ProductVariant = _unitOfWork.ProductVariant.Get(v => v.Id == shoppingCart.ProductVariantId.Value);
                }
                
                if (shoppingCart.ProductVariant != null)
                {
                    return (double)shoppingCart.ProductVariant.Price;
                }
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
                    var now = BulkyBook.Utility.DateTimeHelper.Now;
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
            // Use base price for all quantities
            return shoppingCart.product.Price;
        }

        // 🔥 Helper method to get product image URL (checks ProductImages first, then falls back to ImageUrl)
        private string GetProductImageUrl(Product product)
        {
            if (product == null)
            {
                return "/images/no-image.png"; // Default placeholder image
            }

            string imageUrl = null;

            // Check if product has ProductImages (ignore images with ImageInfo)
            if (product.ProductImages != null && product.ProductImages.Any())
            {
                // Get the first image ordered by DisplayOrder, excluding images with ImageInfo
                var firstImage = product.ProductImages
                    .Where(pi => pi.ImageInfo == null)
                    .OrderBy(pi => pi.DisplayOrder)
                    .FirstOrDefault();
                if (firstImage != null && !string.IsNullOrEmpty(firstImage.ImageUrl))
                {
                    imageUrl = firstImage.ImageUrl;
                }
            }

            // Fallback to ImageUrl if no ProductImages
            if (string.IsNullOrEmpty(imageUrl) && !string.IsNullOrEmpty(product.ImageUrl))
            {
                imageUrl = product.ImageUrl;
            }

            // Normalize image URL
            if (!string.IsNullOrEmpty(imageUrl))
            {
                // Replace backslashes with forward slashes
                imageUrl = imageUrl.Replace('\\', '/');
                
                // Remove double slashes (except after http:// or https://)
                imageUrl = System.Text.RegularExpressions.Regex.Replace(imageUrl, @"([^:]/)/+", "$1");
                
                // Fix case sensitivity: Images -> images, but keep Products as Products
                // The actual folder structure is: wwwroot/images/Products/
                imageUrl = System.Text.RegularExpressions.Regex.Replace(imageUrl, @"/Images/", "/images/", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                // Ensure it starts with / if it's a relative path
                if (!imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                    && !imageUrl.StartsWith("/") 
                    && !imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    imageUrl = "/" + imageUrl;
                }
                
                return imageUrl;
            }

            // Return placeholder if no image found
            return "/images/no-image.png";
        }
    }

}
