using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class ReturnRequestItem
    {
        public int Id { get; set; }
        
        [Required]
        public int ReturnRequestId { get; set; }
        
        [ForeignKey("ReturnRequestId")]
        [ValidateNever]
        public ReturnRequest ReturnRequest { get; set; } = null!;
        
        [Required]
        public int OrderDetailId { get; set; }
        
        [ForeignKey("OrderDetailId")]
        [ValidateNever]
        public OrderDetail OrderDetail { get; set; } = null!;
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        
        public decimal ReturnPrice { get; set; } // Price at the time of return request
        
        [StringLength(500)]
        public string? ItemReason { get; set; } // Specific reason for returning this item
        
        // Condition of returned item
        [StringLength(50)]
        public string? ItemCondition { get; set; } // New, Used, Damaged, Defective
    }
}

