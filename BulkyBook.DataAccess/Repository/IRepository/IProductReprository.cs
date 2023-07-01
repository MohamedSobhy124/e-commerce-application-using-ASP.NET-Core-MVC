using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IProductReprository :IRepository<Product> 
    {
        void update(Product obj);
       

    }
}
