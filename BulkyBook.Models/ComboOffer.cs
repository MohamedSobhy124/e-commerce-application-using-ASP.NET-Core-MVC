using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace BulkyBook.Models
{
    public class ComboOffer : BaseEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Combo offer name is required")]
        [StringLength(100)]
        [Display(Name = "Combo Offer Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Combo offer name in Arabic is required")]
        [StringLength(100)]
        [Display(Name = "Combo Offer Name (Arabic)")]
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
        [Range(0.01, double.MaxValue, ErrorMessage = "Combo price must be greater than 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Combo Price")]
        public decimal ComboPrice { get; set; }

        [NotMapped]
        [Display(Name = "Original Total Price")]
        public decimal OriginalTotalPrice
        {
            get
            {
                if (ComboOfferItems == null || !ComboOfferItems.Any())
                    return 0;

                return ComboOfferItems
                    .Where(item => !item.IsDeleted && item.Product != null)
                    .Sum(item =>
                    {
                        var product = item.Product;
                        var variant = item.ProductVariant;
                        decimal price = 0;

                        if (variant != null && !variant.IsDeleted)
                        {
                            price = (decimal)variant.Price;
                        }
                        else if (product != null)
                        {
                            price = (decimal)product.Price;
                        }

                        return price * item.Quantity;
                    });
            }
        }

        [NotMapped]
        [Display(Name = "Discount Percentage")]
        public decimal DiscountPercentage
        {
            get
            {
                if (OriginalTotalPrice == 0)
                    return 0;

                var savings = OriginalTotalPrice - ComboPrice;
                return (savings / OriginalTotalPrice) * 100;
            }
        }

        [NotMapped]
        [Display(Name = "Total Savings")]
        public decimal TotalSavings => OriginalTotalPrice - ComboPrice;

        [Required]
        [Display(Name = "Start Date & Time")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date & Time")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Minimum Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Minimum quantity must be at least 1")]
        public int MinimumQuantity { get; set; } = 1;

        [Display(Name = "Maximum Quantity Per Customer")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximum quantity must be 0 (unlimited) or greater")]
        public int? MaximumQuantity { get; set; }

        [Display(Name = "Display Order")]
        [Range(0, int.MaxValue, ErrorMessage = "Display order must be 0 or greater")]
        public int DisplayOrder { get; set; } = 0;

        // Navigation property for items in this combo
        [ValidateNever]
        public ICollection<ComboOfferItem> ComboOfferItems { get; set; } = new List<ComboOfferItem>();
        
        // Navigation property for images
        [ValidateNever]
        public ICollection<ComboOfferImage> ComboOfferImages { get; set; } = new List<ComboOfferImage>();

        // Calculated properties
        [NotMapped]
        public bool IsCurrentlyActive
        {
            get
            {
                DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai"));

                return IsActive && 
                       !IsDeleted && 
                       now >= StartDate && 
                       now <= EndDate && 
                       HasAvailableStock &&
                       ComboOfferItems != null && 
                       ComboOfferItems.Any(item => !item.IsDeleted);
            }
        }

        [NotMapped]
        public bool HasStarted => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai")) >= StartDate;

        [NotMapped]
        public bool HasEnded => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai")) > EndDate;

        [NotMapped]
        public bool HasAvailableStock
        {
            get
            {
                if (ComboOfferItems == null || !ComboOfferItems.Any())
                    return false;

                return ComboOfferItems
                    .Where(item => !item.IsDeleted && item.IsRequired)
                    .All(item =>
                    {
                        if (item.ProductVariant != null && !item.ProductVariant.IsDeleted)
                        {
                            return item.ProductVariant.StockQuantity >= item.Quantity;
                        }
                        else if (item.Product != null)
                        {
                            return item.Product.StockQuantity >= item.Quantity;
                        }
                        return false;
                    });
            }
        }

        [NotMapped]
        public TimeSpan TimeRemaining
        {
            get
            {
                if (HasEnded) return TimeSpan.Zero;
                if (!HasStarted) return StartDate - TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai"));
                return EndDate - TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("Asia/Dubai"));
            }
        }

        [NotMapped]
        public int TotalProducts => ComboOfferItems?.Count(item => !item.IsDeleted) ?? 0;

        [NotMapped]
        public int RequiredProductsCount => ComboOfferItems?.Count(item => !item.IsDeleted && item.IsRequired) ?? 0;
    }
}

