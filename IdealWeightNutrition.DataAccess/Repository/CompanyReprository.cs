
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
 

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class CompanyReprository :Repository<Company>, ICompanyRepository
    {
        private ApplicationDBContext _db;

        public CompanyReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }
        
 

        public void Update(Company obj)
        {
            _db.Companys.Update(obj);
        }
    }
}
