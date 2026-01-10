using IdealWeightNutrition.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IPromoCodeUsageRepository : IRepository<PromoCodeUsage>
    {
        void RecordUsage(int promoCodeId, string userId, int orderId);
        int GetUsageCount(int promoCodeId, string userId);
    }
}

