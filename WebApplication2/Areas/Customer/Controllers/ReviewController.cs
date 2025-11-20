using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(int productId, int rating, string comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
                {
                    TempData["error"] = "Please provide a valid rating and comment";
                    return RedirectToAction("Details", "Home", new { productId = productId });
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Check if user already reviewed this product
                var existingReview = _unitOfWork.review.Get(r => r.ProductId == productId && r.UserId == userId);
                
                if (existingReview != null)
                {
                    TempData["error"] = "You have already reviewed this product";
                    return RedirectToAction("Details", "Home", new { productId = productId });
                }

                // Check if user purchased the product (optional - can skip for testing)
                var hasPurchased = false;
                try
                {
                    hasPurchased = _unitOfWork.OrderDetail.GetAll(
                        od => od.ProductId == productId,
                        includeProperties: "OrderHeader"
                    ).Any(od => od.OrderHeader != null && 
                                od.OrderHeader.ApplicationUserId == userId && 
                                od.OrderHeader.OrderStatus == SD.StatusDelivered);
                }
                catch
                {
                    // If error checking purchase, just set to false
                    hasPurchased = false;
                }

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    IsVerifiedPurchase = hasPurchased,
                    IsApproved = true, // Auto-approve for now (change to false for moderation)
                    CreatedAt = DateTime.Now,
                    HelpfulCount = 0
                };

                _unitOfWork.review.add(review);
                _unitOfWork.save();

                TempData["success"] = "Your review has been submitted successfully!";
                return RedirectToAction("Details", "Home", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error submitting review: {ex.Message}";
                return RedirectToAction("Details", "Home", new { productId = productId });
            }
        }

        [HttpGet]
        public IActionResult GetReviews(int productId)
        {
            var reviews = _unitOfWork.review.GetAll(
                r => r.ProductId == productId && r.IsApproved,
                includeProperties: "User"
            ).OrderByDescending(r => r.CreatedAt);

            var reviewData = reviews.Select(r => new
            {
                id = r.Id,
                userName = r.User.Name,
                rating = r.Rating,
                comment = r.Comment,
                date = r.CreatedAt.ToString("MMM dd, yyyy"),
                isVerifiedPurchase = r.IsVerifiedPurchase,
                helpfulCount = r.HelpfulCount
            });

            return Json(new { success = true, reviews = reviewData });
        }

        [HttpGet]
        public IActionResult GetProductRating(int productId)
        {
            var averageRating = _unitOfWork.review.GetAverageRating(productId);
            var reviewCount = _unitOfWork.review.GetReviewCount(productId);

            return Json(new
            {
                averageRating = averageRating,
                reviewCount = reviewCount
            });
        }
    }
}

