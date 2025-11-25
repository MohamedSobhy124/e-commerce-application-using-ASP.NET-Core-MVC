using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.DataAccess.Repository
{
    public class ProductVariantRepository : Repository<ProductVariant>, IProductVariantRepository
    {
        private ApplicationDBContext _db;

        public ProductVariantRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductVariant obj)
        {
            var objFromDb = _db.ProductVariants.FirstOrDefault(a => a.Id == obj.Id);
            if (objFromDb != null)
            {
                objFromDb.ProductId = obj.ProductId;
                objFromDb.SKU = obj.SKU;
                objFromDb.Price = obj.Price;
                objFromDb.ListPrice = obj.ListPrice;
                objFromDb.StockQuantity = obj.StockQuantity;
                objFromDb.MinimumStockAlert = obj.MinimumStockAlert;
                objFromDb.ImageUrl = obj.ImageUrl;
            }
        }

        public ProductVariant? GetVariantByOptionValues(int productId, List<int> optionValueIds)
        {
            if (optionValueIds == null || !optionValueIds.Any())
                return null;

            // Get all variants for this product
            var variants = _db.ProductVariants
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.OptionValue)
                .Where(v => v.ProductId == productId)
                .ToList();

            // Find variant that matches all option values
            foreach (var variant in variants)
            {
                var variantOptionValueIds = variant.VariantOptionValues
                    .Select(vov => vov.OptionValue.Id)
                    .OrderBy(id => id)
                    .ToList();

                var requestedIds = optionValueIds.OrderBy(id => id).ToList();

                if (variantOptionValueIds.Count == requestedIds.Count &&
                    variantOptionValueIds.SequenceEqual(requestedIds))
                {
                    return variant;
                }
            }

            return null;
        }
    }
}

