using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace BulkyBook.Models
{
    public class Categry : BaseEntity
    {
        public int Id { get; set; }
        [Required]
        public required String Name { get; set; }
        
        [Required]
        public required string NameAr { get; set; }
        
        [Required]
        public required string Description { get; set; }
        
        [Required]
        public required string DescriptionAr { get; set; }
        
        [Required]
        public required string ImageUrl { get; set; }
        
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
