namespace IdealWeightNutrition.Models.ViewModels
{
    /// <summary>
    /// View model for displaying blog posts on the customer-facing site (culture-specific)
    /// </summary>
    public class BlogPostDisplayViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime PublishedDate { get; set; }
        public int ReadTime { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Excerpt { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MetaDescription { get; set; } = string.Empty;
        public string MetaKeywords { get; set; } = string.Empty;
    }
}
