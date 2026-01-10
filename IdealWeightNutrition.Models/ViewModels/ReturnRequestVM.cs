using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdealWeightNutrition.Models.ViewModels
{
    public class ReturnRequestVM
    {
        public int OrderHeaderId { get; set; }
        public OrderHeader? OrderHeader { get; set; }
        
        // For guest orders - email verification
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? Email { get; set; }
        
        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string Reason { get; set; } = string.Empty;
        
        [StringLength(1000, ErrorMessage = "Additional notes cannot exceed 1000 characters")]
        public string? AdditionalNotes { get; set; }
        
        public List<ReturnRequestItemVM> Items { get; set; } = new List<ReturnRequestItemVM>();
        
        // Flag to indicate if this is a guest order
        public bool IsGuestOrder { get; set; }
    }
    
    public class ReturnRequestItemVM
    {
        public int OrderDetailId { get; set; }
        public OrderDetail? OrderDetail { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        [StringLength(500)]
        public string? ItemReason { get; set; }
        
        [StringLength(50)]
        public string? ItemCondition { get; set; }
    }
}

