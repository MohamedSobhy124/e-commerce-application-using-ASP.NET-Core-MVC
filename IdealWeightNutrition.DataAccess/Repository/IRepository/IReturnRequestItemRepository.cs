using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IReturnRequestItemRepository : IRepository<ReturnRequestItem>
    {
        void Update(ReturnRequestItem returnRequestItem);
        void Add(ReturnRequestItem returnRequestItem);
    }
}

