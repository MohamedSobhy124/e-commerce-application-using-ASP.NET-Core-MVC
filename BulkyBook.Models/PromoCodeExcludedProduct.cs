using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class PromoCodeExcludedProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromoCodeId { get; set; }

        [ForeignKey("PromoCodeId")]
        [ValidateNever]
        public PromoCode PromoCode { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }
    }
}

