using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace IdealWeightNutrition.Models
{
    public class BlogPost : BaseEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Slug is required")]
        [StringLength(200)]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title (English) is required")]
        [StringLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title (Arabic) is required")]
        [StringLength(300)]
        public string TitleAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category (English) is required")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category (Arabic) is required")]
        [StringLength(100)]
        public string CategoryAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author (English) is required")]
        [StringLength(150)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author (Arabic) is required")]
        [StringLength(150)]
        public string AuthorAr { get; set; } = string.Empty;

        public DateTime PublishedDate { get; set; }

        public int ReadTime { get; set; } = 5;

        [ValidateNever]
        public string? ImageUrl { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        [Required(ErrorMessage = "Excerpt (English) is required")]
        public string Excerpt { get; set; } = string.Empty;

        [Required(ErrorMessage = "Excerpt (Arabic) is required")]
        public string ExcerptAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content (English) is required")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content (Arabic) is required")]
        public string ContentAr { get; set; } = string.Empty;

        [StringLength(300)]
        public string? MetaDescription { get; set; }

        [StringLength(300)]
        public string? MetaDescriptionAr { get; set; }

        [StringLength(500)]
        public string? MetaKeywords { get; set; }

        [StringLength(500)]
        public string? MetaKeywordsAr { get; set; }
    }
}
