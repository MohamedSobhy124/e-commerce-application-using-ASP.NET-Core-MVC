using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class FlashSaleItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Flash Sale")]
        public int FlashSaleId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

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

        // Navigation properties
        [ForeignKey(nameof(FlashSaleId))]
        [ValidateNever]
        public FlashSale FlashSale { get; set; }

        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        // Calculated properties
        [NotMapped]
        public bool IsAvailable => FlashSaleQuantity > 0;

        [NotMapped]
        public decimal Savings
        {
            get
            {
                if (Product == null) return 0;
                return (decimal)Product.Price - FlashSalePrice;
            }
        }

        [NotMapped]
        public double DiscountPercentage
        {
            get
            {
                if (Product == null || Product.Price <= 0) return 0;
                return Math.Round(((Product.Price - (double)FlashSalePrice) / Product.Price) * 100, 2);
            }
        }
    }
}
