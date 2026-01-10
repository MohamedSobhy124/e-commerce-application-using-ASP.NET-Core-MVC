using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required]
        public string UserId { get; set; }
        
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters")]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = false; // Admin moderation

        public bool IsVerifiedPurchase { get; set; } = false; // Bought the product

        // Helpful votes (optional for future)
        public int HelpfulCount { get; set; } = 0;
    }
}

