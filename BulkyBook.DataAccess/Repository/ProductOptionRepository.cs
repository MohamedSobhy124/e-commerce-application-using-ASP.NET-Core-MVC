using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductOptionRepository : Repository<ProductOption>, IProductOptionRepository
    {
        private ApplicationDBContext _db;

        public ProductOptionRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductOption obj)
        {
            var objFromDb = _db.ProductOptions.FirstOrDefault(a => a.Id == obj.Id);
            if (objFromDb != null)
            {
                objFromDb.Name = obj.Name;
                objFromDb.ProductId = obj.ProductId;
                objFromDb.DisplayOrder = obj.DisplayOrder;
            }
        }
    }
}

