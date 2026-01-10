using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class PromoCodeUsage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromoCodeId { get; set; }
        
        [ForeignKey("PromoCodeId")]
        [ValidateNever]
        public PromoCode PromoCode { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime UsedDate { get; set; } = DateTime.Now;

        public int OrderId { get; set; }
        
        [ForeignKey("OrderId")]
        [ValidateNever]
        public OrderHeader OrderHeader { get; set; }
    }
}

