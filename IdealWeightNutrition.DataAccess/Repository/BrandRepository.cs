using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
 

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class BrandRepository : Repository<Brand>, IBrandRepository
    {
        private ApplicationDBContext _db;

        public BrandRepository(ApplicationDBContext db) : base(db) 
        {
            _db = db;   
        }
        

        public void update(Brand obj)
        {
            // Set audit fields
            if (obj is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            }
            _db.Brands.Update(obj);
        }
    }
}
