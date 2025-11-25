    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
     public interface IUnitOfWork
    {
        ICategryReprository categry{ get; }
        IProductReprository product { get; }
        ICompanyRepository company { get; }
        IShoppingCartReprository shoppingCart { get; }
        IApplicationUserReprository applicationUser { get; }
        IOrderDetailRepository OrderDetail { get; }
        IOrderHeaderRepository OrderHeader { get; }
        INotificationRepository notification { get; }
        IReviewRepository review { get; }
        IFlashSaleRepository FlashSale { get; }
        IFlashSaleItemRepository FlashSaleItem { get; }
        IPromoCodeRepository PromoCode { get; }
        IPromoCodeUsageRepository PromoCodeUsage { get; }
        IWishlistRepository wishlist { get; }
        IServicePurchaseRepository ServicePurchases { get; }
        IServiceSubscriptionRepository ServiceSubscriptions { get; }
        IServiceOfferRepository ServiceOffers { get; }
        INewsletterSubscriptionRepository NewsletterSubscription { get; }
        IProductOptionRepository ProductOption { get; }
        IProductOptionValueRepository ProductOptionValue { get; }
        IProductVariantRepository ProductVariant { get; }
        void save();
    }
}
