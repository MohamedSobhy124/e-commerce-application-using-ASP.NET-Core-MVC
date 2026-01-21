using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class PromoCodeExcludedServiceSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PromoCodeId { get; set; }

        [ForeignKey("PromoCodeId")]
        [ValidateNever]
        public PromoCode PromoCode { get; set; }

        [Required]
        public int ServiceSubscriptionId { get; set; }

        [ForeignKey("ServiceSubscriptionId")]
        [ValidateNever]
        public ServiceSubscription ServiceSubscription { get; set; }
    }
}
