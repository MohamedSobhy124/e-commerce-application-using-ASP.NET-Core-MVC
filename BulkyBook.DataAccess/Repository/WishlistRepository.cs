using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository
{
    public class WishlistRepository : Repository<Wishlist>, IWishlistRepository
    {
        private ApplicationDBContext _db;
        public WishlistRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Wishlist obj)
        {
            _db.Wishlists.Update(obj);
        } 
        public void Add(Wishlist obj)
        {
            _db.Wishlists.Add(obj);
        }  
        public void Remove(Wishlist obj)
        {
            _db.Wishlists.Remove(obj);
        }
    }
}

