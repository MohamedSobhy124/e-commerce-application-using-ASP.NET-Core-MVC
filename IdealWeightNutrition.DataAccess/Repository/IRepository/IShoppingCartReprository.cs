using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IShoppingCartReprository : IRepository<ShoppingCart> 
    {
        void update(ShoppingCart obj);
        void Add(ShoppingCart obj);
       

    }
}
