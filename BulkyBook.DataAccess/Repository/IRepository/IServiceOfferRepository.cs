using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IServiceOfferRepository : IRepository<ServiceOffer>
    {
        void Update(ServiceOffer obj);
        void Add(ServiceOffer obj);
        void Remove(ServiceOffer obj);
    }
}

