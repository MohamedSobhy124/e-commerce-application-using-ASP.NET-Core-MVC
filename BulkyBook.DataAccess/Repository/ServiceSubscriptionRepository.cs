using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class ServiceSubscriptionRepository : Repository<ServiceSubscription>, IServiceSubscriptionRepository
    {
        private ApplicationDBContext _db;
        public ServiceSubscriptionRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceSubscription obj)
        {
            _db.ServiceSubscriptions.Update(obj);
        } 
        public void Add(ServiceSubscription obj)
        {
            _db.ServiceSubscriptions.Add(obj);
        }  
        public void Remove(ServiceSubscription obj)
        {
            _db.ServiceSubscriptions.Remove(obj);
        }
    }
}

