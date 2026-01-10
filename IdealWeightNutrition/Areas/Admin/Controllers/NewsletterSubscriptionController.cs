using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class NewsletterSubscriptionController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;
        private readonly ILogger<NewsletterSubscriptionController> _logger;

        public NewsletterSubscriptionController(
            IUnitOfWork unitOfWork, 
            IStringLocalizer<IdealWeightNutrition.SharedResources> localizer,
            ILogger<NewsletterSubscriptionController> logger)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var subscriptions = _unitOfWork.NewsletterSubscription.GetAll()
                    .OrderByDescending(n => n.SubscribedDate)
                    .ToList();

                var data = subscriptions.Select(s => new
                {
                    id = s.Id,
                    email = s.Email,
                    subscribedDate = s.SubscribedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    isActive = s.IsActive,
                    unsubscribedDate = s.UnsubscribedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                    source = s.Source ?? "N/A"
                }).ToList();

                return Json(new { data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading newsletter subscriptions");
                return Json(new { data = new List<object>() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            try
            {
                var subscription = _unitOfWork.NewsletterSubscription.Get(s => s.Id == id);
                
                if (subscription == null)
                {
                    return Json(new { success = false, message = _localizer["SubscriptionNotFound"].ToString() });
                }

                subscription.IsActive = !subscription.IsActive;
                
                if (!subscription.IsActive)
                {
                    subscription.UnsubscribedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
                }
                else
                {
                    subscription.UnsubscribedDate = null;
                    subscription.SubscribedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
                }

                _unitOfWork.NewsletterSubscription.Update(subscription);
                _unitOfWork.save();

                return Json(new 
                { 
                    success = true, 
                    message = subscription.IsActive 
                        ? _localizer["SubscriptionActivated"].ToString() 
                        : _localizer["SubscriptionDeactivated"].ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling subscription status: {Id}", id);
                return Json(new { success = false, message = _localizer["ErrorUpdatingSubscription"].ToString() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var subscription = _unitOfWork.NewsletterSubscription.Get(s => s.Id == id);
                
                if (subscription == null)
                {
                    return Json(new { success = false, message = _localizer["SubscriptionNotFound"].ToString() });
                }

                _unitOfWork.NewsletterSubscription.remove(subscription);
                _unitOfWork.save();

                return Json(new { success = true, message = _localizer["SubscriptionDeleted"].ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription: {Id}", id);
                return Json(new { success = false, message = _localizer["ErrorDeletingSubscription"].ToString() });
            }
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var subscription = _unitOfWork.NewsletterSubscription.Get(s => s.Id == id);
            
            if (subscription == null)
            {
                TempData["error"] = _localizer["SubscriptionNotFound"].ToString();
                return RedirectToAction(nameof(Index));
            }

            return View(subscription);
        }

        [HttpGet]
        public IActionResult Export()
        {
            try
            {
                var subscriptions = _unitOfWork.NewsletterSubscription.GetAll()
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.SubscribedDate)
                    .ToList();

                // Generate CSV content
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Email,Subscribed Date,Source");
                
                foreach (var sub in subscriptions)
                {
                    csv.AppendLine($"{sub.Email},{sub.SubscribedDate:yyyy-MM-dd HH:mm:ss},{sub.Source ?? "N/A"}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"newsletter-subscribers-{IdealWeightNutrition.Utility.DateTimeHelper.Now:yyyyMMdd-HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting newsletter subscriptions");
                TempData["error"] = _localizer["ErrorExportingSubscriptions"].ToString();
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

