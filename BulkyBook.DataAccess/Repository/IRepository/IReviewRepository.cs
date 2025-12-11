using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IReviewRepository : IRepository<Review>
    {
        void Update(Review review);
        double GetAverageRating(int productId);
        int GetReviewCount(int productId);
        Dictionary<int, (double averageRating, int reviewCount)> GetBatchProductRatings(List<int> productIds);
        IEnumerable<Review> GetApprovedReviewsByProduct(int productId);
        IEnumerable<Review> GetFeaturedTestimonials(int count = 10);
    }
}

