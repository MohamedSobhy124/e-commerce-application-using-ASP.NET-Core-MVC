using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class OrderAuditLog : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderHeaderId { get; set; }

        [ForeignKey("OrderHeaderId")]
        public OrderHeader? OrderHeader { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ActionDetails { get; set; }

        [MaxLength(100)]
        public string? PerformedByUserId { get; set; }

        [MaxLength(256)]
        public string? PerformedByUserEmail { get; set; }

        [MaxLength(50)]
        public string? OldOrderStatus { get; set; }

        [MaxLength(50)]
        public string? NewOrderStatus { get; set; }

        [MaxLength(50)]
        public string? OldPaymentStatus { get; set; }

        [MaxLength(50)]
        public string? NewPaymentStatus { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.Now;

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }
    }
}

