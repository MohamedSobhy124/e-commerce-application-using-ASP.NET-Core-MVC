using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IProductReprository :IRepository<Product> 
    {
        void update(Product obj);
       

    }
}
