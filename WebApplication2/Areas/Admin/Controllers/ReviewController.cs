using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBook.Areas.Admin.Controllers
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
            var reviews = _unitOfWork.review.GetAll(includeProperties: "Product,User");

            // Filter by approval status
            if (status == "pending")
            {
                reviews = reviews.Where(r => !r.IsApproved);
            }
            else if (status == "approved")
            {
                reviews = reviews.Where(r => r.IsApproved);
            }

            var reviewData = reviews.OrderByDescending(r => r.CreatedAt).Select(r => new
            {
                id = r.Id,
                productName = r.Product.Title,
                userName = r.User.Name,
                rating = r.Rating,
                comment = r.Comment.Length > 100 ? r.Comment.Substring(0, 100) + "..." : r.Comment,
                fullComment = r.Comment,
                createdAt = r.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                isApproved = r.IsApproved,
                isVerifiedPurchase = r.IsVerifiedPurchase
            });

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

