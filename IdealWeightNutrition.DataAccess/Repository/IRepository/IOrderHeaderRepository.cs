using IdealWeightNutrition.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);
        Task UpdateStatus(int id, string orderStatus, string? paymentStatus = null);
        void UpdatePaymentID(int id, string sessionId, string paymentIntentId);
        
        [Obsolete("Use UpdatePaymentID instead")]
        void UpdateStripePaymentID(int id, string sessionId, string paymentIntentId);
    }
}
