using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
    public class PromoCodeUsageRepository : Repository<PromoCodeUsage>, IPromoCodeUsageRepository
    {
        private ApplicationDBContext _db;

        public PromoCodeUsageRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void RecordUsage(int promoCodeId, string userId, int orderId)
        {
            var usage = new PromoCodeUsage
            {
                PromoCodeId = promoCodeId,
                UserId = userId,
                OrderId = orderId,
                UsedDate = DateTime.Now
            };
            
            _db.PromoCodeUsages.Add(usage);
        }

        public int GetUsageCount(int promoCodeId, string userId)
        {
            return _db.PromoCodeUsages
                .Count(u => u.PromoCodeId == promoCodeId && u.UserId == userId);
        }
    }
}

