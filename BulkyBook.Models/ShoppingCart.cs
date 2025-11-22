using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product product { get; set; }
        [Range(1,1000,ErrorMessage ="Please Enter a value between 1 and 1000")]
        public int Count { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser applicationUser { get; set; }
        [NotMapped]
        public double Price { get; set; }

        // Flash Sale Support
        public int? FlashSaleItemId { get; set; }
        [ForeignKey(nameof(FlashSaleItemId))]
        [ValidateNever]
        public FlashSaleItem? FlashSaleItem { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FlashSalePrice { get; set; }

        [NotMapped]
        public bool IsFlashSale => FlashSaleItemId.HasValue; 
        [NotMapped]
        public bool CanReview { get; set; } =false;
    }
}
