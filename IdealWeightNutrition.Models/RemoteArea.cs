using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class RemoteArea
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Area Name")]
        public string Name { get; set; } = default!;

        [Display(Name = "Area Name (Arabic)")]
        public string? NameAr { get; set; }

        [Required]
        [Display(Name = "City")]
        public int CityId { get; set; }

        [ForeignKey("CityId")]
        [ValidateNever]
        public City City { get; set; } = null!;

        [Required]
        [Display(Name = "Delivery Charge")]
        [Range(0, double.MaxValue, ErrorMessage = "Delivery charge must be 0 or greater")]
        public double DeliveryCharge { get; set; } = 0;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;
    }
}
