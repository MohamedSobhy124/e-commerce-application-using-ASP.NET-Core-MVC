using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface ICategryReprository :IRepository<Categry> 
    {
        void update(Categry obj);
       

    }
}
