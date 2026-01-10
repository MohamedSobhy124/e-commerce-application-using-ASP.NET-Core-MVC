using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
 

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ShoppingCartReprository : Repository<ShoppingCart>, IShoppingCartReprository
    {
        private ApplicationDBContext _db;

        public ShoppingCartReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }
        

        public void update(ShoppingCart obj)
        {
            _db.ShoppingCarts.Update(obj);
        }
        public void Add(ShoppingCart obj)
        {
            _db.ShoppingCarts.Add(obj);
        }
    }
}
