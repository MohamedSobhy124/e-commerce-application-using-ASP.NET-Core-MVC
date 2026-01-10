using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IProductOptionRepository : IRepository<ProductOption>
    {
        void Update(ProductOption obj);
    }
}

