
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
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
