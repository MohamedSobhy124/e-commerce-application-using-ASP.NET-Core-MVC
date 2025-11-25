using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class ProductOptionValue
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductOptionId { get; set; }
        
        [ForeignKey("ProductOptionId")]
        [ValidateNever]
        public ProductOption ProductOption { get; set; }
        
        [Required]
        [Display(Name = "Value")]
        [StringLength(100)]
        public string Value { get; set; } // e.g., "S", "M", "L" for Size or "Red", "Black" for Color
        
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }
}

