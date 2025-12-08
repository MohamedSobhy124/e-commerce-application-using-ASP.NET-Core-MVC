using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyBook.Models
{
    public class Brand : BaseEntity
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Arabic name is required")]
        public string NameAr { get; set; } = string.Empty;
        
        public string? Description { get; set; } 
        
        public string? DescriptionAr { get; set; } 
        
        [ValidateNever]
        public string? ImageUrl { get; set; }
        
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
