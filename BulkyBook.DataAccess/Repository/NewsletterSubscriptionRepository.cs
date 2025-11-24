using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class NewsletterSubscriptionRepository : Repository<NewsletterSubscription>, INewsletterSubscriptionRepository
    {
        private ApplicationDBContext _db;
        
        public NewsletterSubscriptionRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(NewsletterSubscription obj)
        {
            _db.NewsletterSubscriptions.Update(obj);
        }
        
        public void Add(NewsletterSubscription obj)
        {
            _db.NewsletterSubscriptions.Add(obj);
        }
        
        public NewsletterSubscription? GetByEmail(string email)
        {
            return _db.NewsletterSubscriptions.FirstOrDefault(n => n.Email.ToLower() == email.ToLower());
        }
        
        public bool IsEmailSubscribed(string email)
        {
            return _db.NewsletterSubscriptions
                .Any(n => n.Email.ToLower() == email.ToLower() && n.IsActive);
        }
    }
}

