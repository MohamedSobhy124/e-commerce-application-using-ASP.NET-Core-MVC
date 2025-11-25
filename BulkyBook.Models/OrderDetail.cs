using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        [Required]
        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        [ValidateNever]
        public OrderHeader OrderHeader { get; set; }


        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product Product { get; set; }

        public int Count { get; set; }
        public double Price { get; set; }

        // Flash Sale Support
        public int? FlashSaleItemId { get; set; }
        [ForeignKey(nameof(FlashSaleItemId))]
        [ValidateNever]
        public FlashSaleItem? FlashSaleItem { get; set; }

        [NotMapped]
        public bool IsFromFlashSale => FlashSaleItemId.HasValue;
        
        // Variant Support
        public int? ProductVariantId { get; set; }
        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant? ProductVariant { get; set; }
    }
}
