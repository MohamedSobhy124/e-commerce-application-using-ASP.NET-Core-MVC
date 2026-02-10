using IdealWeightNutrition.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IReviewRepository : IRepository<Review>
    {
        void Update(Review review);
        
        // Product Review Methods
        double GetAverageRating(int productId);
        int GetReviewCount(int productId);
        Dictionary<int, (double averageRating, int reviewCount)> GetBatchProductRatings(List<int> productIds);
        IEnumerable<Review> GetApprovedReviewsByProduct(int productId);
        IEnumerable<Review> GetFeaturedTestimonials(int count = 10);
        
        // Service Review Methods
        double GetAverageServiceRating(int serviceId);
        int GetServiceReviewCount(int serviceId);
        Dictionary<int, (double averageRating, int reviewCount)> GetBatchServiceRatings(List<int> serviceIds);
        IEnumerable<Review> GetApprovedReviewsByService(int serviceId);
    }
}

