using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IFlashSaleRepository : IRepository<FlashSale>
    {
        void Update(FlashSale flashSale);
        void Add(FlashSale flashSale);
        void Remove(FlashSale flashSale);
        
        /// <summary>
        /// Gets all currently active flash sales (started, not ended, has items with quantity)
        /// </summary>
        IEnumerable<FlashSale> GetActiveFlashSales();
        
        /// <summary>
        /// Gets flash sale with all items by ID
        /// </summary>
        FlashSale GetFlashSaleWithItems(int flashSaleId);
    }
}

