using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class ServiceOffer
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ServiceSubscriptionId { get; set; }
        
        [ForeignKey("ServiceSubscriptionId")]
        public virtual ServiceSubscription? ServiceSubscription { get; set; }
        
        public int? PromoCodeId { get; set; } // Link to existing PromoCode
        
        [ForeignKey("PromoCodeId")]
        public virtual PromoCode? PromoCode { get; set; }
        
        [Required]
        public DiscountType DiscountType { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [MaxLength(450)]
        public string? CreatedBy { get; set; }
    }
}

