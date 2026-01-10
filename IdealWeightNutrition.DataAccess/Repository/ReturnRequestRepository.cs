using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ReturnRequestRepository : Repository<ReturnRequest>, IReturnRequestRepository
    {
        private readonly ApplicationDBContext _db;

        public ReturnRequestRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ReturnRequest returnRequest)
        {
            _db.ReturnRequests.Update(returnRequest);
        } 
        public void Add(ReturnRequest returnRequest)
        {
            _db.ReturnRequests.Add(returnRequest);
        }

        public IEnumerable<ReturnRequest> GetByOrderId(int orderId)
        {
            return _db.ReturnRequests
                .Where(r => r.OrderHeaderId == orderId)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.Product)
                .OrderByDescending(r => r.RequestDate)
                .ToList();
        }

        public IEnumerable<ReturnRequest> GetByUserId(string userId)
        {
            return _db.ReturnRequests
                .Where(r => r.ApplicationUserId == userId)
                .Include(r => r.OrderHeader)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.Product)
                .OrderByDescending(r => r.RequestDate)
                .ToList();
        }

        public IEnumerable<ReturnRequest> GetByStatus(string status)
        {
            return _db.ReturnRequests
                .Where(r => r.Status == status)
                .Include(r => r.OrderHeader)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.Product)
                .OrderByDescending(r => r.RequestDate)
                .ToList();
        }

        public ReturnRequest? GetWithItems(int id)
        {
            return _db.ReturnRequests
                .Include(r => r.OrderHeader)
                .Include(r => r.ApplicationUser)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.Product)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.FlashSaleItem)
                .Include(r => r.ReturnRequestItems)
                .ThenInclude(i => i.OrderDetail)
                .ThenInclude(od => od.ComboOffer)
                .FirstOrDefault(r => r.Id == id);
        }
    }
}

