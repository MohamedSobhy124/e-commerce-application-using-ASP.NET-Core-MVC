using System;

namespace IdealWeightNutrition.Models.ViewModels
{
    public class SEOViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string ImageUrl { get; set; }
        public string CanonicalUrl { get; set; }
        public string PageType { get; set; } // "website", "product", "article", etc.
        public DateTime? PublishedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string Author { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; } = "AED";
        public bool InStock { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public double? Rating { get; set; }
        public int? ReviewCount { get; set; }
    }
}

