using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
	[Area("Customer")]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailSender _emailSender;
		private readonly IdealWeightNutrition.Services.INotificationService _notificationService;
		private readonly IdealWeightNutrition.Services.IStockService _stockService;
		private readonly TappySettings _tappySettings;
		private readonly TamaraSettings _tamaraSettings;
		private readonly GeideaSettings _geideaSettings;
		private readonly IStringLocalizer<SharedResources> _localizer;
		private readonly ILogger<CartController> _logger;
		public ShoppingCartVM  ShoppingCartVM { get; set; }

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender, IdealWeightNutrition.Services.INotificationService notificationService, IdealWeightNutrition.Services.IStockService stockService, IOptions<TappySettings> tappySettings, IOptions<TamaraSettings> tamaraSettings, IOptions<GeideaSettings> geideaSettings, IStringLocalizer<SharedResources> localizer, ILogger<CartController> logger) 
        {
         _unitOfWork = unitOfWork;
			_emailSender = emailSender;
			_notificationService = notificationService;
			_stockService = stockService;
			_tappySettings = tappySettings.Value;
			_tamaraSettings = tamaraSettings.Value;
			_geideaSettings = geideaSettings.Value;
			_localizer = localizer;
			_logger = logger;
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
                var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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
                var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                cartItems = guestCart.Select(gc => new ShoppingCart
                {
                    Id = 0, // Guest cart items don't have database IDs - use 0 as placeholder
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
                
                // Get stock information for validation
                int availableStock = 0;
                int? flashSaleQuantity = null;
                
                if (cart.FlashSaleItemId.HasValue && cart.FlashSaleItem != null)
                {
                    // Flash sale item - use flash sale quantity
                    flashSaleQuantity = cart.FlashSaleItem.FlashSaleQuantity;
                    availableStock = cart.FlashSaleItem.FlashSaleQuantity;
                }
                else if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
                {
                    // Variant product - use variant stock
                    availableStock = cart.ProductVariant.StockQuantity;
                }
                else if (cart.product != null)
                {
                    // Regular product - use product stock
                    availableStock = cart.product.StockQuantity;
                }
                
                // Get product slug for details page link
                string productSlug = null;
                if (cart.product != null && !isComboOffer)
                {
                    productSlug = cart.product.GetSlug();
                }
                
                return new
                {
                    productId = cart.ProductId, // Always use ProductId (never 0 for valid items)
                    title = displayTitle,
                    imageUrl = imageUrl,
                    price = GetCartItemPrice(cart), // 🔥 Use new method that checks flash sale price
                    count = cart.Count,
                    cartId = cart.Id > 0 ? cart.Id : (int?)null, // Use null for guest cart items (Id = 0)
                    isFlashSale = cart.FlashSaleItemId.HasValue, // 🔥 Indicate if it's a flash sale item
                    isComboOffer = isComboOffer, // 🔥 Indicate if it's a combo offer
                    variantName = variantName, // 🔥 Include variant name if exists
                    availableStock = availableStock, // 🔥 Stock available for this item
                    flashSaleQuantity = flashSaleQuantity, // 🔥 Flash sale quantity if applicable
                    flashSaleItemId = cart.FlashSaleItemId, // 🔥 Flash sale item ID if applicable
                    productSlug = productSlug // 🔥 Product slug for details page link
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
                var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                var guestItem = guestCart.FirstOrDefault(c => c.ProductId == productId);
                
                if (guestItem != null)
                {
                    // 🔥 Validate quantity limits
                    var validationResult = ValidateQuantityUpdate(productId, guestItem.FlashSaleItemId, count);
                    
                    if (!validationResult.isValid)
                    {
                        return Json(new { success = false, message = validationResult.message });
                    }

                    IdealWeightNutrition.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, productId, count);
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
                var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
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
                    var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    
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

                    IdealWeightNutrition.Utility.GuestCartHelper.SaveGuestCart(HttpContext.Session, guestCart);

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

                    // Reload cart item with all navigation properties to ensure they're fresh
                    cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    
                    if (cartFromDD == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    // Ensure product is loaded before calculating prices
                    if (cartFromDD.product == null)
                    {
                        cartFromDD.product = _unitOfWork.product.Get(p => p.Id == cartFromDD.ProductId);
                    }
                    
                    // Calculate updated prices
                    cartFromDD.Price = GetCartItemPrice(cartFromDD);
                    var unitPrice = cartFromDD.Price;
                    
                    // If GetCartItemPrice returned 0, check if it's a variant (variants can legitimately be 0)
                    // Only try to fix price if no variant exists
                    if (unitPrice == 0 && cartFromDD.ProductVariantId.HasValue && cartFromDD.ProductVariant != null)
                    {
                        // Variant exists and price is 0 - this is valid (free variant)
                        // Don't override with product price, keep variant price of 0
                        _logger.LogInformation($"Pluse - Variant price is 0 (free variant) for ProductId: {cartFromDD.ProductId}, VariantId: {cartFromDD.ProductVariantId}");
                    }
                    else if (unitPrice == 0 && cartFromDD.product != null && !cartFromDD.ProductVariantId.HasValue)
                    {
                        // No variant, but price is 0 - try to get price from product as fallback
                        // This might indicate a data issue
                        if (cartFromDD.product.Price > 0)
                        {
                            unitPrice = cartFromDD.product.Price;
                            cartFromDD.Price = unitPrice;
                            _logger.LogWarning($"Pluse - GetCartItemPrice returned 0 for non-variant product, using product.Price directly: {unitPrice}");
                        }
                        else if (cartFromDD.product.ListPrice > 0)
                        {
                            unitPrice = cartFromDD.product.ListPrice;
                            cartFromDD.Price = unitPrice;
                            _logger.LogWarning($"Pluse - GetCartItemPrice returned 0 for non-variant product, using product.ListPrice as fallback: {unitPrice}");
                        }
                        // If both are 0, product is legitimately free - keep 0
                    }
                    
                    var totalPrice = unitPrice * cartFromDD.Count;
                    decimal? originalPrice = (decimal)(cartFromDD.FlashSaleItemId.HasValue && cartFromDD.product != null && cartFromDD.product.Price > unitPrice ? cartFromDD.product.Price : 0);

                    // Debug logging with detailed information
                    var productPrice = cartFromDD.product?.Price ?? 0;
                    var productListPrice = cartFromDD.product?.ListPrice ?? 0;
                    _logger.LogWarning($"Pluse - CartId: {CartId}, ProductId: {cartFromDD.ProductId}, Product: {(cartFromDD.product != null ? cartFromDD.product.Title : "NULL")}, ProductPrice: {productPrice}, ProductListPrice: {productListPrice}, UnitPrice: {unitPrice}, Count: {cartFromDD.Count}, FlashSaleItemId: {cartFromDD.FlashSaleItemId}, ProductVariantId: {cartFromDD.ProductVariantId}, ComboOfferId: {cartFromDD.ComboOfferId}");

                    // Calculate order total
                    var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                    var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    var orderTotal = allCartItems.Sum(c => {
                        var itemPrice = GetCartItemPrice(c);
                        // If price is 0 and we have a variant, that's valid (free variant)
                        // Only try to fix if no variant exists
                        if (itemPrice == 0 && c.product != null && !c.ProductVariantId.HasValue)
                        {
                            // Try Price first, then ListPrice as fallback
                            if (c.product.Price > 0)
                            {
                                itemPrice = c.product.Price;
                            }
                            else if (c.product.ListPrice > 0)
                            {
                                itemPrice = c.product.ListPrice;
                            }
                            // If both are 0, product is legitimately free - keep 0
                        }
                        return itemPrice * c.Count;
                    });
                    
                    _logger.LogWarning($"Pluse - OrderTotal calculated: {orderTotal}, CartItemsCount: {allCartItems.Count()}");
                    
                    // Log each cart item price for debugging
                    foreach (var item in allCartItems)
                    {
                        var itemPrice = GetCartItemPrice(item);
                        _logger.LogWarning($"Pluse - CartItem: ProductId={item.ProductId}, Count={item.Count}, Price={itemPrice}, Total={itemPrice * item.Count}");
                    }

                    return Json(new { 
                        success = true, 
                        count = cartFromDD.Count,
                        unitPrice = unitPrice,
                        totalPrice = totalPrice,
                        originalPrice = originalPrice,
                        orderTotal = orderTotal,
                        debugInfo = new {
                            productPrice = productPrice,
                            productListPrice = productListPrice,
                            productTitle = cartFromDD.product?.Title,
                            hasFlashSale = cartFromDD.FlashSaleItemId.HasValue,
                            hasVariant = cartFromDD.ProductVariantId.HasValue,
                            hasCombo = cartFromDD.ComboOfferId.HasValue
                        },
                        message = _localizer["QuantityUpdated"].Value
                    });
                }
                else if (ProductId.HasValue)
                {
                    var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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

                    IdealWeightNutrition.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, newQuantity);
                    
                    // Recalculate prices using GetCartItemPrice (handles variants, flash sales, combos correctly)
                    var updatedCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var updatedItem = updatedCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                    
                    if (updatedItem == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }
                    
                    // Convert GuestCartItem to ShoppingCart to use GetCartItemPrice
                    var shoppingCartItem = new ShoppingCart
                    {
                        ProductId = updatedItem.ProductId,
                        Count = updatedItem.Count,
                        ProductVariantId = updatedItem.ProductVariantId,
                        FlashSaleItemId = updatedItem.FlashSaleItemId,
                        FlashSalePrice = (decimal?)updatedItem.FlashSalePrice,
                        ComboOfferId = updatedItem.ComboOfferId,
                        product = _unitOfWork.product.Get(p => p.Id == updatedItem.ProductId),
                        ProductVariant = updatedItem.ProductVariantId.HasValue 
                            ? _unitOfWork.ProductVariant.Get(v => v.Id == updatedItem.ProductVariantId.Value) 
                            : null,
                        FlashSaleItem = updatedItem.FlashSaleItemId.HasValue 
                            ? _unitOfWork.FlashSaleItem.Get(f => f.Id == updatedItem.FlashSaleItemId.Value) 
                            : null,
                        ComboOffer = updatedItem.ComboOfferId.HasValue 
                            ? _unitOfWork.ComboOffer.GetComboOfferWithItems(updatedItem.ComboOfferId.Value) 
                            : null
                    };
                    
                    // Calculate unit price using GetCartItemPrice
                    shoppingCartItem.Price = GetCartItemPrice(shoppingCartItem);
                    var unitPrice = shoppingCartItem.Price;
                    var totalPrice = unitPrice * updatedItem.Count;
                    
                    // Calculate order total for all items
                    var orderTotal = updatedCart.Sum(gc => {
                        var cartItem = new ShoppingCart
                        {
                            ProductId = gc.ProductId,
                            Count = gc.Count,
                            ProductVariantId = gc.ProductVariantId,
                            FlashSaleItemId = gc.FlashSaleItemId,
                            FlashSalePrice = (decimal?)gc.FlashSalePrice,
                            ComboOfferId = gc.ComboOfferId,
                            product = _unitOfWork.product.Get(p => p.Id == gc.ProductId),
                            ProductVariant = gc.ProductVariantId.HasValue 
                                ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) 
                                : null,
                            FlashSaleItem = gc.FlashSaleItemId.HasValue 
                                ? _unitOfWork.FlashSaleItem.Get(f => f.Id == gc.FlashSaleItemId.Value) 
                                : null,
                            ComboOffer = gc.ComboOfferId.HasValue 
                                ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) 
                                : null
                        };
                        var itemPrice = GetCartItemPrice(cartItem);
                        return itemPrice * gc.Count;
                    });
                    
                    // Calculate original price for display (if flash sale)
                    decimal? originalPrice = null;
                    if (shoppingCartItem.FlashSaleItemId.HasValue && shoppingCartItem.product != null && shoppingCartItem.product.Price > unitPrice)
                    {
                        originalPrice = (decimal)shoppingCartItem.product.Price;
                    }

                    return Json(new { 
                        success = true, 
                        count = updatedItem.Count,
                        unitPrice = unitPrice,
                        totalPrice = totalPrice,
                        originalPrice = originalPrice,
                        orderTotal = orderTotal,
                        debugInfo = new {
                            productPrice = shoppingCartItem.product?.Price ?? 0,
                            productListPrice = shoppingCartItem.product?.ListPrice ?? 0,
                            productTitle = shoppingCartItem.product?.Title,
                            hasFlashSale = shoppingCartItem.FlashSaleItemId.HasValue,
                            hasVariant = shoppingCartItem.ProductVariantId.HasValue,
                            hasCombo = shoppingCartItem.ComboOfferId.HasValue,
                            variantPrice = shoppingCartItem.ProductVariant?.Price ?? 0
                        },
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
                        var orderTotal = allCartItems.Sum(c => {
                            var itemPrice = GetCartItemPrice(c);
                            // If GetCartItemPrice returned 0 but product exists, try to get price directly
                            if (itemPrice == 0 && c.product != null)
                            {
                                // Try Price first, then ListPrice as fallback
                                if (c.product.Price > 0)
                                {
                                    itemPrice = c.product.Price;
                                }
                                else if (c.product.ListPrice > 0)
                                {
                                    itemPrice = c.product.ListPrice;
                                }
                            }
                            return itemPrice * c.Count;
                        });

                        return Json(new { 
                            success = true, 
                            removed = true,
                            orderTotal = orderTotal,
                            message = _localizer["ItemRemovedFromCart"].Value
                        });
                    }
                    else
                    {
                        // Reload cart item with all navigation properties to ensure they're fresh
                        cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                        
                        // Ensure product is loaded before calculating prices
                        if (cartFromDD.product == null)
                        {
                            cartFromDD.product = _unitOfWork.product.Get(p => p.Id == cartFromDD.ProductId);
                        }
                        
                        // Calculate updated prices
                        cartFromDD.Price = GetCartItemPrice(cartFromDD);
                        var unitPrice = cartFromDD.Price;
                        
                        // If GetCartItemPrice returned 0, check if it's a variant (variants can legitimately be 0)
                        // Only try to fix price if no variant exists
                        if (unitPrice == 0 && cartFromDD.ProductVariantId.HasValue && cartFromDD.ProductVariant != null)
                        {
                            // Variant exists and price is 0 - this is valid (free variant)
                            // Don't override with product price, keep variant price of 0
                            _logger.LogInformation($"Minus - Variant price is 0 (free variant) for ProductId: {cartFromDD.ProductId}, VariantId: {cartFromDD.ProductVariantId}");
                        }
                        else if (unitPrice == 0 && cartFromDD.product != null && !cartFromDD.ProductVariantId.HasValue)
                        {
                            // No variant, but price is 0 - try to get price from product as fallback
                            // This might indicate a data issue
                            if (cartFromDD.product.Price > 0)
                            {
                                unitPrice = cartFromDD.product.Price;
                                cartFromDD.Price = unitPrice;
                                _logger.LogWarning($"Minus - GetCartItemPrice returned 0 for non-variant product, using product.Price directly: {unitPrice}");
                            }
                            else if (cartFromDD.product.ListPrice > 0)
                            {
                                unitPrice = cartFromDD.product.ListPrice;
                                cartFromDD.Price = unitPrice;
                                _logger.LogWarning($"Minus - GetCartItemPrice returned 0 for non-variant product, using product.ListPrice as fallback: {unitPrice}");
                            }
                            // If both are 0, product is legitimately free - keep 0
                        }
                        
                        var totalPrice = unitPrice * cartFromDD.Count;
                        decimal? originalPrice = (decimal)(cartFromDD.FlashSaleItemId.HasValue && cartFromDD.product != null && cartFromDD.product.Price > unitPrice ? cartFromDD.product.Price : 0);

                        // Calculate order total
                        var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                        var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                        var orderTotal = allCartItems.Sum(c => {
                            var itemPrice = GetCartItemPrice(c);
                            // If GetCartItemPrice returned 0 but product exists, try to get price directly
                            if (itemPrice == 0 && c.product != null)
                            {
                                // Try Price first, then ListPrice as fallback
                                if (c.product.Price > 0)
                                {
                                    itemPrice = c.product.Price;
                                }
                                else if (c.product.ListPrice > 0)
                                {
                                    itemPrice = c.product.ListPrice;
                                }
                            }
                            return itemPrice * c.Count;
                        });

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
                    var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    var item = guestCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                    if (item == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    bool removed = false;
                    if (item.Count <= 1)
                    {
                        IdealWeightNutrition.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
                        removed = true;
                    }
                    else
                    {
                        IdealWeightNutrition.Utility.GuestCartHelper.UpdateQuantity(HttpContext.Session, ProductId.Value, item.Count - 1);
                    }

                    if (removed)
                    {
                        // Calculate order total using GetCartItemPrice for all remaining items
                        var updatedCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                        var orderTotal = updatedCart.Sum(gc => {
                            var cartItem = new ShoppingCart
                            {
                                ProductId = gc.ProductId,
                                Count = gc.Count,
                                ProductVariantId = gc.ProductVariantId,
                                FlashSaleItemId = gc.FlashSaleItemId,
                                FlashSalePrice = (decimal?)gc.FlashSalePrice,
                                ComboOfferId = gc.ComboOfferId,
                                product = _unitOfWork.product.Get(p => p.Id == gc.ProductId),
                                ProductVariant = gc.ProductVariantId.HasValue 
                                    ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) 
                                    : null,
                                FlashSaleItem = gc.FlashSaleItemId.HasValue 
                                    ? _unitOfWork.FlashSaleItem.Get(f => f.Id == gc.FlashSaleItemId.Value) 
                                    : null,
                                ComboOffer = gc.ComboOfferId.HasValue 
                                    ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) 
                                    : null
                            };
                            var itemPrice = GetCartItemPrice(cartItem);
                            return itemPrice * gc.Count;
                        });

                        return Json(new { 
                            success = true, 
                            removed = true,
                            orderTotal = orderTotal,
                            message = _localizer["ItemRemovedFromCart"].Value
                        });
                    }
                    else
                    {
                        // Recalculate prices using GetCartItemPrice (handles variants, flash sales, combos correctly)
                        var updatedCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                        var updatedItem = updatedCart.FirstOrDefault(c => c.ProductId == ProductId.Value);
                        
                        if (updatedItem == null)
                        {
                            return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                        }
                        
                        // Convert GuestCartItem to ShoppingCart to use GetCartItemPrice
                        var shoppingCartItem = new ShoppingCart
                        {
                            ProductId = updatedItem.ProductId,
                            Count = updatedItem.Count,
                            ProductVariantId = updatedItem.ProductVariantId,
                            FlashSaleItemId = updatedItem.FlashSaleItemId,
                            FlashSalePrice = (decimal?)updatedItem.FlashSalePrice,
                            ComboOfferId = updatedItem.ComboOfferId,
                            product = _unitOfWork.product.Get(p => p.Id == updatedItem.ProductId),
                            ProductVariant = updatedItem.ProductVariantId.HasValue 
                                ? _unitOfWork.ProductVariant.Get(v => v.Id == updatedItem.ProductVariantId.Value) 
                                : null,
                            FlashSaleItem = updatedItem.FlashSaleItemId.HasValue 
                                ? _unitOfWork.FlashSaleItem.Get(f => f.Id == updatedItem.FlashSaleItemId.Value) 
                                : null,
                            ComboOffer = updatedItem.ComboOfferId.HasValue 
                                ? _unitOfWork.ComboOffer.GetComboOfferWithItems(updatedItem.ComboOfferId.Value) 
                                : null
                        };
                        
                        // Calculate unit price using GetCartItemPrice
                        shoppingCartItem.Price = GetCartItemPrice(shoppingCartItem);
                        var unitPrice = shoppingCartItem.Price;
                        var totalPrice = unitPrice * updatedItem.Count;
                        
                        // Calculate order total for all items
                        var orderTotal = updatedCart.Sum(gc => {
                            var cartItem = new ShoppingCart
                            {
                                ProductId = gc.ProductId,
                                Count = gc.Count,
                                ProductVariantId = gc.ProductVariantId,
                                FlashSaleItemId = gc.FlashSaleItemId,
                                FlashSalePrice = (decimal?)gc.FlashSalePrice,
                                ComboOfferId = gc.ComboOfferId,
                                product = _unitOfWork.product.Get(p => p.Id == gc.ProductId),
                                ProductVariant = gc.ProductVariantId.HasValue 
                                    ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) 
                                    : null,
                                FlashSaleItem = gc.FlashSaleItemId.HasValue 
                                    ? _unitOfWork.FlashSaleItem.Get(f => f.Id == gc.FlashSaleItemId.Value) 
                                    : null,
                                ComboOffer = gc.ComboOfferId.HasValue 
                                    ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) 
                                    : null
                            };
                            var itemPrice = GetCartItemPrice(cartItem);
                            return itemPrice * gc.Count;
                        });
                        
                        // Calculate original price for display (if flash sale)
                        decimal? originalPrice = null;
                        if (shoppingCartItem.FlashSaleItemId.HasValue && shoppingCartItem.product != null && shoppingCartItem.product.Price > unitPrice)
                        {
                            originalPrice = (decimal)shoppingCartItem.product.Price;
                        }

                        return Json(new { 
                            success = true, 
                            removed = false,
                            count = updatedItem.Count,
                            unitPrice = unitPrice,
                            totalPrice = totalPrice,
                            originalPrice = originalPrice,
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
                    var userId = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier).Value;
                    ShoppingCart cartFromDD = null;
                    
                    if (CartId > 0)
                    {
                        // Try to find by CartId first
                        cartFromDD = _unitOfWork.shoppingCart.Get(a => a.Id == CartId && a.ApplicationUserId == userId);
                    }
                    
                    // If not found by CartId, try to find by ProductId
                    if (cartFromDD == null && ProductId.HasValue)
                    {
                        cartFromDD = _unitOfWork.shoppingCart.Get(a => a.ProductId == ProductId.Value && a.ApplicationUserId == userId);
                    }
                    
                    if (cartFromDD == null)
                    {
                        return Json(new { success = false, message = _localizer["CartItemNotFound"].Value });
                    }

                    _unitOfWork.shoppingCart.remove(cartFromDD);
                    _unitOfWork.save();

                    // Calculate order total after removal
                    var allCartItems = _unitOfWork.shoppingCart.GetAll(a => a.ApplicationUserId == userId, includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer");
                    var orderTotal = allCartItems.Sum(c => GetCartItemPrice(c) * c.Count);
                    var cartCount = allCartItems.Count(); // Count of unique products

                    return Json(new { 
                        success = true, 
                        orderTotal = orderTotal,
                        cartCount = cartCount,
                        message = _localizer["ItemRemovedFromCart"].Value 
                    });
                }
                else if (ProductId.HasValue)
                {
                    IdealWeightNutrition.Utility.GuestCartHelper.RemoveFromCart(HttpContext.Session, ProductId.Value);
                    
                    var updatedCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
                    
                    // Calculate order total using GetCartItemPrice (handles variants, flash sales, combos correctly)
                    var orderTotal = updatedCart.Sum(gc => {
                        var cartItem = new ShoppingCart
                        {
                            ProductId = gc.ProductId,
                            Count = gc.Count,
                            ProductVariantId = gc.ProductVariantId,
                            FlashSaleItemId = gc.FlashSaleItemId,
                            FlashSalePrice = (decimal?)gc.FlashSalePrice,
                            ComboOfferId = gc.ComboOfferId,
                            product = _unitOfWork.product.Get(p => p.Id == gc.ProductId),
                            ProductVariant = gc.ProductVariantId.HasValue 
                                ? _unitOfWork.ProductVariant.Get(v => v.Id == gc.ProductVariantId.Value) 
                                : null,
                            FlashSaleItem = gc.FlashSaleItemId.HasValue 
                                ? _unitOfWork.FlashSaleItem.Get(f => f.Id == gc.FlashSaleItemId.Value) 
                                : null,
                            ComboOffer = gc.ComboOfferId.HasValue 
                                ? _unitOfWork.ComboOffer.GetComboOfferWithItems(gc.ComboOfferId.Value) 
                                : null
                        };
                        var itemPrice = GetCartItemPrice(cartItem);
                        return itemPrice * gc.Count;
                    });
                    
                    var cartCount = updatedCart.Count; // Count of unique products

                    return Json(new { 
                        success = true, 
                        orderTotal = orderTotal,
                        cartCount = cartCount,
                        message = _localizer["ItemRemovedFromCart"].Value
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

			var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
			
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
				var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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
					// Get current culture for currency formatting
					var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
					var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
					var currencySymbol = IdealWeightNutrition.Utility.CurrencyHelper.GetCurrencySymbol(currentCulture);
					Func<decimal, string> formatCurrency = (amount) => 
						$"{currencySymbol} {amount.ToString("N2", System.Globalization.CultureInfo.InvariantCulture).Replace(".", ",")}";
					
					var minAmountMessage = string.Format(
						_localizer["MinimumOrderAmountRequiredForEligibleItems"].Value,
						formatCurrency(promo.MinimumOrderAmount.Value),
						formatCurrency((decimal)eligibleSubtotal)
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
				discountText = promo.DiscountType == IdealWeightNutrition.Models.DiscountType.Percentage 
					? $"{promo.DiscountValue}% off" 
					: $"{promo.DiscountValue:C} off",
				itemDiscounts = itemDiscounts, // Include item-level discounts for reference
				eligibleItems = eligibleItemsInfo, // Include eligible items with discount info
				eligibleItemCount = eligibleItemCount
			});
		}

		public async Task<IActionResult> Summary()
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
				includeProperties: "product,FlashSaleItem,ProductVariant,ComboOffer,product.ProductImages");

			// Load variant option values for each cart item and ensure ImageUrl is loaded
			foreach (var cart in cartList)
			{
				if (cart.FlashSaleItemId.HasValue && cart.FlashSaleItem == null)
				{
					cart.FlashSaleItem = _unitOfWork.FlashSaleItem.Get(f => f.Id == cart.FlashSaleItemId.Value);
				}
				
				if (cart.ProductVariantId.HasValue && cart.ProductVariant != null)
				{
					var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
						includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
					if (variant != null)
					{
						// Assign the fully loaded variant (with ImageUrl) to cart
						cart.ProductVariant = variant;
						
						if (cart.FlashSaleItemId.HasValue && cart.FlashSaleItem != null)
						{
							cart.FlashSaleItem.ProductVariant = variant;
						}
					}
				}
				else if (cart.ProductVariantId.HasValue && cart.ProductVariant == null)
				{
					var variant = _unitOfWork.ProductVariant.Get(v => v.Id == cart.ProductVariantId.Value, 
						includeProperties: "VariantOptionValues,VariantOptionValues.OptionValue,VariantOptionValues.OptionValue.ProductOption");
					if (variant != null)
					{
						cart.ProductVariant = variant;
						
						if (cart.FlashSaleItemId.HasValue && cart.FlashSaleItem != null)
						{
							cart.FlashSaleItem.ProductVariant = variant;
						}
					}
				}
			}

		// Try to get ApplicationUser first (users registered via email)
			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == userId);
		
		if (ShoppingCartVM.OrderHeader.ApplicationUser == null)
		{
			var allUsers = _unitOfWork.applicationUser.GetAllUsersWithoutDiscriminator();
			var identityUser = allUsers.FirstOrDefault(u => u.Id == userId);
			
			if (identityUser != null)
			{
				// Create a temporary ApplicationUser object for the view
				// Google users don't have address details, so they'll fill them at checkout
				ShoppingCartVM.OrderHeader.ApplicationUser = new ApplicationUser
				{
					Id = identityUser.Id,
					Email = identityUser.Email,
					UserName = identityUser.UserName,
					PhoneNumber = identityUser.PhoneNumber,
					Name = identityUser.UserName ?? identityUser.Email?.Split('@')[0] ?? "",
					StreetAddress = "",
					City = "",
					State = "",
					PostalCode = ""
				};
			}
		}
		
		// Handle users registered via Google who may not have complete address details
		if (ShoppingCartVM.OrderHeader.ApplicationUser != null)
		{
			ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name ?? "";
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber ?? "";
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress ?? "";
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City ?? "";
			ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State ?? "";
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode ?? "";
		}
		else
		{
			// Fallback if user not found at all (shouldn't happen, but safety check)
			ShoppingCartVM.OrderHeader.Name = "";
			ShoppingCartVM.OrderHeader.PhoneNumber = "";
			ShoppingCartVM.OrderHeader.StreetAddress = "";
			ShoppingCartVM.OrderHeader.City = "";
			ShoppingCartVM.OrderHeader.State = "";
			ShoppingCartVM.OrderHeader.PostalCode = "";
		}
		}
		else
		{
			// Guest user - load from session
			var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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
				product = _unitOfWork.product.Get(p => p.Id == gc.ProductId, includeProperties: "categry,ProductImages"),
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
			
			// Check if Tamara payment is available for this order amount
			bool tamaraAvailable = false;
			if (_tamaraSettings.Enabled && ShoppingCartVM.OrderHeader.OrderTotal > 0)
			{
				try
				{
					var tamaraHelper = new TamaraHelper(_tamaraSettings);
					// For AED, the range is typically similar (100-2500 AED)
					decimal orderAmount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal;
					tamaraAvailable = await tamaraHelper.IsPaymentAvailableAsync(orderAmount, _tamaraSettings.CountryCode ?? "AE");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error checking Tamara payment availability");
					tamaraAvailable = false;
				}
			}
			
			ViewData["TamaraAvailable"] = tamaraAvailable;
			ViewData["TamaraPublicKey"] = _tamaraSettings.PublicKey;
			ViewData["TamaraOrderTotal"] = ShoppingCartVM.OrderHeader.OrderTotal;
			
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
					var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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
				var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(HttpContext.Session);
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
				ShoppingCartVM.OrderHeader.OrderDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
				
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
						var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
						
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
							
							// Split customer name - Tamara requires both first and last name
							var nameParts = ShoppingCartVM.OrderHeader.Name.Trim().Split(' ', 2);
							var firstName = nameParts[0];
							var lastName = nameParts.Length > 1 ? nameParts[1] : firstName; // Use first name as last name if not provided
							
							// Ensure lastName is not empty (Tamara requirement)
							if (string.IsNullOrWhiteSpace(lastName))
							{
								lastName = firstName;
							}
							
							// Format phone number for Tamara (NO + prefix, just digits after country code)
							// Example: "501234567" or "971501234567" becomes "501234567"
							var phoneNumber = ShoppingCartVM.OrderHeader.PhoneNumber.Trim().Replace("+", "").Replace("-", "").Replace(" ", "");
							
							// Remove country code if present - Tamara wants local format
							if (phoneNumber.StartsWith("971"))
							{
								phoneNumber = phoneNumber.Substring(3); // Remove 971 prefix
							}
							// Remove leading 0 if present
							else if (phoneNumber.StartsWith("0"))
							{
								phoneNumber = phoneNumber.Substring(1);
							}
							
							// Validate phone number length (should be 9 digits for UAE)
							if (phoneNumber.Length != 9)
							{
								_logger.LogWarning($"Invalid UAE phone number length: {phoneNumber.Length}. Phone: {ShoppingCartVM.OrderHeader.PhoneNumber}");
							}
							
							// Get email - ensure it's not empty
							var customerEmail = ShoppingCartVM.OrderHeader.Email;
					
					// If email not in OrderHeader, try to get from current user directly
					if (string.IsNullOrWhiteSpace(customerEmail))
					{
						if (User.Identity.IsAuthenticated)
						{
							var claimsIdentity = (ClaimsIdentity)User.Identity;
							 userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
							
							// Query user directly from AspNetUsers (works for both Google and email users)
							var allUsers = _unitOfWork.applicationUser.GetAllUsersWithoutDiscriminator();
							var currentUser = allUsers.FirstOrDefault(u => u.Id == userId);
							
							if (currentUser != null)
							{
								customerEmail = currentUser.Email;
							}
						}
						else if (ShoppingCartVM.OrderHeader.ApplicationUser != null)
							{
								customerEmail = ShoppingCartVM.OrderHeader.ApplicationUser.Email;
							}
					}
					
							if (string.IsNullOrWhiteSpace(customerEmail))
							{
								TempData["error"] = "Email address is required for Tamara payments";
								return RedirectToAction("Summary");
							}
							
							// Determine locale based on current culture
							var requestCulture = HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
							var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
							var locale = currentCulture.StartsWith("ar") ? "ar_AE" : "en_US";
							
							// Get currency and country from settings or defaults
							var currency = _tamaraSettings.Currency ?? "AED";
							var countryCode = _tamaraSettings.CountryCode ?? "AE";
							
							// Ensure postal code is provided (some regions require it)
							var postalCode = ShoppingCartVM.OrderHeader.PostalCode;
							if (string.IsNullOrWhiteSpace(postalCode))
							{
								postalCode = "00000"; // Default for regions without postal codes
							}
							
							// Calculate discount amount and name if promo code applied
							TamaraDiscount discountInfo = null;
							if (ShoppingCartVM.OrderHeader.PromoCodeId.HasValue && ShoppingCartVM.OrderHeader.PromoCodeId.Value > 0)
							{
								var originalTotal = ShoppingCartVM.ShoppingCartList.Sum(item => item.Price * item.Count);
								var discountAmount = (decimal)(originalTotal - ShoppingCartVM.OrderHeader.OrderTotal);
								
								if (discountAmount > 0)
								{
									var promoCode = _unitOfWork.PromoCode.Get(p => p.Id == ShoppingCartVM.OrderHeader.PromoCodeId.Value);
									discountInfo = new TamaraDiscount
									{
										Name = promoCode?.Code ?? "Discount",
										Amount = new TamaraAmount
										{
											Amount = discountAmount,
											Currency = currency
										}
									};
								}
							}
							
							_logger.LogInformation($"Creating Tamara checkout for order {ShoppingCartVM.OrderHeader.Id}, amount: {ShoppingCartVM.OrderHeader.OrderTotal} {currency}");
							
							var tamaraRequest = new TamaraPaymentRequest
							{
								OrderReferenceId = ShoppingCartVM.OrderHeader.Id.ToString(),
								OrderNumber = $"ORD-{ShoppingCartVM.OrderHeader.Id}",
								TotalAmount = new TamaraAmount
								{
									Amount = (decimal)ShoppingCartVM.OrderHeader.OrderTotal,
									Currency = currency
								},
								Description = $"Order {ShoppingCartVM.OrderHeader.Id}",
								CountryCode = countryCode,
								PaymentType = "PAY_BY_INSTALMENTS",
								Instalments = null, // Let Tamara decide based on amount
								Locale = locale,
								Platform = "ASP.NET Core MVC",
								IsMobile = false,
								MerchantUrl = new TamaraMerchantUrl
								{
									Success = domain + $"customer/cart/TamaraCallback?orderId={ShoppingCartVM.OrderHeader.Id}&status=success",
									Failure = domain + $"customer/cart/TamaraCallback?orderId={ShoppingCartVM.OrderHeader.Id}&status=failure",
									Cancel = domain + "customer/cart/index",
									Notification = "" // Empty notification URL as required for checkout creation
								},
								Consumer = new TamaraConsumer
								{
									FirstName = firstName,
									LastName = lastName,
									PhoneNumber = phoneNumber,
									Email = customerEmail
								},
								BillingAddress = new TamaraAddress
								{
									FirstName = firstName,
									LastName = lastName,
									Line1 = ShoppingCartVM.OrderHeader.StreetAddress,
									Line2 = null,
									City = ShoppingCartVM.OrderHeader.City,
									Region = ShoppingCartVM.OrderHeader.State,
									PostalCode = postalCode,
									CountryCode = countryCode,
									PhoneNumber = phoneNumber
								},
								ShippingAddress = new TamaraAddress
								{
									FirstName = firstName,
									LastName = lastName,
									Line1 = ShoppingCartVM.OrderHeader.StreetAddress,
									Line2 = null,
									City = ShoppingCartVM.OrderHeader.City,
									Region = ShoppingCartVM.OrderHeader.State,
									PostalCode = postalCode,
									CountryCode = countryCode,
									PhoneNumber = phoneNumber
								},
								Items = ShoppingCartVM.ShoppingCartList.Select(item => new TamaraItem
								{
									ReferenceId = item.ProductId.ToString(),
									Type = "Physical",
									Name = item.product.Title?.Length > 200 ? item.product.Title.Substring(0, 200) : item.product.Title,
									Sku = item.ProductId.ToString(),
									Quantity = item.Count,
									UnitPrice = new TamaraAmount
									{
										Amount = (decimal)item.Price,
										Currency = currency
									},
									TotalAmount = new TamaraAmount
									{
										Amount = (decimal)(item.Price * item.Count),
										Currency = currency
									},
									DiscountAmount = new TamaraAmount
									{
										Amount = 0,
										Currency = currency
									},
									TaxAmount = new TamaraAmount
									{
										Amount = 0,
										Currency = currency
									}
								}).ToList(),
								TaxAmount = new TamaraAmount
								{
									Amount = 0,
									Currency = currency
								},
								ShippingAmount = new TamaraAmount
								{
									Amount = 0, // Free shipping or add your shipping cost
									Currency = currency
								},
								Discount = discountInfo
							};

							_logger.LogInformation($"Sending Tamara request - Order: {ShoppingCartVM.OrderHeader.Id}, Amount: {ShoppingCartVM.OrderHeader.OrderTotal}, Phone: {phoneNumber}, Email: {customerEmail}");
							
							var tamaraResponse = await tamaraHelper.CreateCheckoutAsync(tamaraRequest);
							
							_logger.LogInformation($"Tamara response - Success: {tamaraResponse.Success}, Message: {tamaraResponse.Message}");
							
							if (tamaraResponse.Success && !string.IsNullOrEmpty(tamaraResponse.CheckoutUrl))
							{
								// Store Tamara checkout ID and order ID
								ShoppingCartVM.OrderHeader.SessionId = tamaraResponse.CheckoutId;
								ShoppingCartVM.OrderHeader.PaymentIntentId = tamaraResponse.OrderId;
								_unitOfWork.OrderHeader.Update(ShoppingCartVM.OrderHeader);
								_unitOfWork.save();
								
								_logger.LogInformation($"Redirecting to Tamara checkout: {tamaraResponse.CheckoutUrl}");
								
								// Redirect to Tamara checkout page
								Response.Headers.Add("Location", tamaraResponse.CheckoutUrl);
								return new StatusCodeResult(303);
							}
							else
							{
								_logger.LogError($"Tamara checkout creation failed for order {ShoppingCartVM.OrderHeader.Id}: {tamaraResponse.Message}");
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
				_logger.LogWarning($"GeideaCallback: Order {orderId} not found");
				return BadRequest(new { status = "error", message = "Order not found" });
			}

			try
			{
				// Read request body to log the callback data
				string requestBody = "";
				try
				{
					Request.Body.Position = 0;
					using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
					{
						requestBody = await reader.ReadToEndAsync();
					}
					Request.Body.Position = 0;
				}
				catch { /* Ignore if body cannot be read */ }

				_logger.LogInformation($"GeideaCallback received for order {orderId}. Request body: {requestBody}");

				if (!string.IsNullOrEmpty(orderHeader.SessionId))
				{
					var geideaHelper = new GeideaHelper(_geideaSettings);
					// Use order ID (merchant reference ID) for verification, not session ID
					var verificationResponse = await geideaHelper.VerifyPaymentAsync(orderHeader.Id.ToString());

					if (verificationResponse.Success)
					{
						if (verificationResponse.IsPaid)
						{
							// Payment successful
							orderHeader.PaymentStatus = SD.PaymentStatusPaid;
							orderHeader.OrderStatus = SD.StatusPaid;
							if (!string.IsNullOrEmpty(orderHeader.SessionId))
							{
								orderHeader.PaymentIntentId = orderHeader.SessionId;
							}
							orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
							
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							_logger.LogInformation($"GeideaCallback: Order {orderId} payment verified successfully. Status: {verificationResponse.Status}");
							return Ok(new { status = "success", message = "Payment verified and order updated" });
						}
						else
						{
							// Payment verification succeeded but payment is not paid
							string errorMessage = verificationResponse.Message ?? $"Payment status: {verificationResponse.Status ?? "Unknown"}";
							_logger.LogWarning($"GeideaCallback: Order {orderId} payment verification succeeded but payment is not paid. Status: {verificationResponse.Status}, Message: {errorMessage}");
							
							// Update order status to reflect payment failure
							orderHeader.PaymentStatus = SD.PaymentStatusRejected;
							orderHeader.OrderStatus = SD.StatusCancelled;
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							return Ok(new { 
								status = "failed", 
								message = errorMessage,
								paymentStatus = verificationResponse.Status,
								details = $"Payment verification completed but payment was not successful. Status: {verificationResponse.Status}"
							});
						}
					}
					else
					{
						// Payment verification failed
						string errorMessage = verificationResponse.Message ?? "Payment verification failed";
						_logger.LogError($"GeideaCallback: Order {orderId} payment verification failed. Error: {errorMessage}");
						
						// Update order status to reflect payment failure
						orderHeader.PaymentStatus = SD.PaymentStatusRejected;
						orderHeader.OrderStatus = SD.StatusCancelled;
						_unitOfWork.OrderHeader.Update(orderHeader);
						_unitOfWork.save();
						
						return Ok(new { 
							status = "error", 
							message = errorMessage,
							details = "Payment verification failed. Please check the payment status in Geidea dashboard."
						});
					}
				}
				else
				{
					_logger.LogWarning($"GeideaCallback: Order {orderId} has no SessionId");
					return Ok(new { status = "warning", message = "Order has no payment session ID" });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"GeideaCallback: Exception processing callback for order {orderId}");
				return Ok(new { 
					status = "error", 
					message = $"Error processing callback: {ex.Message}",
					details = ex.ToString()
				});
			}
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
				orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
				orderHeader.PaymentStatus = SD.StatusPaid;
				orderHeader.OrderStatus = SD.PaymentStatusPaid;
                _unitOfWork.OrderHeader.Update(orderHeader);
				_unitOfWork.save();
				
				//await _stockService.ProcessOrderStockDeduction(orderId);

				return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
			}
			else
			{
				TempData["error"] = "Payment verification failed. Please contact support with your order ID: " + orderId;
				return RedirectToAction("Index", "Home");
			}
		}

		 
		/// <summary>
		/// Dedicated webhook endpoint for Tamara notifications
		/// URL: /customer/cart/TamaraWebhook
		/// This endpoint should be configured in Tamara dashboard as the webhook URL
		/// </summary>
		[HttpPost]
		[AllowAnonymous]
		[Route("/customer/cart/TamaraWebhook")]
		public async Task<IActionResult> TamaraWebhook()
		{
			// Log that endpoint was hit (even before reading body)
			_logger.LogInformation("========== TAMARA WEBHOOK ENDPOINT HIT ==========");
			_logger.LogInformation($"Request Method: {Request.Method}");
			_logger.LogInformation($"Request Path: {Request.Path}");
			_logger.LogInformation($"Request Headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");
			
			try
			{
				// Read the raw request body
				using var reader = new StreamReader(Request.Body);
				var body = await reader.ReadToEndAsync();
				
				_logger.LogInformation($"========== TAMARA WEBHOOK BODY RECEIVED ==========");
				_logger.LogInformation($"Body Content: {body}");
				_logger.LogInformation($"Body Length: {body?.Length ?? 0} characters");
				
				// Parse the notification
				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				};
				
				var notification = JsonSerializer.Deserialize<TamaraNotificationPayload>(body, options);
				
				if (notification == null || string.IsNullOrEmpty(notification.OrderId))
				{
					_logger.LogWarning("Invalid Tamara webhook payload at TamaraWebhook endpoint");
					// Always return HTTP 200 for webhooks, even on errors
					return Ok(new { success = false, message = "Invalid payload - order_id is required" });
				}
				
				// Verify notification token if configured
				var authHeader = Request.Headers["Authorization"].ToString();
				if (!string.IsNullOrEmpty(_tamaraSettings.NotificationToken))
				{
					// Tamara sends JWT token in Authorization header, not the static NotificationToken
					// For now, we'll verify that the Authorization header exists and starts with "Bearer"
					// The JWT token is signed by Tamara and validated by their system
					if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
					{
						_logger.LogWarning("Missing or invalid Authorization header format at TamaraWebhook endpoint");
						// Always return HTTP 200 for webhooks, even on errors
						return Ok(new { success = false, message = "Invalid authorization header" });
					}
					
					// Extract token from Bearer header
					var token = authHeader.Substring("Bearer ".Length).Trim();
					if (string.IsNullOrEmpty(token))
					{
						_logger.LogWarning("Empty token in Authorization header at TamaraWebhook endpoint");
						// Always return HTTP 200 for webhooks, even on errors
						return Ok(new { success = false, message = "Empty authorization token" });
					}
					
					_logger.LogInformation($"Tamara webhook Authorization token received (length: {token.Length})");
				}
				
				// Find the order by Tamara order ID (stored in PaymentIntentId)
				// Get order ID first to avoid tracking issues
				var orderId = _unitOfWork.OrderHeader.Get(o => o.PaymentIntentId == notification.OrderId)?.Id;
				
				if (!orderId.HasValue || orderId.Value <= 0)
				{
					_logger.LogWarning($"Order not found for Tamara order ID in webhook: {notification.OrderId}");
					// Always return HTTP 200 for webhooks, even if order not found
					return Ok(new { success = false, message = "Order not found", orderId = notification.OrderId });
				}
				
				// Log the notification payload for debugging
				_logger.LogInformation($"Tamara webhook payload - OrderId: {notification.OrderId}, OrderReferenceId: {notification.OrderReferenceId}, OrderStatus: {notification.OrderStatus}, PaymentStatus: {notification.PaymentStatus}");
				
				// Check notification payload status first (this is what Tamara sends)
				var notificationStatusLower = notification.OrderStatus?.ToLower() ?? "";
				var notificationPaymentStatusLower = notification.PaymentStatus?.ToLower() ?? "";
				
				_logger.LogInformation($"Processing Tamara webhook at TamaraWebhook: OrderId={notification.OrderId}, NotificationStatus={notification.OrderStatus}, NotificationPaymentStatus={notification.PaymentStatus}");
				
				// Get order details from Tamara API to verify current status
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				var orderDetails = await tamaraHelper.GetOrderDetailsAsync(notification.OrderId);
				
				if (orderDetails.Success)
				{
					_logger.LogInformation($"Tamara API order status: {orderDetails.Status}, payment status: {orderDetails.PaymentStatus}");
				}
				
				// Handle approved status - ALWAYS authorize when we receive approved status
				// According to Tamara requirements: When order is approved, we MUST authorize it via API
				// Check both notification payload and API response
				var isApproved = notificationStatusLower.Contains("approved") || 
				                 notificationPaymentStatusLower.Contains("approved") ||
				                 (orderDetails.Success && (orderDetails.Status?.ToLower().Contains("approved") == true || 
				                                           orderDetails.PaymentStatus?.ToLower().Contains("approved") == true));
				
				if (isApproved)
				{
					_logger.LogInformation($"Tamara order {notification.OrderId} is in APPROVED status - attempting authorization");
					
					// IMPORTANT: Always try to authorize when order is approved
					// Even if order is already marked as paid in our DB, we need to authorize in Tamara
					var authResponse = await tamaraHelper.AuthorizeOrderAsync(notification.OrderId);
					
					if (authResponse.Success)
					{
						_logger.LogInformation($"Tamara order {notification.OrderId} successfully authorized via webhook at TamaraWebhook. Authorization status: {authResponse.Status}");
						
						// Re-fetch order header after async operations to avoid DbContext concurrency issues
						var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
						
						if (orderHeader == null)
						{
							_logger.LogError($"Order {orderId.Value} not found after authorization - this should not happen");
							return Ok(new { 
								success = true, 
								message = "Order authorized successfully but could not update local status",
								orderId = notification.OrderId,
								authorizationStatus = authResponse.Status
							});
						}
						
						// Update order status only if not already paid
						if (orderHeader.PaymentStatus != SD.PaymentStatusPaid)
						{
							// Update PaymentDate first, then use UpdateStatus which saves automatically
							orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
							_unitOfWork.OrderHeader.Update(orderHeader);
							await _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusPaid);
							
							// Process stock deduction when payment is authorized
							//await _stockService.ProcessOrderStockDeduction(orderHeader.Id);
							
							_logger.LogInformation($"Tamara order {notification.OrderId} marked as authorized/paid via webhook");
						}
						else
						{
							_logger.LogInformation($"Tamara order {notification.OrderId} already marked as paid in database, but authorization was successful in Tamara");
						}
						
						// Return HTTP 200 as required by Tamara
						return Ok(new { 
							success = true, 
							message = "Order authorized successfully",
							orderId = notification.OrderId,
							authorizationStatus = authResponse.Status
						});
					}
					else
					{
						_logger.LogError($"Tamara order {notification.OrderId} authorization failed in webhook at TamaraWebhook: {authResponse.Message}");
						
						// Check if order is already authorized (might return error but order is actually authorized)
						if (orderDetails.Success)
						{
							var apiStatusLower = orderDetails.Status?.ToLower() ?? "";
							if (apiStatusLower.Contains("authorised") || apiStatusLower.Contains("authorized"))
							{
								_logger.LogInformation($"Tamara order {notification.OrderId} is already authorized according to API, updating local status");
								
								// Re-fetch order header after async operations to avoid DbContext concurrency issues
								var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
								
								if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusPaid)
								{
									// Update PaymentDate first, then use UpdateStatus which saves automatically
									orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
									_unitOfWork.OrderHeader.Update(orderHeader);
									await _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusPaid);
									//await _stockService.ProcessOrderStockDeduction(orderHeader.Id);
								}
								
								return Ok(new { 
									success = true, 
									message = "Order already authorized",
									orderId = notification.OrderId
								});
							}
						}
						
						// Still return 200 to acknowledge webhook receipt, but log the error
						return Ok(new { 
							success = false, 
							message = authResponse.Message,
							orderId = notification.OrderId
						});
					}
				}
				
				// Handle other statuses (captured, cancelled, etc.)
				if (orderDetails.Success)
				{
					var statusLower = orderDetails.Status?.ToLower() ?? "";
					var paymentStatusLower = orderDetails.PaymentStatus?.ToLower() ?? "";
					
					// Handle captured status (payment captured - order can be shipped)
					if (statusLower.Contains("captured") || paymentStatusLower.Contains("captured"))
					{
						// Re-fetch order header after async operations to avoid DbContext concurrency issues
						var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
						
						if (orderHeader != null && 
						    orderHeader.PaymentStatus == SD.PaymentStatusPaid && 
						    orderHeader.OrderStatus != SD.StatusShipped &&
						    orderHeader.OrderStatus != SD.StatusDelivered)
						{
							// Order is captured, ready to ship
							_logger.LogInformation($"Tamara order {notification.OrderId} captured, ready to ship");
						}
						return Ok(new { success = true, message = "Order captured", orderId = notification.OrderId });
					}
					// Handle cancelled status
					if (statusLower.Contains("cancelled") || statusLower.Contains("canceled"))
					{
						// Re-fetch order header after async operations to avoid DbContext concurrency issues
						var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
						
						if (orderHeader != null && orderHeader.OrderStatus != SD.StatusCancelled)
						{
							// UpdateStatus saves automatically, no need for separate save()
							await _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRefunded);
							_logger.LogInformation($"Tamara order {notification.OrderId} cancelled via webhook");
						}
						// Always return HTTP 200 for webhooks
						return Ok(new { success = true, message = "Order cancelled", orderId = notification.OrderId });
					}
					
					_logger.LogInformation($"Tamara order {notification.OrderId} status update received: {orderDetails.Status}");
					return Ok(new { success = true, message = "Status updated", orderId = notification.OrderId, status = orderDetails.Status });
				}
				else
				{
					_logger.LogWarning($"Failed to get order details for Tamara order {notification.OrderId}: {orderDetails.Message}");
					
					// Even if we can't get order details, if notification says approved, try to authorize
					if (isApproved)
					{
						_logger.LogInformation($"Cannot get order details, but notification indicates approved status. Attempting authorization anyway.");
						var authResponse = await tamaraHelper.AuthorizeOrderAsync(notification.OrderId);
						
						if (authResponse.Success)
						{
							// Re-fetch order header after async operations to avoid DbContext concurrency issues
							var orderHeader = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
							
							if (orderHeader != null && orderHeader.PaymentStatus != SD.PaymentStatusPaid)
							{
								// Update PaymentDate first, then use UpdateStatus which saves automatically
								orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
								_unitOfWork.OrderHeader.Update(orderHeader);
								await _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusPaid);
								//await _stockService.ProcessOrderStockDeduction(orderHeader.Id);
							}
							
							return Ok(new { 
								success = true, 
								message = "Order authorized successfully (without order details)",
								orderId = notification.OrderId
							});
						}
					}
					
					return Ok(new { 
						success = false, 
						message = orderDetails.Message,
						orderId = notification.OrderId
					});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing Tamara webhook at TamaraWebhook endpoint");
				// Return 200 to acknowledge receipt even on error (to prevent retries)
				return Ok(new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// GET callback endpoint for Tamara payment redirects (after customer completes payment)
		/// This is NOT the webhook endpoint - use TamaraWebhook for webhooks
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> TamaraCallback(int? orderId = null, string status = "", string order_id = "", string tamara_order_id = "")
		{
			// This endpoint only handles GET requests (redirects from Tamara after payment)
			// For webhooks, use the TamaraWebhook endpoint
			
			if (orderId == null)
			{
				TempData["error"] = "Order ID is required";
				return RedirectToAction("Index", "Home");
			}
			
			var orderHeaderRedirect = _unitOfWork.OrderHeader.Get(o => o.Id == orderId.Value);
			
			if (orderHeaderRedirect == null)
			{
				TempData["error"] = "Order not found";
				return RedirectToAction("Index", "Home");
			}
			
			// Check if Tamara sent the OrderId in the callback URL parameters
			var tamaraOrderIdFromCallback = !string.IsNullOrEmpty(tamara_order_id) 
				? tamara_order_id 
				: (!string.IsNullOrEmpty(order_id) ? order_id : null);
			
			if (!string.IsNullOrEmpty(tamaraOrderIdFromCallback))
			{
				_logger?.LogInformation("Tamara OrderId received in callback URL: {TamaraOrderId} for order {OrderId}", 
					tamaraOrderIdFromCallback, orderId);
				
				// Update PaymentIntentId if it's different or missing
				if (orderHeaderRedirect.PaymentIntentId != tamaraOrderIdFromCallback)
				{
					orderHeaderRedirect.PaymentIntentId = tamaraOrderIdFromCallback;
					_unitOfWork.OrderHeader.Update(orderHeaderRedirect);
					_unitOfWork.save();
					_logger?.LogInformation("Updated PaymentIntentId with Tamara OrderId from callback: {TamaraOrderId}", tamaraOrderIdFromCallback);
				}
			}

			if (status == "success")
			{
			try
			{
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				string tamaraOrderId = null;
				
				// Priority 1: Use OrderId from callback URL (most reliable)
				if (!string.IsNullOrEmpty(tamaraOrderIdFromCallback))
				{
					tamaraOrderId = tamaraOrderIdFromCallback;
					_logger?.LogInformation("Using Tamara OrderId from callback URL: {OrderId}", tamaraOrderId);
				}
				// Priority 2: Try to get the actual Tamara OrderId using the CheckoutId (SessionId)
				// The OrderId might not be available immediately after checkout creation
				else if (!string.IsNullOrEmpty(orderHeaderRedirect.SessionId))
				{
					_logger?.LogInformation("Attempting to get Tamara order details using CheckoutId: {CheckoutId}", orderHeaderRedirect.SessionId);
					
					// Try to get order details using CheckoutId first
					// Note: Some Tamara SDKs might support getting order by CheckoutId
					// If not, we'll fall back to using PaymentIntentId
					var orderDetailsByCheckout = await tamaraHelper.GetOrderDetailsAsync(orderHeaderRedirect.SessionId);
					
					if (orderDetailsByCheckout.Success && !string.IsNullOrEmpty(orderDetailsByCheckout.OrderId))
					{
						tamaraOrderId = orderDetailsByCheckout.OrderId;
						_logger?.LogInformation("Retrieved Tamara OrderId from CheckoutId: {OrderId}", tamaraOrderId);
					}
				}
				
				// Fallback to PaymentIntentId if we have it and didn't get OrderId from CheckoutId
				if (string.IsNullOrEmpty(tamaraOrderId) && !string.IsNullOrEmpty(orderHeaderRedirect.PaymentIntentId))
				{
					tamaraOrderId = orderHeaderRedirect.PaymentIntentId;
					_logger?.LogInformation("Using stored PaymentIntentId as Tamara OrderId: {OrderId}", tamaraOrderId);
				}
				
				// Last resort: use SessionId (CheckoutId) if nothing else works
				if (string.IsNullOrEmpty(tamaraOrderId) && !string.IsNullOrEmpty(orderHeaderRedirect.SessionId))
				{
					tamaraOrderId = orderHeaderRedirect.SessionId;
					_logger?.LogWarning("Falling back to SessionId (CheckoutId) as Tamara OrderId: {OrderId}", tamaraOrderId);
				}
				
				if (string.IsNullOrEmpty(tamaraOrderId))
				{
					_logger?.LogError("No Tamara order identifier found for order {OrderId}. SessionId: {SessionId}, PaymentIntentId: {PaymentIntentId}", 
						orderId, orderHeaderRedirect.SessionId, orderHeaderRedirect.PaymentIntentId);
					TempData["error"] = "Unable to find Tamara payment information. Please contact support with your order ID: " + orderId;
					return RedirectToAction("Index", "Home");
				}
				
				// First, try to get order details to check the current status
				// Tamara may auto-authorize orders after payment, so we check status first
				_logger?.LogInformation("Checking Tamara order status: {TamaraOrderId}", tamaraOrderId);
				var orderDetails = await tamaraHelper.GetOrderDetailsAsync(tamaraOrderId);
				
				if (!orderDetails.Success)
				{
					_logger?.LogWarning("Failed to get Tamara order details: {Message}", orderDetails?.Message);
					// Try to authorize anyway if we have the order ID
					var authResponse = await tamaraHelper.AuthorizeOrderAsync(tamaraOrderId);
					if (authResponse.Success)
					{
						_logger?.LogInformation("Order authorized successfully via callback");
						// Update order status
						orderHeaderRedirect.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
						await _unitOfWork.OrderHeader.UpdateStatus(orderHeaderRedirect.Id, SD.StatusApproved, SD.PaymentStatusPaid);
						return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
					}
					else
					{
						var errorMsg = $"Payment verification failed: {orderDetails?.Message ?? "Unable to verify payment status"}";
						_logger?.LogWarning("Tamara payment verification failed for order {OrderId}: {Error}", orderId, errorMsg);
						TempData["error"] = $"{errorMsg} Please contact support with your order ID: {orderId}";
						return RedirectToAction("Index", "Home");
					}
				}
				
				// Check order status - Tamara may return "approved" or "authorised"
				var statusLower = orderDetails.Status?.ToLower() ?? "";
				var paymentStatusLower = orderDetails.PaymentStatus?.ToLower() ?? "";
				var isApproved = statusLower.Contains("approved") || statusLower.Contains("authorised") ||
				                 paymentStatusLower.Contains("approved") || paymentStatusLower.Contains("authorised");
				
				_logger?.LogInformation("Tamara order status: {Status}, PaymentStatus: {PaymentStatus}, IsApproved: {IsApproved}", 
					orderDetails.Status, orderDetails.PaymentStatus, isApproved);
				
				if (isApproved)
				{
					// Update PaymentIntentId with the confirmed Tamara OrderId if it changed
					if (!string.IsNullOrEmpty(orderDetails.OrderId) && orderDetails.OrderId != orderHeaderRedirect.PaymentIntentId)
					{
						orderHeaderRedirect.PaymentIntentId = orderDetails.OrderId;
						_unitOfWork.OrderHeader.Update(orderHeaderRedirect);
						_unitOfWork.save();
						_logger?.LogInformation("Updated PaymentIntentId with confirmed Tamara OrderId: {OrderId}", orderDetails.OrderId);
					}
					
					// If order is approved but not yet authorized, try to authorize it
					if (!statusLower.Contains("authorised") && !paymentStatusLower.Contains("authorised"))
					{
						_logger?.LogInformation("Order is approved but not yet authorized. Attempting authorization...");
						var authResponse = await tamaraHelper.AuthorizeOrderAsync(tamaraOrderId);
						if (authResponse.Success)
						{
							_logger?.LogInformation("Order successfully authorized via callback");
						}
						else
						{
							_logger?.LogWarning("Authorization attempt failed: {Message}", authResponse.Message);
						}
					}
					
					// Update order status
					orderHeaderRedirect.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
					await _unitOfWork.OrderHeader.UpdateStatus(orderHeaderRedirect.Id, SD.StatusApproved, SD.PaymentStatusPaid);
					
					_logger?.LogInformation("Tamara order {OrderId} marked as paid via callback", orderId);
					return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
				}
				else
				{
					// Order is not approved yet - try to authorize if it's in pending/created status
					if (statusLower.Contains("pending") || statusLower.Contains("created") || 
					    paymentStatusLower.Contains("pending") || paymentStatusLower.Contains("created"))
					{
						_logger?.LogInformation("Order is in pending/created status. Attempting authorization...");
						var authResponse = await tamaraHelper.AuthorizeOrderAsync(tamaraOrderId);
						if (authResponse.Success)
						{
							_logger?.LogInformation("Order successfully authorized via callback");
							// Update order status
							orderHeaderRedirect.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
							await _unitOfWork.OrderHeader.UpdateStatus(orderHeaderRedirect.Id, SD.StatusApproved, SD.PaymentStatusPaid);
							return RedirectToAction(nameof(OrderConfirmation), new { id = orderId });
						}
					}
					
					// Order details retrieval failed or payment not approved
					var errorMsg = !string.IsNullOrEmpty(orderDetails?.Message) 
						? $"Payment verification failed: {orderDetails.Message}" 
						: $"Payment verification failed. Payment status: {orderDetails.PaymentStatus}, Order status: {orderDetails.Status}";
					
					_logger?.LogWarning("Tamara payment verification failed for order {OrderId}: {Error}", orderId, errorMsg);
					TempData["error"] = $"{errorMsg} Please contact support with your order ID: {orderId}";
					return RedirectToAction("Index", "Home");
				}
			 
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Unexpected error during Tamara payment authorization for order {OrderId}", orderId);
				TempData["error"] = $"An unexpected error occurred during payment processing. Please contact support with your order ID: {orderId}";
				return RedirectToAction("Index", "Home");
			}
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
			try
			{
				// Read the raw request body
				using var reader = new StreamReader(Request.Body);
				var body = await reader.ReadToEndAsync();
				
				_logger.LogInformation($"Tamara Notification received: {body}");
				
				// Parse the notification
				var options = new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				};
				
				var notification = JsonSerializer.Deserialize<TamaraNotificationPayload>(body, options);
				
				if (notification == null || string.IsNullOrEmpty(notification.OrderId))
				{
					_logger.LogWarning("Invalid Tamara notification payload");
					return BadRequest("Invalid payload");
				}
				
				// Verify notification token if configured
				var authHeader = Request.Headers["Authorization"].ToString();
				if (!string.IsNullOrEmpty(_tamaraSettings.NotificationToken))
				{
					if (authHeader != $"Bearer {_tamaraSettings.NotificationToken}")
					{
						_logger.LogWarning("Invalid Tamara notification token");
						return Unauthorized();
					}
				}
				
				// Find the order by Tamara order ID (stored in PaymentIntentId)
				var orderHeader = _unitOfWork.OrderHeader.Get(o => o.PaymentIntentId == notification.OrderId);
				
				if (orderHeader == null)
				{
					_logger.LogWarning($"Order not found for Tamara order ID: {notification.OrderId}");
					return NotFound("Order not found");
				}
				
				// Update order status based on notification
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				var orderDetails = await tamaraHelper.GetOrderDetailsAsync(notification.OrderId);
				
				if (orderDetails.Success)
				{
					_logger.LogInformation($"Tamara order {notification.OrderId} status: {orderDetails.Status}, payment status: {orderDetails.PaymentStatus}");
					
					// Update order based on Tamara status
					// Tamara webhook is the source of truth for payment status
					var statusLower = orderDetails.Status?.ToLower() ?? "";
					var paymentStatusLower = orderDetails.PaymentStatus?.ToLower() ?? "";
					
					_logger.LogInformation($"Processing Tamara webhook: OrderId={notification.OrderId}, Status={orderDetails.Status}, PaymentStatus={orderDetails.PaymentStatus}");
					
					// Handle authorized/approved status (payment authorized but not captured)
					// According to Tamara requirements: When order is approved, we must authorize it via API
					if (statusLower.Contains("approved") || statusLower.Contains("authorised") || statusLower.Contains("authorized"))
					{
						if (orderHeader.PaymentStatus != SD.PaymentStatusPaid && 
						    orderHeader.OrderStatus != SD.StatusCancelled)
						{
							// IMPORTANT: Authorize the order via Tamara API as required
							// This is required by Tamara - when order is approved, we must authorize it
							var authResponse = await tamaraHelper.AuthorizeOrderAsync(notification.OrderId);
							
							if (authResponse.Success)
							{
								_logger.LogInformation($"Tamara order {notification.OrderId} successfully authorized via webhook");
								
								// After successful authorization, mark as paid
								_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusPaid);
								orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
								_unitOfWork.OrderHeader.Update(orderHeader);
								_unitOfWork.save();
								
								// Process stock deduction when payment is authorized
								//await _stockService.ProcessOrderStockDeduction(orderHeader.Id);
								
								_logger.LogInformation($"Tamara order {notification.OrderId} marked as authorized/paid");
							}
							else
							{
								_logger.LogError($"Tamara order {notification.OrderId} authorization failed in webhook: {authResponse.Message}");
								// Still return 200 to acknowledge webhook receipt, but log the error
							}
						}
					}
					// Handle captured status (payment captured - order can be shipped)
					else if (statusLower.Contains("captured") || paymentStatusLower.Contains("captured"))
					{
						if (orderHeader.PaymentStatus == SD.PaymentStatusPaid && 
						    orderHeader.OrderStatus != SD.StatusShipped &&
						    orderHeader.OrderStatus != SD.StatusDelivered)
						{
							// Payment captured - ready for shipping
							// Don't change order status to Shipped automatically - admin will do that
							// Just ensure payment status is correct
							orderHeader.PaymentStatus = SD.PaymentStatusPaid;
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							_logger.LogInformation($"Tamara order {notification.OrderId} payment captured");
						}
					}
					// Handle cancelled status
					else if (statusLower.Contains("canceled") || statusLower.Contains("cancelled"))
					{
						if (orderHeader.OrderStatus != SD.StatusCancelled)
						{
							_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusCancelled);
							_unitOfWork.save();
							
							_logger.LogInformation($"Tamara order {notification.OrderId} cancelled via webhook");
						}
					}
					// Handle declined/rejected status
					else if (statusLower.Contains("declined") || statusLower.Contains("rejected"))
					{
						if (orderHeader.OrderStatus != SD.StatusCancelled)
						{
							_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.PaymentStatusRejected);
							_unitOfWork.save();
							
							_logger.LogInformation($"Tamara order {notification.OrderId} declined/rejected via webhook");
						}
					}
					// Handle refunded status
					else if (statusLower.Contains("refunded") || paymentStatusLower.Contains("refunded"))
					{
						// Check if full or partial refund
						var isFullRefund = statusLower.Contains("fully_refunded") || 
						                  paymentStatusLower.Contains("fully_refunded");
						
						if (isFullRefund)
						{
							_unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusRefunded, SD.PaymentStatusRefunded);
							_unitOfWork.save();
							
							_logger.LogInformation($"Tamara order {notification.OrderId} fully refunded via webhook");
						}
						else
						{
							// Partial refund
							orderHeader.PaymentStatus = SD.PaymentStatusPartiallyRefunded;
							_unitOfWork.OrderHeader.Update(orderHeader);
							_unitOfWork.save();
							
							_logger.LogInformation($"Tamara order {notification.OrderId} partially refunded via webhook");
						}
					}
					else
					{
						_logger.LogWarning($"Tamara webhook received unknown status: {orderDetails.Status} for order {notification.OrderId}");
					}
				}
				
				return Ok();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing Tamara notification");
				return StatusCode(500, "Internal server error");
			}
		}
		
		// Tamara notification payload model
		public class TamaraNotificationPayload
		{
			[JsonPropertyName("order_id")]
			public string OrderId { get; set; }
			
			[JsonPropertyName("order_reference_id")]
			public string OrderReferenceId { get; set; }
			
			[JsonPropertyName("order_status")]
			public string OrderStatus { get; set; }
			
			[JsonPropertyName("payment_status")]
			public string PaymentStatus { get; set; }
		}
		
		// Test endpoint to verify Tamara configuration
		[HttpGet]
		public async Task<IActionResult> TestTamaraConnection()
		{
			try
			{
				var tamaraHelper = new TamaraHelper(_tamaraSettings);
				var isAvailable = await tamaraHelper.IsPaymentAvailableAsync(500, "AE");
				
				return Json(new
				{
					success = true,
					enabled = _tamaraSettings.Enabled,
					useSandbox = _tamaraSettings.UseSandbox,
					baseUrl = _tamaraSettings.BaseUrl,
					paymentAvailable = isAvailable,
					message = "Tamara connection test successful"
				});
			}
			catch (Exception ex)
			{
				return Json(new
				{
					success = false,
					message = $"Tamara connection test failed: {ex.Message}"
				});
			}
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
							orderHeader.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
							
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
				IdealWeightNutrition.Utility.GuestCartHelper.ClearCart(HttpContext.Session);
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
            // Ensure product is loaded
            if (shoppingCart.product == null)
            {
                // Load product if not already loaded
                shoppingCart.product = _unitOfWork.product.Get(p => p.Id == shoppingCart.ProductId);
            }
            
            // Return product price, or 0 if product is still null
            if (shoppingCart.product == null)
            {
                _logger.LogWarning($"GetPriceBasedOnQty: Product is null for ProductId: {shoppingCart.ProductId}");
                return 0;
            }
            
            var price = shoppingCart.product.Price;
            
            // If Price is 0, try using ListPrice as fallback
            if (price == 0 && shoppingCart.product.ListPrice > 0)
            {
                _logger.LogWarning($"GetPriceBasedOnQty: Price is 0, using ListPrice as fallback for ProductId: {shoppingCart.ProductId}, ListPrice: {shoppingCart.product.ListPrice}");
                price = shoppingCart.product.ListPrice;
            }
            
            // Log if price is still 0 for debugging
            if (price == 0)
            {
                _logger.LogWarning($"GetPriceBasedOnQty: Price is 0 for ProductId: {shoppingCart.ProductId}, ProductTitle: {shoppingCart.product.Title}, ListPrice: {shoppingCart.product.ListPrice}");
            }
            
            return price;
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

            if (promoCode.DiscountType == IdealWeightNutrition.Models.DiscountType.Percentage)
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

            if (promoCode.DiscountType == IdealWeightNutrition.Models.DiscountType.Percentage)
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
            try
            {
                // If this item is from a flash sale, use the flash sale price
                if (shoppingCart.FlashSaleItemId.HasValue)
                {
                    // First check if FlashSalePrice is set directly
                    if (shoppingCart.FlashSalePrice.HasValue && shoppingCart.FlashSalePrice.Value > 0)
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
                    if (comboOffer != null && comboOffer.ComboPrice > 0)
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
                        // Use variant price even if it's 0 (free variant)
                        // Only check if variant exists, not if price > 0
                        return (double)shoppingCart.ProductVariant.Price;
                    }
                }

                // Otherwise, use the regular quantity-based pricing
                // Ensure product is loaded before getting price
                if (shoppingCart.product == null)
                {
                    shoppingCart.product = _unitOfWork.product.Get(p => p.Id == shoppingCart.ProductId);
                }
                
                var price = GetPriceBasedOnQty(shoppingCart);
                
                // Log if price is 0 for debugging
                if (price == 0 && shoppingCart.product != null)
                {
                    _logger.LogWarning($"GetCartItemPrice: Price is 0 for ProductId: {shoppingCart.ProductId}, ProductTitle: {shoppingCart.product.Title}, ProductPrice: {shoppingCart.product.Price}");
                }
                
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetCartItemPrice for CartId: {shoppingCart.Id}, ProductId: {shoppingCart.ProductId}");
                return 0;
            }
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
                    var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
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

        /// <summary>
        /// Merges guest cart items into user's cart after login/register.
        /// Only adds items that don't already exist in the user's cart.
        /// </summary>
        /// <param name="userId">The authenticated user's ID</param>
        /// <param name="session">The HTTP session containing guest cart</param>
        /// <returns>Number of items merged</returns>
        public static int MergeGuestCartToUserCart(IUnitOfWork unitOfWork, string userId, ISession session)
        {
            var guestCart = IdealWeightNutrition.Utility.GuestCartHelper.GetGuestCart(session);
            if (guestCart == null || !guestCart.Any())
            {
                return 0; // No guest cart items to merge
            }

            // Get user's existing cart items
            var userCartItems = unitOfWork.shoppingCart.GetAll(
                c => c.ApplicationUserId == userId,
                includeProperties: "product,ProductVariant,FlashSaleItem,ComboOffer"
            ).ToList();

            int mergedCount = 0;

            foreach (var guestItem in guestCart)
            {
                // Check if this item already exists in user's cart
                // Match by: ProductId, ProductVariantId, FlashSaleItemId, ComboOfferId
                var existingItem = userCartItems.FirstOrDefault(uc =>
                    uc.ProductId == guestItem.ProductId &&
                    uc.ProductVariantId == guestItem.ProductVariantId &&
                    uc.FlashSaleItemId == guestItem.FlashSaleItemId &&
                    uc.ComboOfferId == guestItem.ComboOfferId
                );

                if (existingItem == null)
                {
                    // Item doesn't exist in user's cart - add it
                    var newCartItem = new ShoppingCart
                    {
                        ProductId = guestItem.ProductId,
                        Count = guestItem.Count,
                        ApplicationUserId = userId,
                        ProductVariantId = guestItem.ProductVariantId,
                        FlashSaleItemId = guestItem.FlashSaleItemId,
                        FlashSalePrice = guestItem.FlashSalePrice.HasValue ? (decimal?)guestItem.FlashSalePrice.Value : null,
                        ComboOfferId = guestItem.ComboOfferId
                    };

                    unitOfWork.shoppingCart.Add(newCartItem);
                    mergedCount++;
                }
                // If item exists, skip it (don't add duplicate)
            }

            if (mergedCount > 0)
            {
                unitOfWork.save();
            }

            // Clear guest cart after merging
            IdealWeightNutrition.Utility.GuestCartHelper.ClearCart(session);

            return mergedCount;
        }
    }

}
