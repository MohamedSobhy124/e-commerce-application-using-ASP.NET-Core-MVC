using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
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
            var reviews = _db.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .ToList();

            if (reviews.Count == 0)
                return 0;

            return Math.Round(reviews.Average(r => r.Rating), 1);
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
    }
}

