using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace IdealWeightNutrition.Models
{
    public class FlashSale : BaseEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Flash sale name is required")]
        [StringLength(100)]
        [Display(Name = "Flash Sale Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Flash sale name in Arabic is required")]
        [StringLength(100)]
        [Display(Name = "Flash Sale Name (Arabic)")]
        public string NameAr { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Description (Arabic)")]
        public string DescriptionAr { get; set; }

        [StringLength(500)]
        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation property for items in this flash sale
        [ValidateNever]
        public ICollection<FlashSaleItem> FlashSaleItems { get; set; } = new List<FlashSaleItem>();

        // Calculated properties
        [NotMapped]
        public bool IsCurrentlyActive
        {
            get
            {
                var now = DateTime.Now;
                return IsActive && now >= StartDate && now <= EndDate && HasAvailableStock;
            }
        }

        [NotMapped]
        public bool HasStarted => DateTime.Now >= StartDate;

        [NotMapped]
        public bool HasEnded => DateTime.Now > EndDate;

        [NotMapped]
        public bool HasAvailableStock => FlashSaleItems != null && FlashSaleItems.Any(item => item.FlashSaleQuantity > 0);

        [NotMapped]
        public TimeSpan TimeRemaining
        {
            get
            {
                if (HasEnded) return TimeSpan.Zero;
                if (!HasStarted) return StartDate - DateTime.Now;
                return EndDate - DateTime.Now;
            }
        }

        [NotMapped]
        public int TotalProducts => FlashSaleItems?.Count ?? 0;

        [NotMapped]
        public int TotalAvailableItems => FlashSaleItems?.Sum(i => i.FlashSaleQuantity) ?? 0;
    }
}

