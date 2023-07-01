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
    public class Product
    {
        public int Id { get; set; }
        [Required]
        public   String Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]

        public   string Author { get; set; }
        [Required]
        public   string ISBN { get; set; }
        [Range(0, 1000)]
        [Display(Name = "List Price")]
        [Required]
        public   double ListPrice { get; set; }
        [Range(0, 1000)]
        [Display(Name = "Price For 1-50")]
        [Required]
        public   double Price { get; set; }
        [Range(0,100)]
        [Display(Name = "Price For 50+")]
        public   double Price50 { get; set; }
        [Range(0, 1000)]
        [Display(Name = "Price For 100+")]
        [Required]
        public   double Price100 { get; set; }
        [Required]
        public int CategryId { get; set; }
        [ForeignKey("CategryId")]
        [ValidateNever]
        public Categry categry { get; set; }
        [ValidateNever]
        public string ImageUrl { get; set; }
    }
}
