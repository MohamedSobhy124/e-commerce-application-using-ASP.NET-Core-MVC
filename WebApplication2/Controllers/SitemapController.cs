using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml.Linq;

namespace BulkyBook.Controllers
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
                
                var urlElements = new List<XElement>
                {
                    // Home Page
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home", BulkyBook.Utility.DateTimeHelper.Now, "daily", 1.0, baseUrl),
                    
                    // Static Pages
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/AboutUs", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Privacy", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/PrivacyPolicy", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Terms", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Shipping", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/Returns", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7, baseUrl),
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Home/HelpCenter", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8, baseUrl),
                    
                    // Services
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/ServiceSubscription", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-1), "weekly", 0.9, baseUrl),
                    
                    // Flash Sales
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/FlashSale", BulkyBook.Utility.DateTimeHelper.Now, "daily", 0.9, baseUrl),
                    
                    // Combo Offers
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/ComboOffer", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-1), "weekly", 0.9, baseUrl),
                    
                    // Blog
                    CreateUrlElementWithHreflang(baseUrl + "/Customer/Blog", BulkyBook.Utility.DateTimeHelper.Now, "daily", 0.9, baseUrl)
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
                var basicSitemap = new XDocument(
                    new XDeclaration("1.0", "UTF-8", "yes"),
                    new XElement("urlset",
                        new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                        CreateUrlElement(baseUrl + "/Customer/Home", BulkyBook.Utility.DateTimeHelper.Now, "daily", 1.0)
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
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/Home/Details?productId={product.Id}",
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
                        BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7),
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
                var urls = new List<XElement>();
                
                // Blog post slugs (matching BlogController)
                var blogPosts = new[]
                {
                    new { Slug = "10-essential-weight-management-tips", DaysAgo = 5 },
                    new { Slug = "ultimate-guide-healthy-meal-planning", DaysAgo = 8 },
                    new { Slug = "understanding-body-nutritional-needs", DaysAgo = 12 },
                    new { Slug = "best-supplements-health-wellness", DaysAgo = 15 },
                    new { Slug = "build-sustainable-exercise-routine", DaysAgo = 18 },
                    new { Slug = "science-intermittent-fasting", DaysAgo = 20 },
                    new { Slug = "hydration-key-optimal-health", DaysAgo = 22 },
                    new { Slug = "sleep-impact-weight-management", DaysAgo = 25 },
                    new { Slug = "reading-nutrition-labels-guide", DaysAgo = 28 },
                    new { Slug = "stress-management-techniques", DaysAgo = 30 },
                    new { Slug = "probiotics-gut-health-guide", DaysAgo = 32 },
                    new { Slug = "realistic-health-goals", DaysAgo = 35 },
                    new { Slug = "strength-training-benefits-women", DaysAgo = 38 },
                    new { Slug = "healthy-snacking-weight-control", DaysAgo = 40 },
                    new { Slug = "shop-healthy-products-guide", DaysAgo = 42 },
                    new { Slug = "building-healthy-habits-psychology", DaysAgo = 45 }
                };
                
                foreach (var post in blogPosts)
                {
                    urls.Add(CreateUrlElementWithHreflang(
                        baseUrl + $"/Customer/Blog/Details/{post.Slug}",
                        BulkyBook.Utility.DateTimeHelper.Now.AddDays(-post.DaysAgo),
                        "monthly",
                        0.7,
                        baseUrl
                    ));
                }

                return urls;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load blog posts for sitemap");
                return Enumerable.Empty<XElement>();
            }
        }
    }
}

