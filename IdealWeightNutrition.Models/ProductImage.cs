using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
        
        [Required]
        public string ImageUrl { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
        
        [StringLength(500)]
        public string? ImageInfo { get; set; }
    }
}

