using IdealWeightNutrition.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml.Linq;

namespace IdealWeightNutrition.Controllers
{
    public class SitemapController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SitemapController> _logger;

        public SitemapController(IUnitOfWork unitOfWork, IConfiguration configuration, ILogger<SitemapController> logger)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Route("/sitemap.xml")]
        public IActionResult Sitemap()
        {
            try
            {
                var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
                // Ensure baseUrl uses HTTPS for better SEO
                baseUrl = baseUrl.Replace("http://", "https://");
                
                var urlElements = new List<XElement>
                {
                    // Home Page
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home", IdealWeightNutrition.Utility.DateTimeHelper.Now, "daily", 1.0, baseUrl),
                    
                    // Static Pages
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/AboutUs", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Privacy", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/PrivacyPolicy", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Terms", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Shipping", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Returns", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/HelpCenter", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8, baseUrl),
                    
                    // Services
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/ServiceSubscription", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-1), "weekly", 0.9, baseUrl),
                    
                    // Flash Sales
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/FlashSale", IdealWeightNutrition.Utility.DateTimeHelper.Now, "daily", 0.9, baseUrl),
                    
                    // Combo Offers
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/ComboOffer", IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-1), "weekly", 0.9, baseUrl),
                    
                    // Blog
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Blog", IdealWeightNutrition.Utility.DateTimeHelper.Now, "daily", 0.9, baseUrl)
                };

                // Try to add dynamic content, but don't fail if database is unavailable
                try
                {
                    // Products
                    urlElements.AddRange(GetProductUrls(baseUrl));
                    
                    // Service Details
                    urlElements.AddRange(GetServiceUrls(baseUrl));
                    
                    // Flash Sale Details
                    urlElements.AddRange(GetFlashSaleUrls(baseUrl));
                    
                    // Combo Offer Details
                    urlElements.AddRange(GetComboOfferUrls(baseUrl));
                    
                    // Blog Posts
                    urlElements.AddRange(GetBlogUrls(baseUrl));
                    
                    // Categories
                    urlElements.AddRange(GetCategoryUrls(baseUrl));
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Failed to load dynamic content for sitemap, using static content only");
                }
                
                var sitemap = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement("urlset",
                        new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                        new XAttribute("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                        new XAttribute("xmlns:xhtml", "http://www.w3.org/1999/xhtml"),
                        new XAttribute("xsi:schemaLocation", "http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd"),
                        urlElements.ToArray()
                    )
                );

                return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap");
                
                // Return basic sitemap on error
                var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
                // Ensure baseUrl uses HTTPS
                baseUrl = baseUrl.Replace("http://", "https://");
                var basicSitemap = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement("urlset",
                        new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                        CreateUrlElement(baseUrl + "/Customer/Home", IdealWeightNutrition.Utility.DateTimeHelper.Now, "daily", 1.0)
                    )
                );
                return Content(basicSitemap.ToString(), "application/xml", Encoding.UTF8);
            }
        }

        private XElement CreateUrlElement(string url, DateTime lastModified, string changeFrequency, double priority)
        {
            return new XElement("url",
                new XElement("loc", url),
                new XElement("lastmod", lastModified.ToString("yyyy-MM-dd")),
                new XElement("changefreq", changeFrequency),
                new XElement("priority", priority.ToString("F1"))
            );
        }

        private XElement CreateUrlElementWithHreflang(string url, DateTime lastModified, string changeFrequency, double priority, string baseUrl)
        {
            var urlElement = new XElement("url",
                new XElement("loc", url),
                new XElement("lastmod", lastModified.ToString("yyyy-MM-dd")),
                new XElement("changefreq", changeFrequency),
                new XElement("priority", priority.ToString("F1"))
            );

            // Add hreflang links for multilingual support
            var xhtmlNamespace = XNamespace.Get("http://www.w3.org/1999/xhtml");
            var querySeparator = url.Contains("?") ? "&" : "?";
            urlElement.Add(new XElement(xhtmlNamespace + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "ar"),
                new XAttribute("href", url + querySeparator + "culture=ar")));
            urlElement.Add(new XElement(xhtmlNamespace + "link",
                new XAttribute("rel", "alternate"),
                new XAttribute("hreflang", "en"),
                new XAttribute("href", url + querySeparator + "culture=en")));

            return urlElement;
        }

        private IEnumerable<XElement> GetProductUrls(string baseUrl)
        {
            try
            {
                var products = _unitOfWork.product.GetAll(includeProperties: "categry");
                var urls = new List<XElement>();

                foreach (var product in products)
                {
                    var lastModified = product.ModifiedDate ?? product.CreatedDate ;
                    // Use slug for SEO-friendly URLs
                    var productSlug = !string.IsNullOrEmpty(product.SlugEn) ? product.SlugEn : product.Id.ToString();
                    // Add product URL
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/Home/Details/{productSlug}",
                        lastModified,
                        "weekly",
                        0.8,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }

        private IEnumerable<XElement> GetServiceUrls(string baseUrl)
        {
            try
            {
                var services = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive);
                var urls = new List<XElement>();

                foreach (var service in services)
                {
                    var lastModified = service.UpdatedDate ?? service.CreatedDate;
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/ServiceSubscription/Details/{service.Id}",
                        lastModified,
                        "monthly",
                        0.7,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }

        private IEnumerable<XElement> GetFlashSaleUrls(string baseUrl)
        {
            try
            {
                var flashSales = _unitOfWork.FlashSale.GetAll(f => f.IsActive);
                var urls = new List<XElement>();

                foreach (var flashSale in flashSales)
                {
                    var lastModified = flashSale.ModifiedDate ?? flashSale.StartDate;
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/FlashSale/Details/{flashSale.Id}",
                        lastModified,
                        "daily",
                        0.9,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }

        private IEnumerable<XElement> GetCategoryUrls(string baseUrl)
        {
            try
            {
                var categories = _unitOfWork.categry.GetAll();
                var urls = new List<XElement>();

                foreach (var category in categories)
                {
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/Home?categoryId={category.Id}",
                        IdealWeightNutrition.Utility.DateTimeHelper.Now.AddDays(-7),
                        "weekly",
                        0.7,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }

        private IEnumerable<XElement> GetComboOfferUrls(string baseUrl)
        {
            try
            {
                var comboOffers = _unitOfWork.ComboOffer.GetAll(c => c.IsActive && !c.IsDeleted);
                var urls = new List<XElement>();

                foreach (var comboOffer in comboOffers)
                {
                    var lastModified = comboOffer.ModifiedDate ?? comboOffer.CreatedDate;
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/ComboOffer/Details/{comboOffer.Id}",
                        lastModified,
                        "weekly",
                        0.8,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }

        private IEnumerable<XElement> GetBlogUrls(string baseUrl)
        {
            try
            {
                var blogPosts = _unitOfWork.BlogPost.GetAll()
                    .OrderByDescending(b => b.PublishedDate)
                    .ToList();

                return blogPosts.Select(post => CreateUrlElementWithHreflang(
                    baseUrl + $"/Customer/Blog/Details/{post.Slug}",
                    post.PublishedDate,
                    "monthly",
                    0.7,
                    baseUrl
                ));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load blog posts for sitemap");
                return Enumerable.Empty<XElement>();
            }
        }
    }
}

