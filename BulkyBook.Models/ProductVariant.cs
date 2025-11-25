using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace BulkyBook.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
        
        [Display(Name = "SKU")]
        [StringLength(100)]
        public string? SKU { get; set; }
        
        [Display(Name = "Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Display(Name = "List Price")]
        [Range(0, double.MaxValue, ErrorMessage = "List price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ListPrice { get; set; }
        
        [Display(Name = "Price For 50+")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price50 { get; set; }
        
        [Display(Name = "Price For 100+")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price100 { get; set; }
        
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        public int StockQuantity { get; set; } = 0;
        
        [Display(Name = "Minimum Stock Alert")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be 0 or greater")]
        public int MinimumStockAlert { get; set; } = 5;
        
        [Display(Name = "Variant Image")]
        [ValidateNever]
        public string? ImageUrl { get; set; }
        
        // Navigation property for variant option values (many-to-many relationship)
        [ValidateNever]
        public ICollection<ProductVariantOptionValue> VariantOptionValues { get; set; } = new List<ProductVariantOptionValue>();
        
        // Calculated properties
        [NotMapped]
        public bool IsInStock => StockQuantity > 0;
        
        [NotMapped]
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= MinimumStockAlert;
        
        [NotMapped]
        public bool IsOutOfStock => StockQuantity == 0;
        
        // Helper property to get variant name (e.g., "S - Red")
        [NotMapped]
        public string VariantName
        {
            get
            {
                if (VariantOptionValues == null || !VariantOptionValues.Any())
                    return "Default";
                    
                return string.Join(" / ", VariantOptionValues
                    .OrderBy(vov => vov.OptionValue?.ProductOption?.DisplayOrder ?? 0)
                    .ThenBy(vov => vov.OptionValue?.DisplayOrder ?? 0)
                    .Select(vov => $"{vov.OptionValue?.ProductOption?.Name}: {vov.OptionValue?.Value}"));
            }
        }
    }
}

