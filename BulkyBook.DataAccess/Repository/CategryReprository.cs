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
            _db.Categries.Update(obj);
        }
    }
}
