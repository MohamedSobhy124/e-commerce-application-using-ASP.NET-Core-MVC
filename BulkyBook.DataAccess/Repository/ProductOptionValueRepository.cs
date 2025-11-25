using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductOptionValueRepository : Repository<ProductOptionValue>, IProductOptionValueRepository
    {
        private ApplicationDBContext _db;

        public ProductOptionValueRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductOptionValue obj)
        {
            var objFromDb = _db.ProductOptionValues.FirstOrDefault(a => a.Id == obj.Id);
            if (objFromDb != null)
            {
                objFromDb.Value = obj.Value;
                objFromDb.ProductOptionId = obj.ProductOptionId;
                objFromDb.DisplayOrder = obj.DisplayOrder;
            }
        }
    }
}

