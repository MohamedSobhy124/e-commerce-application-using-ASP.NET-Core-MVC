using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDBContext _db;
        public ICompanyRepository company { get; private set; }

        public ICategryReprository categry { get; private set; }
        public IBrandRepository brand { get; private set; }
        public IProductReprository product { get; private set; }

        public IApplicationUserReprository   applicationUser { get; private set; }

        public IShoppingCartReprository shoppingCart  { get; private set; }
        public IOrderHeaderRepository OrderHeader { get; private set; }
        public IOrderDetailRepository OrderDetail { get; private set; }
        public INotificationRepository notification { get; private set; }
        public IReviewRepository review { get; private set; }
        public IFlashSaleRepository FlashSale { get; private set; }
        public IFlashSaleItemRepository FlashSaleItem { get; private set; }
        public IPromoCodeRepository PromoCode { get; private set; }
        public IPromoCodeUsageRepository PromoCodeUsage { get; private set; }
        public IWishlistRepository wishlist { get; private set; }
        public IServicePurchaseRepository ServicePurchases { get; private set; }
        public IServiceSubscriptionRepository ServiceSubscriptions { get; private set; }
        public IServiceOfferRepository ServiceOffers { get; private set; }
        public INewsletterSubscriptionRepository NewsletterSubscription { get; private set; }
        public IProductOptionRepository ProductOption { get; private set; }
        public IProductOptionValueRepository ProductOptionValue { get; private set; }
        public IProductVariantRepository ProductVariant { get; private set; }
        public IStockNotificationRepository StockNotification { get; private set; }
        public IComboOfferRepository ComboOffer { get; private set; }
        public IComboOfferItemRepository ComboOfferItem { get; private set; }
        public IBrandRepository Brand { get; private set; }

        public UnitOfWork(ApplicationDBContext db) 
        {
            _db = db;
            categry=new CategryReprository(_db);
            brand = new BrandRepository(_db);
            product = new ProductReprository(_db);
            company = new CompanyReprository(_db);
            applicationUser = new ApplicationUserReprository(_db);
            shoppingCart =new ShoppingCartReprository(_db);
            OrderHeader = new OrderHeaderRepository(_db);
            OrderDetail = new OrderDetailRepository(_db);
            notification = new NotificationRepository(_db);
            review = new ReviewRepository(_db);
            FlashSale = new FlashSaleRepository(_db);
            FlashSaleItem = new FlashSaleItemRepository(_db);
            PromoCode = new PromoCodeRepository(_db);
            PromoCodeUsage = new PromoCodeUsageRepository(_db);
            wishlist = new WishlistRepository(_db);
            ServiceOffers = new ServiceOfferRepository(_db);
            ServiceSubscriptions = new ServiceSubscriptionRepository(_db);
            ServicePurchases = new ServicePurchaseRepository(_db);
            NewsletterSubscription = new NewsletterSubscriptionRepository(_db);
            ProductOption = new ProductOptionRepository(_db);
            ProductOptionValue = new ProductOptionValueRepository(_db);
            ProductVariant = new ProductVariantRepository(_db);
            StockNotification = new StockNotificationRepository(_db);
            ComboOffer = new ComboOfferRepository(_db);
            ComboOfferItem = new ComboOfferItemRepository(_db);
            Brand = new BrandRepository(_db);

        }

        public void save()
        {
            _db.SaveChanges();
        }
    }
}
