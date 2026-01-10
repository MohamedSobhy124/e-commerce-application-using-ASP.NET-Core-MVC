using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IServiceOfferRepository : IRepository<ServiceOffer>
    {
        void Update(ServiceOffer obj);
        void Add(ServiceOffer obj);
        void Remove(ServiceOffer obj);
    }
}

