using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
 

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class CategryReprository :Repository<Categry>, ICategryReprository
    {
        private ApplicationDBContext _db;

        public CategryReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }
        

        public void update(Categry obj)
        {
            // Set audit fields
            if (obj is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            }
            _db.Categries.Update(obj);
        }
    }
}
