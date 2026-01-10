using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderHeaderId { get; set; }
        
        [ForeignKey("OrderHeaderId")]
        [ValidateNever]
        public OrderHeader OrderHeader { get; set; } = null!;
        
        public string? ApplicationUserId { get; set; }
        
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }
        
        // For guest orders
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? AdditionalNotes { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Processing, Completed, Cancelled
        
        public DateTime RequestDate { get; set; } = DateTime.Now;
        
        public DateTime? ApprovedDate { get; set; }
        
        public DateTime? RejectedDate { get; set; }
        
        public DateTime? CompletedDate { get; set; }
        
        [StringLength(500)]
        public string? AdminNotes { get; set; }
        
        public string? RejectionReason { get; set; }
        
        // Return shipping information
        [StringLength(100)]
        public string? ReturnTrackingNumber { get; set; }
        
        [StringLength(50)]
        public string? ReturnCarrier { get; set; }
        
        public DateTime? ReturnShippedDate { get; set; }
        
        public DateTime? ReturnReceivedDate { get; set; }
        
        // Refund information
        public decimal? RefundAmount { get; set; }
        
        public string? RefundStatus { get; set; } // Pending, Processed, Failed
        
        public DateTime? RefundProcessedDate { get; set; }
        
        [StringLength(200)]
        public string? RefundTransactionId { get; set; }
        
        // Navigation property for return items
        [ValidateNever]
        public ICollection<ReturnRequestItem> ReturnRequestItems { get; set; } = new List<ReturnRequestItem>();
    }
}

