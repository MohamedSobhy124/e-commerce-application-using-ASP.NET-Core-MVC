using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class ServicePurchase
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ServiceSubscriptionId { get; set; }
        
        [ForeignKey("ServiceSubscriptionId")]
        public virtual ServiceSubscription? ServiceSubscription { get; set; }
        
        [MaxLength(450)]
        public string? ApplicationUserId { get; set; } // NULL for guest users
        
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser? ApplicationUser { get; set; }
        
        [MaxLength(256)]
        public string? GuestEmail { get; set; }
        
        [MaxLength(256)]
        public string? GuestName { get; set; }
        
        [MaxLength(50)]
        public string? GuestPhone { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";
        
        [MaxLength(500)]
        public string? PaymentIntentId { get; set; } // Stripe payment intent ID
        
        [MaxLength(500)]
        public string? SessionId { get; set; } // Stripe session ID
        
        public int? ServiceOfferId { get; set; }
        
        [ForeignKey("ServiceOfferId")]
        public virtual ServiceOffer? ServiceOffer { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;
        
        public DateTime PurchaseDate { get; set; } = DateTime.Now;
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";
    }
}

