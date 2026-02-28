using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using IdealWeightNutrition.Utility;
using Microsoft.Extensions.Configuration;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models.ViewModels;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BlogController : Controller
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public BlogController(IStringLocalizer<SharedResources> localizer, IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _localizer = localizer;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        private string GetCurrentCulture()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            return requestCulture?.RequestCulture.Culture.Name ?? "en";
        }

        private static BlogPostDisplayViewModel MapToDisplay(IdealWeightNutrition.Models.BlogPost blog, bool isArabic)
        {
            return new BlogPostDisplayViewModel
            {
                Id = blog.Id,
                Title = isArabic ? blog.TitleAr : blog.Title,
                Slug = blog.Slug,
                Category = isArabic ? blog.CategoryAr : blog.Category,
                Author = isArabic ? blog.AuthorAr : blog.Author,
                PublishedDate = blog.PublishedDate,
                ReadTime = blog.ReadTime,
                ImageUrl = blog.ImageUrl ?? "",
                Excerpt = isArabic ? blog.ExcerptAr : blog.Excerpt,
                Content = isArabic ? blog.ContentAr : blog.Content,
                MetaDescription = isArabic ? blog.MetaDescriptionAr : blog.MetaDescription ?? "",
                MetaKeywords = isArabic ? blog.MetaKeywordsAr : blog.MetaKeywords ?? ""
            };
        }

        public IActionResult Index()
        {
            var currentCulture = GetCurrentCulture();
            var isArabic = currentCulture == "ar";
            var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;

            var blogTitle = isArabic
                ? "مدونة الصحة والعافية - نصائح وإرشادات الخبراء"
                : "Health & Wellness Blog - Expert Tips & Guides";
            var blogDescription = isArabic
                ? "اكتشف نصائح صحية من الخبراء، ونصائح إدارة الوزن، وأدلة التغذية، ومراجعات المنتجات من مدونة الصحة والعافية لدينا."
                : "Discover expert health tips, weight management advice, nutrition guides, and product reviews from our health and wellness blog.";

            var seo = SEOHelper.GetBlogPageSEO(blogTitle, blogDescription, baseUrl, currentCulture);
            ViewData["SEO"] = seo;

            var dbBlogs = _unitOfWork.BlogPost.GetAll()
                .OrderByDescending(b => b.PublishedDate)
                .ToList();
            var blogs = dbBlogs.Select(b => MapToDisplay(b, isArabic)).ToList();

            ViewData["BlogStructuredData"] = SEOHelper.GenerateBlogStructuredData(
                baseUrl,
                "Ideal Weight Nutrition",
                blogDescription,
                currentCulture
            );

            return View(blogs);
        }

        public IActionResult Details(string slug)
        {
            var currentCulture = GetCurrentCulture();
            var isArabic = currentCulture == "ar";

            var dbBlog = _unitOfWork.BlogPost.Get(b => b.Slug == slug);
            if (dbBlog == null)
                return NotFound();

            var blog = MapToDisplay(dbBlog, isArabic);

            var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
            var blogUrl = Url.Action("Details", "Blog", new { area = "Customer", slug = blog.Slug }, Request.Scheme) ??
                         $"{baseUrl}/Customer/Blog/Details/{blog.Slug}";

            ViewData["Title"] = $"{blog.Title} - {(isArabic ? "مدونة الصحة والعافية" : "Health & Wellness Blog")}";
            ViewData["Description"] = blog.MetaDescription;
            ViewData["Keywords"] = blog.MetaKeywords;
            ViewData["Canonical"] = blogUrl;
            ViewData["Image"] = blog.ImageUrl;

            var imageUrlForStructured = blog.ImageUrl?.StartsWith("http") == true
                ? blog.ImageUrl
                : (baseUrl.TrimEnd('/') + (blog.ImageUrl?.StartsWith("/") == true ? "" : "/") + (blog.ImageUrl ?? ""));

            var articleData = new ArticleData
            {
                Headline = blog.Title,
                Description = blog.MetaDescription,
                Url = blogUrl,
                DatePublished = blog.PublishedDate,
                DateModified = blog.PublishedDate,
                Author = blog.Author,
                Category = blog.Category,
                ImageUrl = imageUrlForStructured,
                Keywords = blog.MetaKeywords,
                WordCount = blog.Content?.Length / 5 ?? 0
            };

            ViewData["ArticleStructuredData"] = SEOHelper.GenerateArticleStructuredData(articleData, baseUrl, currentCulture);

            var relatedDb = _unitOfWork.BlogPost.GetAll()
                .Where(b => b.Id != blog.Id && (isArabic ? b.CategoryAr : b.Category) == blog.Category)
                .Take(3)
                .ToList();
            ViewBag.RelatedBlogs = relatedDb.Select(b => MapToDisplay(b, isArabic)).ToList();

            return View(blog);
        }
    }
}
