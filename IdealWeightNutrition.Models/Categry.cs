using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class Categry : BaseEntity
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Arabic name is required")]
        public string NameAr { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Arabic description is required")]
        public string DescriptionAr { get; set; } = string.Empty;
        
        [ValidateNever]
        public string? ImageUrl { get; set; }
        
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
