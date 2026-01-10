using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IBrandRepository : IRepository<Brand> 
    {
        void update(Brand obj);
    }
}

