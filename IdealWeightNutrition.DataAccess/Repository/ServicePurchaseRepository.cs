using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ServicePurchaseRepository : Repository<ServicePurchase>, IServicePurchaseRepository
    {
        private ApplicationDBContext _db;
        public ServicePurchaseRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ServicePurchase obj)
        {
            _db.ServicePurchases.Update(obj);
        } 
        public void Add(ServicePurchase obj)
        {
            _db.ServicePurchases.Update(obj);
        }
    }
}

