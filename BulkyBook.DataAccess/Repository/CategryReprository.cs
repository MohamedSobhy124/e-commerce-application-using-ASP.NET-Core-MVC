using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
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
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
            }
            _db.Categries.Update(obj);
        }
    }
}
