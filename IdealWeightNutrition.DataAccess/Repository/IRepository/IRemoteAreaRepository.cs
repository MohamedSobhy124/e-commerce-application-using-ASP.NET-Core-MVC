using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IRemoteAreaRepository : IRepository<RemoteArea>
    {
        void Update(RemoteArea obj);
    }
}
