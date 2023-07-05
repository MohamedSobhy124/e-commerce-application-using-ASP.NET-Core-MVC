using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IShoppingCartReprository : IRepository<ShoppingCart> 
    {
        void update(ShoppingCart obj);
       

    }
}
