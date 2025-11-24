using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class ServiceSubscription
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string Title { get; set; }
        
        [MaxLength(500)]
        public string? TitleAr { get; set; }
        
        public string? Description { get; set; }
        
        public string? DescriptionAr { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Required]
        public ServiceType ServiceType { get; set; } // 1 = Online, 2 = Offline
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? OfflinePaymentPercent { get; set; } // For offline services
        
        [MaxLength(1000)]
        public string? ImageUrl { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? UpdatedDate { get; set; }
        
        [MaxLength(450)]
        public string? CreatedBy { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
        
        // Navigation properties
        public virtual ICollection<ServiceOffer>? ServiceOffers { get; set; }
        public virtual ICollection<ServicePurchase>? ServicePurchases { get; set; }
        public virtual ICollection<ServiceImage>? ServiceImages { get; set; }
    }
    
    public enum ServiceType
    {
        Online = 1,
        Offline = 2
    }
}

