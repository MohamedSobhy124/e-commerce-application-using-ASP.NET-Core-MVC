using IdealWeightNutrition.Models;
using System.Collections.Generic;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IReturnRequestRepository : IRepository<ReturnRequest>
    {
        void Update(ReturnRequest returnRequest);
        void Add(ReturnRequest returnRequest);
        IEnumerable<ReturnRequest> GetByOrderId(int orderId);
        IEnumerable<ReturnRequest> GetByUserId(string userId);
        IEnumerable<ReturnRequest> GetByStatus(string status);
        ReturnRequest? GetWithItems(int id);
    }
}

