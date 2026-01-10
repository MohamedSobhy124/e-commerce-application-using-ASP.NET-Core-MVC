using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ReviewRepository : Repository<Review>, IReviewRepository
    {
        private readonly ApplicationDBContext _db;

        public ReviewRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Review review)
        {
            _db.Reviews.Update(review);
        }

        public double GetAverageRating(int productId)
        {
            // PERFORMANCE: Calculate average directly in database instead of loading all reviews
            var average = _db.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Select(r => (double?)r.Rating)
                .Average();

            return average.HasValue ? Math.Round(average.Value, 1) : 0;
        }

        public int GetReviewCount(int productId)
        {
            return _db.Reviews
                .Count(r => r.ProductId == productId && r.IsApproved);
        }

        public IEnumerable<Review> GetApprovedReviewsByProduct(int productId)
        {
            return _db.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }

        public Dictionary<int, (double averageRating, int reviewCount)> GetBatchProductRatings(List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
                return new Dictionary<int, (double, int)>();

            // Remove duplicates to prevent dictionary key conflicts
            var uniqueProductIds = productIds.Distinct().ToList();

            // Get all reviews for the products in one query
            var reviews = _db.Reviews
                .Where(r => uniqueProductIds.Contains(r.ProductId) && r.IsApproved)
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = Math.Round(g.Average(r => r.Rating), 1),
                    ReviewCount = g.Count()
                })
                .ToList();

            // Create dictionary with all product IDs (including those with no reviews)
            var result = uniqueProductIds.ToDictionary(
                id => id,
                id =>
                {
                    var review = reviews.FirstOrDefault(r => r.ProductId == id);
                    return review != null
                        ? (review.AverageRating, review.ReviewCount)
                        : (0.0, 0);
                }
            );

            return result;
        }

        public IEnumerable<Review> GetFeaturedTestimonials(int count = 10)
        {
            // Get approved reviews, prioritizing verified purchases and higher ratings
            // Note: User will be loaded separately in controller due to Identity Discriminator filter issues
            // Don't use AsNoTracking here because we need to attach User navigation property
            return _db.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.IsVerifiedPurchase) // Verified purchases first
                .ThenByDescending(r => r.Rating) // Higher ratings next
                .ThenByDescending(r => r.CreatedAt) // Most recent next
                .Take(count)
                .ToList();
        }
    }
}

