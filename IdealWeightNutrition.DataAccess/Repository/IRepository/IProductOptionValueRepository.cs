using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IProductOptionValueRepository : IRepository<ProductOptionValue>
    {
        void Update(ProductOptionValue obj);
    }
}

