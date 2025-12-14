using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IStringLocalizer<BulkyBook.SharedResources> _localizer;
        private readonly IUnitOfWork _unitOfWork;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IStringLocalizer<BulkyBook.SharedResources> localizer,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _unitOfWork = unitOfWork;
        }

        // GET: Account Dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ViewBag.UserName = user.UserName;
            ViewBag.Email = user.Email;
            ViewBag.PhoneNumber = await _userManager.GetPhoneNumberAsync(user);
            ViewBag.EmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            return View();
        }

        // GET: User Orders
        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Index));
            }

            // Get all orders for the current user
            var orders = _unitOfWork.OrderHeader.GetAll(
                o => o.ApplicationUserId == userId && !o.IsGuestOrder,
                includeProperties: "ApplicationUser"
            )
            .OrderByDescending(o => o.OrderDate)
            .ToList();

            // Load order details for each order and store in ViewBag
            var orderDetailsDict = new Dictionary<int, List<OrderDetail>>();
            foreach (var order in orders)
            {
                var details = _unitOfWork.OrderDetail.GetAll(
                    od => od.OrderHeaderId == order.Id,
                    includeProperties: "Product,Product.ProductImages,FlashSaleItem,ProductVariant"
                ).ToList();
                orderDetailsDict[order.Id] = details;
            }

            ViewBag.OrderDetails = orderDetailsDict;

            return View(orders);
        }

        // GET: Order Details
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction(nameof(Index));
            }

            // Get the order and verify it belongs to the current user
            var order = _unitOfWork.OrderHeader.Get(
                o => o.Id == id && o.ApplicationUserId == userId && !o.IsGuestOrder,
                includeProperties: "ApplicationUser"
            );

            if (order == null)
            {
                TempData["error"] = _localizer["OrderNotFound"]?.Value ?? "Order not found";
                return RedirectToAction(nameof(Orders));
            }

            // Get order details
            var orderDetails = _unitOfWork.OrderDetail.GetAll(
                od => od.OrderHeaderId == order.Id,
                includeProperties: "Product,Product.ProductImages,Product.categry,FlashSaleItem,ProductVariant,ProductVariant.VariantOptionValues,ProductVariant.VariantOptionValues.OptionValue,ProductVariant.VariantOptionValues.OptionValue.ProductOption"
            ).ToList();

            ViewBag.OrderDetails = orderDetails;

            return View(order);
        }
    }
}

