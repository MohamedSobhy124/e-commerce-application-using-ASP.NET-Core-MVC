using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
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
                    CreateUrlElement(baseUrl + "/Customer/Home", BulkyBook.Utility.DateTimeHelper.Now, "daily", 1.0),
                    
                    // Static Pages
                    CreateUrlElement(baseUrl + "/Customer/Home/AboutUs", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8),
                    CreateUrlElement(baseUrl + "/Customer/Home/Privacy", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5),
                    CreateUrlElement(baseUrl + "/Customer/Home/PrivacyPolicy", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5),
                    CreateUrlElement(baseUrl + "/Customer/Home/Terms", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.5),
                    CreateUrlElement(baseUrl + "/Customer/Home/Shipping", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7),
                    CreateUrlElement(baseUrl + "/Customer/Home/Returns", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.7),
                    CreateUrlElement(baseUrl + "/Customer/Home/HelpCenter", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7), "monthly", 0.8),
                    
                    // Services
                    CreateUrlElement(baseUrl + "/Customer/ServiceSubscription", BulkyBook.Utility.DateTimeHelper.Now.AddDays(-1), "weekly", 0.9),
                    
                    // Flash Sales
                    CreateUrlElement(baseUrl + "/Customer/FlashSale", BulkyBook.Utility.DateTimeHelper.Now, "daily", 0.9)
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

        private IEnumerable<XElement> GetProductUrls(string baseUrl)
        {
            try
            {
                var products = _unitOfWork.product.GetAll(includeProperties: "categry");
                var urls = new List<XElement>();

                foreach (var product in products)
                {
                    urls.Add(CreateUrlElement(
                        baseUrl + $"/Customer/Home/Details?productId={product.Id}",
                        BulkyBook.Utility.DateTimeHelper.Now.AddDays(-1),
                        "weekly",
                        0.8
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
                    urls.Add(CreateUrlElement(
                        baseUrl + $"/Customer/ServiceSubscription/Details/{service.Id}",
                        service.CreatedDate,
                        "monthly",
                        0.7
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
                    urls.Add(CreateUrlElement(
                        baseUrl + $"/Customer/FlashSale/Details/{flashSale.Id}",
                        flashSale.StartDate,
                        "daily",
                        0.9
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
                    urls.Add(CreateUrlElement(
                        baseUrl + $"/Customer/Home?categoryId={category.Id}",
                        BulkyBook.Utility.DateTimeHelper.Now.AddDays(-7),
                        "weekly",
                        0.7
                    ));
                }

                return urls;
            }
            catch
            {
                return Enumerable.Empty<XElement>();
            }
        }
    }
}

