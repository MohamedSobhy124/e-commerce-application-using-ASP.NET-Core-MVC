using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IProductOptionRepository : IRepository<ProductOption>
    {
        void Update(ProductOption obj);
    }
}

