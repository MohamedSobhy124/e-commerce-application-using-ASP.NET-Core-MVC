using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class OrderAuditLogRepository : Repository<OrderAuditLog>, IOrderAuditLogRepository
    {
        private ApplicationDBContext _db;

        public OrderAuditLogRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderAuditLog obj)
        {
            _db.Update(obj);
        }  
        public void Add(OrderAuditLog obj)
        {
            _db.Add(obj);
        }
    }
}

