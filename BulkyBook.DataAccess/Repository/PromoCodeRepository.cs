using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
    public class PromoCodeRepository : Repository<PromoCode>, IPromoCodeRepository
    {
        private ApplicationDBContext _db;

        public PromoCodeRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(PromoCode obj)
        {
            var objFromDB = _db.PromoCodes.FirstOrDefault(a => a.Id == obj.Id);
            if (objFromDB != null)
            {
                objFromDB.Code = obj.Code;
                objFromDB.Description = obj.Description;
                objFromDB.DiscountType = obj.DiscountType;
                objFromDB.DiscountValue = obj.DiscountValue;
                objFromDB.MinimumOrderAmount = obj.MinimumOrderAmount;
                objFromDB.MaximumDiscountAmount = obj.MaximumDiscountAmount;
                objFromDB.StartDate = obj.StartDate;
                objFromDB.EndDate = obj.EndDate;
                objFromDB.UsageLimit = obj.UsageLimit;
                objFromDB.TimesUsed = obj.TimesUsed;
                objFromDB.UsageLimitPerUser = obj.UsageLimitPerUser;
                objFromDB.IsActive = obj.IsActive;
            }
        }

        public PromoCode GetByCode(string code)
        {
            return _db.PromoCodes.FirstOrDefault(p => p.Code.ToLower() == code.ToLower() && p.IsActive);
        }

        public bool IsCodeAvailable(string code, int? excludeId = null)
        {
            var query = _db.PromoCodes.Where(p => p.Code.ToLower() == code.ToLower());
            
            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }
            
            return !query.Any();
        }

        public IEnumerable<PromoCode> GetActivePromoCodes()
        {
            var now = DateTime.Now;
            return _db.PromoCodes
                .Where(p => p.IsActive && 
                           p.StartDate <= now && 
                           p.EndDate >= now &&
                           (!p.UsageLimit.HasValue || p.TimesUsed < p.UsageLimit.Value))
                .ToList();
        }

        public bool CanUserUsePromoCode(int promoCodeId, string userId)
        {
            var promoCode = _db.PromoCodes.FirstOrDefault(p => p.Id == promoCodeId);
            
            if (promoCode == null || !promoCode.IsActive)
                return false;

            var now = DateTime.Now;
            if (now < promoCode.StartDate || now > promoCode.EndDate)
                return false;

            // Check total usage limit
            if (promoCode.UsageLimit.HasValue && promoCode.TimesUsed >= promoCode.UsageLimit.Value)
                return false;

            // Check per-user usage limit
            if (promoCode.UsageLimitPerUser.HasValue)
            {
                var userUsageCount = _db.PromoCodeUsages.Include(_=>_.OrderHeader).Where(_=>_.OrderHeader.OrderStatus!=SD.StatusPending)
                    .Count(u => u.PromoCodeId == promoCodeId && u.UserId == userId);
                
                if (userUsageCount >= promoCode.UsageLimitPerUser.Value)
                    return false;
            }

            return true;
        }

        public int GetUserPromoCodeUsageCount(int promoCodeId, string userId)
        {
            return _db.PromoCodeUsages
                .Count(u => u.PromoCodeId == promoCodeId && u.UserId == userId);
        }

        public void IncrementUsage(int promoCodeId)
        {
            var promoCode = _db.PromoCodes.FirstOrDefault(p => p.Id == promoCodeId);
            if (promoCode != null)
            {
                promoCode.TimesUsed++;
            }
        }
    }
}

