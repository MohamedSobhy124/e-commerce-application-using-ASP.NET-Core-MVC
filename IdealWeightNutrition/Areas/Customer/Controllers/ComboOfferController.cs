using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ComboOfferController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly ApplicationDBContext _dbContext;

        public ComboOfferController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _dbContext = dbContext;
        }

        // GET: Combo Offers List
        public IActionResult Index()
        {
            var activeComboOffers = _unitOfWork.ComboOffer.GetActiveComboOffers();
            return View(activeComboOffers);
        }

        // GET: Combo Offer Details
        public IActionResult Details(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(id.Value);
            if (comboOffer == null)
            {
                return NotFound();
            }

            // Load variant option values for combo offer items
            if (comboOffer.ComboOfferItems != null)
            {
                foreach (var item in comboOffer.ComboOfferItems.Where(i => !i.IsDeleted && i.ProductVariantId.HasValue))
                {
                    if (item.ProductVariant != null)
                    {
                        // Load variant option values using DbContext
                        item.ProductVariant.VariantOptionValues = _dbContext.ProductVariantOptionValues
                            .Include(vov => vov.OptionValue)
                                .ThenInclude(ov => ov.ProductOption)
                            .Where(vov => vov.ProductVariantId == item.ProductVariant.Id)
                            .ToList();
                    }
                }
            }

            // Allow viewing even if not currently active (for debugging and showing status)
            // The view will handle showing/hiding the Add to Cart button based on IsCurrentlyActive
            return View(comboOffer);
        }

        // POST: Add Combo to Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int comboOfferId)
        {
            var comboOffer = _unitOfWork.ComboOffer.GetComboOfferWithItems(comboOfferId);
            if (comboOffer == null || !comboOffer.IsCurrentlyActive)
            {
                TempData["error"] = _localizer["ComboOfferNotFoundOrInactive"].ToString();
                return RedirectToAction(nameof(Index));
            }

            // Validate stock for all required products
            var requiredItems = comboOffer.ComboOfferItems
                .Where(item => !item.IsDeleted && item.IsRequired)
                .ToList();

            foreach (var item in requiredItems)
            {
                int availableStock = 0;
                if (item.ProductVariantId.HasValue)
                {
                    var variant = _unitOfWork.ProductVariant.Get(v => v.Id == item.ProductVariantId.Value && !v.IsDeleted);
                    if (variant == null || variant.StockQuantity < item.Quantity)
                    {
                        TempData["error"] = _localizer["InsufficientStockForCombo"].ToString();
                        return RedirectToAction(nameof(Details), new { id = comboOfferId });
                    }
                }
                else
                {
                    var product = _unitOfWork.product.Get(p => p.Id == item.ProductId && !p.IsDeleted);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        TempData["error"] = _localizer["InsufficientStockForCombo"].ToString();
                        return RedirectToAction(nameof(Details), new { id = comboOfferId });
                    }
                }
            }

            try
            {
                // Get the first product from combo for display purposes (ProductId is required)
                var firstComboItem = comboOffer.ComboOfferItems
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Id)
                    .FirstOrDefault();

                if (firstComboItem == null)
                {
                    TempData["error"] = _localizer["ErrorAddingComboToCart"].ToString() + ": No products in combo";
                    return RedirectToAction(nameof(Details), new { id = comboOfferId });
                }

                // Check if user is authenticated
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    // Guest user - add to session cart
                    GuestCartHelper.AddToCart(
                        HttpContext.Session,
                        firstComboItem.ProductId,
                        count: 1,
                        productVariantId: firstComboItem.ProductVariantId,
                        comboOfferId: comboOfferId
                    );

                    TempData["success"] = _localizer["ComboAddedToCart"].ToString();
                    return RedirectToAction("Index", "Cart");
                }
                else
                {
                    // Authenticated user - add to database cart
                    // Check if this combo already exists in cart (as a single item)
                    // Only check ComboOfferId - a user should have only one cart item per combo offer
                    var existingComboCartItem = _unitOfWork.shoppingCart.Get(
                        c => c.ApplicationUserId == userId &&
                             c.ComboOfferId == comboOfferId
                    );

                    if (existingComboCartItem != null)
                    {
                        // Update quantity if combo already exists in cart
                        existingComboCartItem.Count += 1; // Add one more combo
                        _unitOfWork.shoppingCart.update(existingComboCartItem);
                    }
                    else
                    {
                        // Create a single cart item representing the entire combo
                        var cartItem = new ShoppingCart
                        {
                            ApplicationUserId = userId,
                            ProductId = firstComboItem.ProductId, // Use first product for display
                            ProductVariantId = firstComboItem.ProductVariantId, // Use first product variant if exists
                            Count = 1, // Count represents number of combos
                            ComboOfferId = comboOfferId, // Mark this as a combo offer
                            Price = (double)comboOffer.ComboPrice // Use combo price directly
                        };

                        _unitOfWork.shoppingCart.Add(cartItem);
                    }

                    _unitOfWork.save();

                    TempData["success"] = _localizer["ComboAddedToCart"].ToString();
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = _localizer["ErrorAddingComboToCart"].ToString() + ": " + ex.Message;
                return RedirectToAction(nameof(Details), new { id = comboOfferId });
            }
        }
    }
}

