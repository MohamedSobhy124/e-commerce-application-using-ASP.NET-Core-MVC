using System;

namespace IdealWeightNutrition.Models
{
    public class GuestCartItem
    {
        public int ProductId { get; set; }
        public int Count { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        
        // Variant Support
        public int? ProductVariantId { get; set; }
        
        // Flash Sale Support
        public int? FlashSaleItemId { get; set; }
        public double? FlashSalePrice { get; set; }
        
        // Combo Offer Support
        public int? ComboOfferId { get; set; }
        
        // Helper properties for display (not persisted)
        public string ProductTitle { get; set; }
        public double ProductPrice { get; set; }
        
        public bool IsFlashSale => FlashSaleItemId.HasValue;
        public bool IsComboOffer => ComboOfferId.HasValue;
    }
}

