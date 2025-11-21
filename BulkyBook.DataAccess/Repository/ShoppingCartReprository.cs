using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
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
