using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class Product : BaseEntity
    {
        public int Id { get; set; }
        [Required]
        public   String Title { get; set; }
        [Required]
        public string Description { get; set; }

        public   string? Author { get; set; }
        public   string? ISBN { get; set; }
        [Range(0, 1000)]
        [Display(Name = "List Price")]
        [Required]
        public   double ListPrice { get; set; }
        [Range(0, 1000)]
        [Display(Name = "Price For 1-50")]
        [Required]
        public   double Price { get; set; }
        [Range(0,100)]
        [Display(Name = "Price For 50+")]
        public   double? Price50 { get; set; }
        [Range(0, 1000)]
        [Display(Name = "Price For 100+")]
        public   double? Price100 { get; set; }
        [Required]
        public int CategryId { get; set; }
        [ForeignKey("CategryId")]
        [ValidateNever]
        public Categry categry { get; set; }
        [ValidateNever]
        public string ImageUrl { get; set; }
        
        // Stock Management
        [Display(Name = "Stock Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        public int StockQuantity { get; set; } = 0;
        
        [Display(Name = "Minimum Stock Alert")]
        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock must be 0 or greater")]
        public int MinimumStockAlert { get; set; } = 5;
        
        // Navigation property for multiple images
        [ValidateNever]
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        
        // Calculated property for stock status
        [NotMapped]
        public bool IsInStock => StockQuantity > 0;
        
        [NotMapped]
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= MinimumStockAlert;
        
        [NotMapped]
        public bool IsOutOfStock => StockQuantity == 0;
        
        // Variant System Support
        [Display(Name = "Product Type")]
        [Required]
        public ProductType ProductType { get; set; } = ProductType.Simple;
        
        // Navigation properties for variant system
        [ValidateNever]
        public ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();
        
        [ValidateNever]
        public ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        
        // Helper property to check if product has variants
        [NotMapped]
        public bool HasVariants => ProductType == ProductType.Variable && ProductVariants != null && ProductVariants.Any();
        
        // Helper property to get default variant (for simple products or fallback)
        [NotMapped]
        public ProductVariant? DefaultVariant => ProductVariants?.FirstOrDefault();
    }
    
    public enum ProductType
    {
        Simple = 0,
        Variable = 1
    }
}
