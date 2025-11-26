using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class ProductOption : BaseEntity
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
        
        [Required]
        [Display(Name = "Option Name")]
        [StringLength(100)]
        public string Name { get; set; } // e.g., "Size", "Color", "Flavor"
        
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
        
        // Navigation property for option values
        [ValidateNever]
        public ICollection<ProductOptionValue> OptionValues { get; set; } = new List<ProductOptionValue>();
    }
}

