using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IPromoCodeRepository : IRepository<PromoCode>
    {
        void Update(PromoCode promoCode);
        PromoCode GetByCode(string code);
        bool IsCodeAvailable(string code, int? excludeId = null);
        IEnumerable<PromoCode> GetActivePromoCodes();
        bool CanUserUsePromoCode(int promoCodeId, string userId);
        int GetUserPromoCodeUsageCount(int promoCodeId, string userId);
        void IncrementUsage(int promoCodeId);
    }
}

