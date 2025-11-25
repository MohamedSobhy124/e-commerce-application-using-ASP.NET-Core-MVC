using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IProductOptionValueRepository : IRepository<ProductOptionValue>
    {
        void Update(ProductOptionValue obj);
    }
}

