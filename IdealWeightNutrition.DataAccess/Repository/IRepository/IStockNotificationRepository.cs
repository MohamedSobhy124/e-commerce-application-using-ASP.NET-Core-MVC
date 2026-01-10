using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IStockNotificationRepository : IRepository<StockNotification>
    {
        void Update(StockNotification obj);
        void Add(StockNotification obj);
        StockNotification? GetByProductAndEmail(int productId, string email, int? variantId = null);
        bool IsEmailSubscribed(int productId, string email, int? variantId = null);
        IEnumerable<StockNotification> GetActiveNotifications(int productId, int? variantId = null);
    }
}

