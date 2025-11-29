using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyBook.Models
{
    public class ComboOfferImage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ComboOfferId { get; set; }
        
        [ForeignKey("ComboOfferId")]
        [ValidateNever]
        public ComboOffer ComboOffer { get; set; }
        
        [Required]
        public string ImageUrl { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
        
        [StringLength(500)]
        public string? ImageInfo { get; set; }
    }
}

