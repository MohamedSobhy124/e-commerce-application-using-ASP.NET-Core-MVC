using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class BlogPostRepository : Repository<BlogPost>, IBlogPostRepository
    {
        private ApplicationDBContext _db;

        public BlogPostRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void update(BlogPost obj)
        {
            if (obj is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            }
            _db.BlogPosts.Update(obj);
        }
    }
}
