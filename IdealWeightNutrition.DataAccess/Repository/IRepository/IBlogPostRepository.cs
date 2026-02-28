using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IBlogPostRepository : IRepository<BlogPost>
    {
        void update(BlogPost obj);
    }
}
