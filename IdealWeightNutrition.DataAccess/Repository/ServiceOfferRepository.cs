using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ServiceOfferRepository : Repository<ServiceOffer>, IServiceOfferRepository
    {
        private ApplicationDBContext _db;
        public ServiceOfferRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServiceOffer obj)
        {
            _db.ServiceOffers.Update(obj);
        }
        public void Add(ServiceOffer obj)
        {
            _db.ServiceOffers.Add(obj);
        }  
        public void Remove(ServiceOffer obj)
        {
            _db.ServiceOffers.Remove(obj);
        }
    }
}

