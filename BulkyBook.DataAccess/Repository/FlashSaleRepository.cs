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
            _db.FlashSales.Update(flashSale);
        }
           
        public void Add(FlashSale flashSale)
        {
            _db.FlashSales.Add(flashSale);
        }
          
        public void Remove(FlashSale flashSale)
        {
            _db.FlashSales.Remove(flashSale);
        }

        public IEnumerable<FlashSale> GetActiveFlashSales()
        {
            var now = DateTime.Now;
            return _db.FlashSales
                .Include(f => f.FlashSaleItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .Where(f => f.IsActive 
                    && f.StartDate <= now 
                    && f.EndDate >= now 
                    && f.FlashSaleItems.Any(i => i.FlashSaleQuantity > 0))
                .OrderBy(f => f.EndDate)
                .ToList();
        }

        public FlashSale GetFlashSaleWithItems(int flashSaleId)
        {
            return _db.FlashSales
                .Include(f => f.FlashSaleItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefault(f => f.Id == flashSaleId);
        }
    }
}

