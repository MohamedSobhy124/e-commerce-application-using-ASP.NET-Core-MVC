using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class RemoteAreaRepository : Repository<RemoteArea>, IRemoteAreaRepository
    {
        private ApplicationDBContext _db;
        public RemoteAreaRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(RemoteArea obj)
        {
            _db.RemoteAreas.Update(obj);
        }
    }
}
