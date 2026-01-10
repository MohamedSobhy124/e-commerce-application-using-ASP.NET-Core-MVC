using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace IdealWeightNutrition.Controllers
{
    public class RobotsController : Controller
    {
        private readonly IConfiguration _configuration;

        public RobotsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/robots.txt")]
        public IActionResult Robots()
        {
            try
            {
                var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
                var sitemapUrl = baseUrl + "/sitemap.xml";

                var robotsContent = new StringBuilder();
                robotsContent.AppendLine("# Crawl-delay for aggressive bots");
                robotsContent.AppendLine("User-agent: AhrefsBot");
                robotsContent.AppendLine("Crawl-delay: 10");
                robotsContent.AppendLine();
                robotsContent.AppendLine("User-agent: SemrushBot");
                robotsContent.AppendLine("Crawl-delay: 10");
                robotsContent.AppendLine();
                robotsContent.AppendLine("# Default rules for all bots");
                robotsContent.AppendLine("User-agent: *");
                robotsContent.AppendLine("Allow: /");
                robotsContent.AppendLine("Disallow: /Admin/");
                robotsContent.AppendLine("Disallow: /Identity/");
                robotsContent.AppendLine("Disallow: /Customer/Cart/");
                robotsContent.AppendLine("Disallow: /Customer/Home/TrackOrder");
                robotsContent.AppendLine();
                robotsContent.AppendLine($"Sitemap: {sitemapUrl}");

                return Content(robotsContent.ToString(), "text/plain", Encoding.UTF8);
            }
            catch
            {
                var robotsContent = new StringBuilder();
                robotsContent.AppendLine("User-agent: *");
                robotsContent.AppendLine("Allow: /");
                robotsContent.AppendLine("Disallow: /Admin/");
                robotsContent.AppendLine("Disallow: /Identity/");
                return Content(robotsContent.ToString(), "text/plain", Encoding.UTF8);
            }
        }
    }
}

