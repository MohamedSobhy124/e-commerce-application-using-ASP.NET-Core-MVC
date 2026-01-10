using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository

    {

		private ApplicationDBContext _db;
		public OrderHeaderRepository(ApplicationDBContext db) : base(db)
		{
			_db = db;
		}


		public void Update(OrderHeader obj)
        {
            _db.Update(obj);
        }

		public async Task UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
		{
            var orderFromDb = await _db.orderHeaders
                        .AsTracking()
                        .FirstOrDefaultAsync(u => u.Id == id);
            if (orderFromDb != null)
			{
				orderFromDb.OrderStatus = orderStatus;
				if (!string.IsNullOrEmpty(paymentStatus))
				{
					orderFromDb.PaymentStatus = paymentStatus;
				}
			}
          await _db.SaveChangesAsync();
        }
		public void UpdatePaymentID(int id, string sessionId, string paymentIntentId)
		{
			var orderFromDb = _db.orderHeaders.FirstOrDefault(u => u.Id == id);
			if (!string.IsNullOrEmpty(sessionId))
			{
				orderFromDb.SessionId = sessionId;
			}
			if (!string.IsNullOrEmpty(paymentIntentId))
			{
				orderFromDb.PaymentIntentId = paymentIntentId;
				orderFromDb.PaymentDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
			}
            _db.SaveChanges();
        }

		// Legacy method name kept for backward compatibility
		[Obsolete("Use UpdatePaymentID instead")]
		public void UpdateStripePaymentID(int id, string sessionId, string paymentIntentId)
		{
			UpdatePaymentID(id, sessionId, paymentIntentId);
		}

	}
}
