using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IServiceSubscriptionRepository : IRepository<ServiceSubscription>
    {
        void Update(ServiceSubscription obj);
        void Add(ServiceSubscription obj);
        void Remove(ServiceSubscription obj);
    }
}

