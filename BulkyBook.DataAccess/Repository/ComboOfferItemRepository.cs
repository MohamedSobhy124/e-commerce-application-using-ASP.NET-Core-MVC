using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Repository
{
    public class ComboOfferItemRepository : Repository<ComboOfferItem>, IComboOfferItemRepository
    {
        private readonly ApplicationDBContext _db;

        public ComboOfferItemRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ComboOfferItem comboOfferItem)
        {
            // Set audit fields
            if (comboOfferItem is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
            }
            _db.ComboOfferItems.Update(comboOfferItem);
        }

        public void Add(ComboOfferItem comboOfferItem)
        {
            _db.ComboOfferItems.Add(comboOfferItem);
        }  
        
      

        public void Remove(ComboOfferItem comboOfferItem)
        {
            // Soft delete for BaseEntity types
            if (comboOfferItem is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
                _db.ComboOfferItems.Update(comboOfferItem);
            }
            else
            {
                _db.ComboOfferItems.Remove(comboOfferItem);
            }
        }

        public IEnumerable<ComboOfferItem> GetItemsByComboOfferId(int comboOfferId)
        {
            return _db.ComboOfferItems
                .AsNoTracking()
                .Where(item => item.ComboOfferId == comboOfferId && !item.IsDeleted)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Id)
                .ToList();
        }

        public IEnumerable<ComboOfferItem> GetItemsWithProducts(int comboOfferId)
        {
            return _db.ComboOfferItems
                .AsNoTracking()
                .Include(item => item.Product)
                    .ThenInclude(p => p.ProductImages.Where(img => img.ImageInfo == null))
                .Include(item => item.Product)
                    .ThenInclude(p => p.categry)
                .Include(item => item.ProductVariant)
                .Where(item => item.ComboOfferId == comboOfferId && !item.IsDeleted)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Id)
                .ToList();
        }
    }
}

