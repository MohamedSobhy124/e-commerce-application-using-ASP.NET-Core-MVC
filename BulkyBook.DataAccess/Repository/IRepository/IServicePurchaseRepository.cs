using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServicePurchaseRepository : IRepository<ServicePurchase>
    {
        void Update(ServicePurchase obj);
        void Add(ServicePurchase obj);
    }
}

