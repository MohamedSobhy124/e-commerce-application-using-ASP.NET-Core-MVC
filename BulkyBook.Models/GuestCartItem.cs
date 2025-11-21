using System;

namespace BulkyBook.Models
{
    public class GuestCartItem
    {
        public int ProductId { get; set; }
        public int Count { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        
        // Flash Sale Support
        public int? FlashSaleItemId { get; set; }
        public double? FlashSalePrice { get; set; }
        
        // Helper properties for display (not persisted)
        public string ProductTitle { get; set; }
        public double ProductPrice { get; set; }
        
        public bool IsFlashSale => FlashSaleItemId.HasValue;
    }
}

