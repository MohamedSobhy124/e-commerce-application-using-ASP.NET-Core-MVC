using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface ICityRepository : IRepository<City>
    {
        void Update(City obj);
    }
}
