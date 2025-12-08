using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class PromoCodeExcludedComboOffer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromoCodeId { get; set; }

        [ForeignKey("PromoCodeId")]
        [ValidateNever]
        public PromoCode PromoCode { get; set; }

        [Required]
        public int ComboOfferId { get; set; }

        [ForeignKey("ComboOfferId")]
        [ValidateNever]
        public ComboOffer ComboOffer { get; set; }
    }
}

