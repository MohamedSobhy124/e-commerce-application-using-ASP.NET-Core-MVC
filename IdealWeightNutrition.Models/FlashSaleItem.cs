using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class FlashSaleItem : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Flash Sale")]
        public int FlashSaleId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Display(Name = "Product Variant")]
        public int? ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Flash Sale Quantity")]
        public int FlashSaleQuantity { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Flash Sale Quantity")]
        public int FlashSaleQuantityCreated { get; set; }= 0;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Flash Sale Price")]
        public decimal FlashSalePrice { get; set; }

        [Display(Name = "Date Added")]
        public DateTime AddedDate { get; set; } = DateTime.Now;
        
        // Note: FlashSaleItem inherits audit fields from BaseEntity

        // Navigation properties
        [ForeignKey(nameof(FlashSaleId))]
        [ValidateNever]
        public FlashSale FlashSale { get; set; }

        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        [ValidateNever]
        public ProductVariant? ProductVariant { get; set; }

        // Calculated properties
        [NotMapped]
        public bool IsAvailable => FlashSaleQuantity > 0;

        [NotMapped]
        public decimal Savings
        {
            get
            {
                decimal originalPrice = ProductVariant?.Price==0 ?( (decimal)Product.Price): ProductVariant!.Price;
                return originalPrice - FlashSalePrice;
            }
        }

        [NotMapped]
        public double DiscountPercentage
        {
            get
            {
                decimal originalPrice =( ProductVariant is null || ProductVariant?.Price == 0 )? ((decimal)Product.Price) : ProductVariant!.Price;
                if (originalPrice <= 0) return 0;
                return Math.Round(((double)originalPrice - (double)FlashSalePrice) / (double)originalPrice * 100, 2);
            }
        }
    }
}
