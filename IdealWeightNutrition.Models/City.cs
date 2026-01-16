using System.ComponentModel.DataAnnotations;

namespace IdealWeightNutrition.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "City Name")]
        public string Name { get; set; } = default!;

        [Display(Name = "City Name (Arabic)")]
        public string? NameAr { get; set; }

        [Required]
        [Display(Name = "Emirate")]
        public string Emirate { get; set; } = default!;

        [Display(Name = "Emirate (Arabic)")]
        public string? EmirateAr { get; set; }

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
