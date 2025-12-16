using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class ComboOfferItem : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Combo Offer")]
        public int ComboOfferId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Display(Name = "Product Variant")]
        public int? ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Display order must be 0 or greater")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Required")]
        public bool IsRequired { get; set; } = true;

        // Navigation properties
        [ForeignKey(nameof(ComboOfferId))]
        [ValidateNever]
        public ComboOffer ComboOffer { get; set; }

        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        [ValidateNever]
        public ProductVariant? ProductVariant { get; set; }

        // Calculated properties
        [NotMapped]
        public bool IsAvailable
        {
            get
            {
                if (ProductVariant != null && !ProductVariant.IsDeleted)
                {
                    return ProductVariant.StockQuantity >= Quantity;
                }
                else if (Product != null)
                {
                    return Product.StockQuantity >= Quantity;
                }
                return false;
            }
        }

        [NotMapped]
        public decimal ItemPrice
        {
            get
            {
                if (ProductVariant != null && !ProductVariant.IsDeleted)
                {
                    return  ProductVariant.ListPrice ?? 0 * Quantity;
                }
                else if (Product != null)
                {
                    return (decimal)Product.ListPrice * Quantity;
                }
                return 0;
            }
        }

        [NotMapped]
        public string ProductName
        {
            get
            {
                if (Product == null) return "Unknown Product";
                // Return localized name based on current culture (handled in views)
                return Product.Title;
            }
        }
    }
}










