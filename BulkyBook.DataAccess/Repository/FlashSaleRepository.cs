using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Repository
{
    public class FlashSaleRepository : Repository<FlashSale>, IFlashSaleRepository
    {
        private readonly ApplicationDBContext _db;

        public FlashSaleRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(FlashSale flashSale)
        {
            // Set audit fields
            if (flashSale is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = DateTime.Now;
            }
            _db.FlashSales.Update(flashSale);
        }
           
        public void Add(FlashSale flashSale)
        {
            _db.FlashSales.Add(flashSale);
        }
          
        public void Remove(FlashSale flashSale)
        {
            // Soft delete for BaseEntity types
            if (flashSale is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.ModifiedDate = DateTime.Now;
                _db.FlashSales.Update(flashSale);
            }
            else
            {
                _db.FlashSales.Remove(flashSale);
            }
        }

        // PERFORMANCE: Optimized with AsNoTracking and filtered includes
        public IEnumerable<FlashSale> GetActiveFlashSales()
        {
            var now = DateTime.Now;
            var flashSales = _db.FlashSales
                .AsNoTracking() // Read-only query
                .Include(f => f.FlashSaleItems.Where(i => !i.IsDeleted && i.FlashSaleQuantity > 0))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(f => !f.IsDeleted 
                    && f.IsActive 
                    && f.StartDate <= now 
                    && f.EndDate >= now)
                .OrderBy(f => f.EndDate)
                .ToList();
            
            // Load ProductVariant data separately to avoid duplicate Include issue
            // EF Core doesn't allow multiple Include on the same navigation with different ThenInclude paths
            var flashSaleItemIds = flashSales
                .SelectMany(f => f.FlashSaleItems)
                .Where(i => i.ProductVariantId.HasValue)
                .Select(i => i.ProductVariantId.Value)
                .Distinct()
                .ToList();
            
            if (flashSaleItemIds.Any())
            {
                var variants = _db.ProductVariants
                    .AsNoTracking()
                    .Include(v => v.VariantOptionValues.Where(vov => vov.OptionValue != null && !vov.OptionValue.IsDeleted))
                        .ThenInclude(vov => vov.OptionValue)
                            .ThenInclude(ov => ov.ProductOption)
                    .Where(v => flashSaleItemIds.Contains(v.Id) && !v.IsDeleted)
                    .ToList();
                
                // Attach variants to flash sale items (works with AsNoTracking)
                var variantDict = variants.ToDictionary(v => v.Id);
                foreach (var flashSale in flashSales)
                {
                    foreach (var item in flashSale.FlashSaleItems.Where(i => i.ProductVariantId.HasValue))
                    {
                        if (variantDict.TryGetValue(item.ProductVariantId.Value, out var variant))
                        {
                            item.ProductVariant = variant;
                        }
                    }
                }
            }
            
            return flashSales;
        }

        // PERFORMANCE: Optimized with AsNoTracking
        public FlashSale GetFlashSaleWithItems(int flashSaleId)
        {
            return _db.FlashSales
                .AsNoTracking() // Read-only query
                .Include(f => f.FlashSaleItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(f => f.FlashSaleItems.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.VariantOptionValues)
                            .ThenInclude(vov => vov.OptionValue)
                                .ThenInclude(ov => ov.ProductOption)
                .Where(f => f.Id == flashSaleId && !f.IsDeleted)
                .FirstOrDefault();
        }
    }
}

