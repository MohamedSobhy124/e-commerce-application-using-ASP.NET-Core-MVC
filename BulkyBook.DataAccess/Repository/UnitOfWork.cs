using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDBContext _db;
        public ICategryReprository categry { get; private set; }
        public IProductReprository product { get; private set; }
        public UnitOfWork(ApplicationDBContext db) 
        {
            _db = db;
            categry=new CategryReprository(_db);
            product = new ProductReprository(_db);
        }

        public void save()
        {
            _db.SaveChanges();
        }
    }
}
