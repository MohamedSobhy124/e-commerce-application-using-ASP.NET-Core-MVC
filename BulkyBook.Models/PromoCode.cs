using System;
using System.ComponentModel.DataAnnotations;

namespace BulkyBook.Models
{
    public class PromoCode
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Promo code is required")]
        [StringLength(50)]
        [Display(Name = "Promo Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Discount type is required")]
        [Display(Name = "Discount Type")]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0.01, 100000, ErrorMessage = "Discount value must be greater than 0")]
        [Display(Name = "Discount Value")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Minimum Order Amount")]
        [Range(0, 1000000, ErrorMessage = "Minimum order amount must be 0 or greater")]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Maximum Discount Amount")]
        [Range(0, 100000, ErrorMessage = "Maximum discount amount must be 0 or greater")]
        public decimal? MaximumDiscountAmount { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Usage Limit")]
        [Range(0, int.MaxValue, ErrorMessage = "Usage limit must be 0 or greater")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Times Used")]
        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Usage Limit Per User")]
        [Range(1, 100, ErrorMessage = "Usage limit per user must be between 1 and 100")]
        public int? UsageLimitPerUser { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        // Navigation properties
        public ICollection<OrderHeader>? Orders { get; set; }
    }

    public enum DiscountType
    {
        [Display(Name = "Percentage")]
        Percentage = 1,
        [Display(Name = "Fixed Amount")]
        FixedAmount = 2
    }
}

