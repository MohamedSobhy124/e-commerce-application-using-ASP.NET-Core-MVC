using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IProductVariantRepository : IRepository<ProductVariant>
    {
        void Update(ProductVariant obj);
        ProductVariant? GetVariantByOptionValues(int productId, List<int> optionValueIds);
    }
}

