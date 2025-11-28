using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class StockNotificationRepository : Repository<StockNotification>, IStockNotificationRepository
    {
        private ApplicationDBContext _db;
        
        public StockNotificationRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(StockNotification obj)
        {
            _db.StockNotifications.Update(obj);
        }
        
        public void Add(StockNotification obj)
        {
            _db.StockNotifications.Add(obj);
        }
        
        public StockNotification? GetByProductAndEmail(int productId, string email, int? variantId = null)
        {
            return _db.StockNotifications
                .FirstOrDefault(n => n.ProductId == productId 
                    && n.Email.ToLower() == email.ToLower() 
                    && n.ProductVariantId == variantId
                    && n.IsActive);
        }
        
        public bool IsEmailSubscribed(int productId, string email, int? variantId = null)
        {
            return _db.StockNotifications
                .Any(n => n.ProductId == productId 
                    && n.Email.ToLower() == email.ToLower() 
                    && n.ProductVariantId == variantId
                    && n.IsActive);
        }
        
        public IEnumerable<StockNotification> GetActiveNotifications(int productId, int? variantId = null)
        {
            return _db.StockNotifications
                .Where(n => n.ProductId == productId 
                    && n.ProductVariantId == variantId
                    && n.IsActive
                    && !n.IsNotified);
        }
    }
}

