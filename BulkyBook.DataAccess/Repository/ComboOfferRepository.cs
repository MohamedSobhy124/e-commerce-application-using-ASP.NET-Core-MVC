using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Repository
{
    public class ComboOfferRepository : Repository<ComboOffer>, IComboOfferRepository
    {
        private readonly ApplicationDBContext _db;

        public ComboOfferRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ComboOffer comboOffer)
        {
            // Set audit fields
            if (comboOffer is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
            }
            _db.ComboOffers.Update(comboOffer);
        }   

        public void Add(ComboOffer comboOffer)
        {
            _db.ComboOffers.Add(comboOffer);
        }

        public void Remove(ComboOffer comboOffer)
        {
            // Soft delete for BaseEntity types
            if (comboOffer is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
                _db.ComboOffers.Update(comboOffer);
            }
            else
            {
                _db.ComboOffers.Remove(comboOffer);
            }
        }

        // PERFORMANCE: Optimized with AsNoTracking and filtered includes
        public IEnumerable<ComboOffer> GetActiveComboOffers()
        {
            var now = BulkyBook.Utility.DateTimeHelper.Now;
            var comboOffers = _db.ComboOffers
                .AsNoTracking() // Read-only query
                .Include(co => co.ComboOfferItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(co => co.ComboOfferItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                .Include(co => co.ComboOfferImages.OrderBy(img => img.DisplayOrder))
                .Where(co => !co.IsDeleted 
                    && co.IsActive 
                    && co.StartDate <= now 
                    && co.EndDate >= now
                    && co.ComboOfferItems.Any(i => !i.IsDeleted))
                .OrderBy(co => co.DisplayOrder)
                .ThenBy(co => co.CreatedDate)
                .ToList();

            return comboOffers;
        }

        public ComboOffer? GetComboOfferWithItems(int id)
        {
            return _db.ComboOffers
                .Include(co => co.ComboOfferItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(co => co.ComboOfferItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                .Include(co => co.ComboOfferItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.categry)
                .Include(co => co.ComboOfferImages.OrderBy(img => img.DisplayOrder))
                .FirstOrDefault(co => co.Id == id && !co.IsDeleted);
        }
    }
}

