using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class CityRepository : Repository<City>, ICityRepository
    {
        private ApplicationDBContext _db;
        public CityRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(City obj)
        {
            _db.Cities.Update(obj);
        }
    }
}
