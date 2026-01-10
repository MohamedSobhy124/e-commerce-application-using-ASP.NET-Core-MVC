using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
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
                // Set audit fields
                if (objFromDb is BaseEntity baseEntity)
                {
                    baseEntity.ModifiedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
                }
            }
        }
    }
}

