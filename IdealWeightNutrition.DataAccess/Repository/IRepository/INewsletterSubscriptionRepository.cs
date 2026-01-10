using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface INewsletterSubscriptionRepository : IRepository<NewsletterSubscription>
    {
        void Update(NewsletterSubscription obj);
        void Add(NewsletterSubscription obj);
        NewsletterSubscription? GetByEmail(string email);
        bool IsEmailSubscribed(string email);
    }
}

