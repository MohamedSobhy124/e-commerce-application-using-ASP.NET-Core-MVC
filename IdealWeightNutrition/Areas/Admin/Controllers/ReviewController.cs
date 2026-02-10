using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status = "all")
        {
            var reviews = _unitOfWork.review.GetAll(includeProperties: "Product,ServiceSubscription,User");

            // Filter by approval status
            if (status == "pending")
            {
                reviews = reviews.Where(r => !r.IsApproved);
            }
            else if (status == "approved")
            {
                reviews = reviews.Where(r => r.IsApproved);
            }

            // Order by CreatedAt descending and materialize the query
            var orderedReviews = reviews.OrderByDescending(r => r.CreatedAt).ToList();

            var reviewData = orderedReviews.Select(r => new
            {
                id = r.Id,
                productName = r.Product != null ? r.Product.Title : null,
                serviceName = r.ServiceSubscription != null ? r.ServiceSubscription.Title : null,
                itemName = r.Product != null ? r.Product.Title : (r.ServiceSubscription != null ? r.ServiceSubscription.Title : "N/A"),
                reviewType = r.Product != null ? "Product" : "Service",
                userName = r.User?.Name ?? "Anonymous",
                rating = r.Rating,
                comment = r.Comment.Length > 100 ? r.Comment.Substring(0, 100) + "..." : r.Comment,
                fullComment = r.Comment,
                createdAt = r.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                createdAtTimestamp = new DateTimeOffset(r.CreatedAt).ToUnixTimeSeconds(), // Unix timestamp for sorting
                createdAtRaw = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), // ISO format for debugging
                isApproved = r.IsApproved,
                isVerifiedPurchase = r.IsVerifiedPurchase
            }).ToList();

            return Json(new { data = reviewData });
        }

        [HttpPost]
        public IActionResult ToggleApproval(int id)
        {
            var review = _unitOfWork.review.Get(r => r.Id == id);
            
            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            review.IsApproved = !review.IsApproved;
            _unitOfWork.review.Update(review);
            _unitOfWork.save();

            var status = review.IsApproved ? "approved" : "pending";
            return Json(new { success = true, message = $"Review {status} successfully", isApproved = review.IsApproved });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var review = _unitOfWork.review.Get(r => r.Id == id);
            
            if (review == null)
            {
                return Json(new { success = false, message = "Review not found" });
            }

            _unitOfWork.review.remove(review);
            _unitOfWork.save();

            return Json(new { success = true, message = "Review deleted successfully" });
        }

        #endregion
    }
}

