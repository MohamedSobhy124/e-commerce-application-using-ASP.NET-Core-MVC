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
        public ICompanyRepository company { get; private set; }

        public ICategryReprository categry { get; private set; }
        public IProductReprository product { get; private set; }

        public IApplicationUserReprository   applicationUser { get; private set; }

        public IShoppingCartReprository shoppingCart  { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IOrderDetailRepository OrderDetail { get; private set; }
        public INotificationRepository notification { get; private set; }

        public UnitOfWork(ApplicationDBContext db) 
        {
            _db = db;
            categry=new CategryReprository(_db);
            product = new ProductReprository(_db);
            company = new CompanyReprository(_db);
            applicationUser = new ApplicationUserReprository(_db);
            shoppingCart =new ShoppingCartReprository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
            notification = new NotificationRepository(_db);

        }

        public void save()
        {
            _db.SaveChanges();
        }
    }
}
