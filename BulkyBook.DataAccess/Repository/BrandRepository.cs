using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
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
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
            }
            _db.Brands.Update(obj);
        }
    }
}
