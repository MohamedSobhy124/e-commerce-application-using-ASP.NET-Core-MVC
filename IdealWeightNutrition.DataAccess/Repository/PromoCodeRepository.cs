using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdealWeightNutrition.DataAccess.Repository
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
           
            
            // Use Entity Framework's Update method directly
            _db.PromoCodes.Update(obj);
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
            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
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

            var now = IdealWeightNutrition.Utility.DateTimeHelper.Now;
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

