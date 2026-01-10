using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IOrderAuditLogRepository : IRepository<OrderAuditLog>
    {
        void Update(OrderAuditLog obj);
        void Add(OrderAuditLog obj);
    }
}

