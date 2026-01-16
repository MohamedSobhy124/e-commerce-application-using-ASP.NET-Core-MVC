using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Models
{
    public class OrderHeader
    {

        public int Id { get; set; }
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        // For guest checkout
        public string? Email { get; set; }
        public bool IsGuestOrder { get; set; } = false;

        public DateTime OrderDate { get; set; }
        public DateTime ShippingDate { get; set; }
        public double OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }

        public DateTime PaymentDate { get; set; }
        public DateTime PaymentDueDate { get; set; }

        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? PaymentMethod { get; set; }

        [Required]
        public string PhoneNumber { get; set; } = default!;
        [Required]
        public string StreetAddress { get; set; } = default!;
        [Required]
        public string City { get; set; } = default!;
        public string? Area { get; set; } // For remote areas or custom area entry
        [Required]
        public string State { get; set; } = default!;
        [Required]
        public string PostalCode { get; set; } = default!;
        [Required]
        public string Name { get; set; } = default!;

        // Promo Code fields
        public int? PromoCodeId { get; set; }
        
        [ForeignKey("PromoCodeId")]
        [ValidateNever]
        public PromoCode? PromoCode { get; set; }

        public string? PromoCodeText { get; set; }
        
        public double? DiscountAmount { get; set; }

        public double? OrderSubtotal { get; set; }

    }
}
