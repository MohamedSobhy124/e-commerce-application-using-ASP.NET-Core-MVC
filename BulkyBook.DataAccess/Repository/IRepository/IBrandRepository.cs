using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IBrandRepository : IRepository<Brand> 
    {
        void update(Brand obj);
    }
}

