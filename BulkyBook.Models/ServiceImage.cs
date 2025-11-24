using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BulkyBook.Models
{
    public class ServiceImage
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ServiceSubscriptionId { get; set; }
        
        [ForeignKey("ServiceSubscriptionId")]
        [ValidateNever]
        public ServiceSubscription ServiceSubscription { get; set; }
        
        [Required]
        public string ImageUrl { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
    }
}

