using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IWishlistRepository : IRepository<Wishlist>
    {
        void Update(Wishlist obj);
        void Add(Wishlist obj);
        void Remove(Wishlist obj);
    }
}

