using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ReturnRequestItemRepository : Repository<ReturnRequestItem>, IReturnRequestItemRepository
    {
        private readonly ApplicationDBContext _db;

        public ReturnRequestItemRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ReturnRequestItem returnRequestItem)
        {
            _db.ReturnRequestItems.Update(returnRequestItem);
        } 
        public void Add(ReturnRequestItem returnRequestItem)
        {
            _db.ReturnRequestItems.Add(returnRequestItem);
        }
    }
}

