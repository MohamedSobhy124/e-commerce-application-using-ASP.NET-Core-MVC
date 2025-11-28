using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkyBook.Models
{
    public class StockNotification : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        public int? ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant? ProductVariant { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public string? ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        [Display(Name = "Is Notified")]
        public bool IsNotified { get; set; } = false;

        [Display(Name = "Notified Date")]
        public DateTime? NotifiedDate { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}

