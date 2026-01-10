using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    // Junction table for many-to-many relationship between ProductVariant and ProductOptionValue
    public class ProductVariantOptionValue
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductVariantId { get; set; }
        
        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant ProductVariant { get; set; }
        
        [Required]
        public int ProductOptionValueId { get; set; }
        
        [ForeignKey("ProductOptionValueId")]
        [ValidateNever]
        public ProductOptionValue OptionValue { get; set; }
    }
}

