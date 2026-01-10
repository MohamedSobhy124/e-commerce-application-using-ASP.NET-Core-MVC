using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdealWeightNutrition.Models
{
    public class NewsletterSubscription
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }
        
        public DateTime SubscribedDate { get; set; } = DateTime.Now;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? UnsubscribedDate { get; set; }
        
        [StringLength(50)]
        public string? Source { get; set; } // e.g., "HomePage", "Footer", etc.
    }
}

