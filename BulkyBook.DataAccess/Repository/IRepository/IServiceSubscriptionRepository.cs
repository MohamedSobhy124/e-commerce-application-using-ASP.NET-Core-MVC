using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceSubscriptionRepository : IRepository<ServiceSubscription>
    {
        void Update(ServiceSubscription obj);
        void Add(ServiceSubscription obj);
        void Remove(ServiceSubscription obj);
    }
}

