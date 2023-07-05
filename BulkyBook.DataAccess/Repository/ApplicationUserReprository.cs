using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
{
    public class ApplicationUserReprository : Repository<ApplicationUser>, IApplicationUserReprository
    {
        private ApplicationDBContext _db;

        public ApplicationUserReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }
        

       
    }
}
