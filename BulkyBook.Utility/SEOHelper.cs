using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BulkyBook.Utility
{
    public static class SEOHelper
    {
        public static SEOViewModel GetProductSEO(Product product, string baseUrl, string culture = "en")
        {
            var description = product.Description?.Length > 160 
                ? product.Description.Substring(0, 157) + "..." 
                : product.Description ?? "";

            var keywords = new List<string>
            {
                product.Title,
                product.categry?.Name ?? "",
                "nutrition supplements UAE",
                "health products Dubai",
                "fitness supplements Abu Dhabi",
                "diet supplements UAE",
                "weight loss products Dubai",
                "protein supplements UAE",
                "vitamins UAE",
                "health supplements online UAE",
                "buy supplements Dubai",
                "nutrition store UAE",
                "ideal weight",
                "UAE delivery",
                "free delivery UAE"
            };

            if (culture == "ar")
            {
                keywords = new List<string>
                {
                    product.Title,
                    product.categry?.Name ?? "",
                    "مكملات غذائية الإمارات",
                    "منتجات صحية دبي",
                    "مكملات لياقة أبوظبي",
                    "مكملات غذائية دبي",
                    "منتجات تخسيس الإمارات",
                    "بروتين الإمارات",
                    "فيتامينات الإمارات",
                    "مكملات صحية أونلاين الإمارات",
                    "شراء مكملات دبي",
                    "متجر تغذية الإمارات",
                    "وزن مثالي",
                    "توصيل الإمارات",
                    "توصيل مجاني الإمارات"
                };
            }

            var imageUrl = product.ProductImages?.FirstOrDefault()?.ImageUrl 
                ?? product.ImageUrl 
                ?? $"{baseUrl}/images/no-image.png";

            return new SEOViewModel
            {
                Title = product.Title,
                Description = description,
                Keywords = string.Join(", ", keywords.Where(k => !string.IsNullOrEmpty(k))),
                ImageUrl = imageUrl.StartsWith("http") ? imageUrl : $"{baseUrl}{imageUrl}",
                CanonicalUrl = $"{baseUrl}/Customer/Home/Details?productId={product.Id}",
                PageType = "product",
                Price = (decimal?)product.Price,
                Currency = "AED",
                InStock = product.StockQuantity > 0,
                Brand = "Ideal Weight Nutrition",
                Category = product.categry?.Name ?? "",
                Rating = 0, // Will be updated from reviews
                ReviewCount = 0 // Will be updated from reviews
            };
        }

        public static string GenerateProductStructuredData(Product product, SEOViewModel seo, string baseUrl, double? rating = null, int? reviewCount = null)
        {
            var imageUrl = seo.ImageUrl;
            var availability = seo.InStock ? "https://schema.org/InStock" : "https://schema.org/OutOfStock";
            var aggregateRating = rating.HasValue && reviewCount.HasValue && reviewCount > 0
                ? $@",
        ""aggregateRating"": {{
            ""@type"": ""AggregateRating"",
            ""ratingValue"": ""{rating.Value:F1}"",
            ""reviewCount"": {reviewCount.Value},
            ""bestRating"": ""5"",
            ""worstRating"": ""1""
        }}"
                : "";

            // Add UAE-specific keywords
            var keywords = new List<string>
            {
                "nutrition supplements UAE",
                "health products Dubai",
                "fitness supplements UAE",
                seo.Category ?? ""
            };
            var keywordStr = string.Join(", ", keywords.Where(k => !string.IsNullOrEmpty(k)));

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Product"",
    ""name"": ""{EscapeJson(product.Title)}"",
    ""description"": ""{EscapeJson(seo.Description)}"",
    ""image"": ""{imageUrl}"",
    ""keywords"": ""{EscapeJson(keywordStr)}"",
    ""brand"": {{
        ""@type"": ""Brand"",
        ""name"": ""Ideal Weight Nutrition""
    }},
    ""category"": ""{EscapeJson(seo.Category)}"",
    ""offers"": {{
        ""@type"": ""Offer"",
        ""url"": ""{seo.CanonicalUrl}"",
        ""priceCurrency"": ""AED"",
        ""price"": ""{product.Price:F2}"",
        ""priceValidUntil"": ""{DateTime.Now.AddYears(1):yyyy-MM-dd}"",
        ""itemCondition"": ""https://schema.org/NewCondition"",
        ""availability"": ""{availability}"",
        ""seller"": {{
            ""@type"": ""Organization"",
            ""name"": ""Ideal Weight Nutrition"",
            ""url"": ""{baseUrl}""
        }},
        ""shippingDetails"": {{
            ""@type"": ""OfferShippingDetails"",
            ""shippingRate"": {{
                ""@type"": ""MonetaryAmount"",
                ""value"": ""0"",
                ""currency"": ""AED""
            }},
            ""shippingDestination"": {{
                ""@type"": ""DefinedRegion"",
                ""addressCountry"": ""AE""
            }},
            ""deliveryTime"": {{
                ""@type"": ""ShippingDeliveryTime"",
                ""businessDays"": {{
                    ""@type"": ""OpeningHoursSpecification"",
                    ""dayOfWeek"": [""Monday"", ""Tuesday"", ""Wednesday"", ""Thursday"", ""Friday"", ""Saturday"", ""Sunday""]
                }},
                ""cutoffTime"": ""14:00"",
                ""handlingTime"": {{
                    ""@type"": ""QuantitativeValue"",
                    ""minValue"": 1,
                    ""maxValue"": 2,
                    ""unitCode"": ""DAY""
                }},
                ""transitTime"": {{
                    ""@type"": ""QuantitativeValue"",
                    ""minValue"": 1,
                    ""maxValue"": 3,
                    ""unitCode"": ""DAY""
                }}
            }}
        }}
    }}{aggregateRating}
}}";
        }

        public static string GenerateBreadcrumbStructuredData(List<BreadcrumbItem> items, string baseUrl)
        {
            var itemList = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                itemList.Append($@"{{
            ""@type"": ""ListItem"",
            ""position"": {i + 1},
            ""name"": ""{EscapeJson(item.Name)}"",
            ""item"": ""{item.Url}""
        }}");
                if (i < items.Count - 1) itemList.Append(",");
            }

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""BreadcrumbList"",
    ""itemListElement"": [
        {itemList}
    ]
}}";
        }

        public static string GenerateOrganizationStructuredData(string baseUrl, string siteName, string description)
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Organization"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""alternateName"": ""وزن مثالي للتغذية"",
    ""url"": ""{baseUrl}"",
    ""logo"": ""{baseUrl}/Images/Products/logo_white_bg.png"",
    ""description"": ""{EscapeJson(description)}"",
    ""keywords"": ""ideal weight nutrition, ideal weight, nutrition, ideal weight solutions, nutrition supplements"",
    ""address"": {{
        ""@type"": ""PostalAddress"",
        ""streetAddress"": ""UAE"",
        ""addressLocality"": ""Dubai"",
        ""addressRegion"": ""Dubai"",
        ""addressCountry"": ""AE"",
        ""addressCountryName"": ""United Arab Emirates""
    }},
    ""contactPoint"": [
        {{
            ""@type"": ""ContactPoint"",
            ""telephone"": ""+971-50-770-0559"",
            ""contactType"": ""Customer Service"",
            ""availableLanguage"": [""Arabic"", ""English""],
            ""areaServed"": ""AE"",
            ""areaServedName"": ""United Arab Emirates""
        }},
        {{
            ""@type"": ""ContactPoint"",
            ""telephone"": ""+971-50-770-0559"",
            ""contactType"": ""Sales"",
            ""availableLanguage"": [""Arabic"", ""English""]
        }}
    ],
    ""areaServed"": {{
        ""@type"": ""Country"",
        ""name"": ""United Arab Emirates""
    }},
    ""sameAs"": [
        ""https://www.facebook.com/idealweightnutrition"",
        ""https://www.instagram.com/idealweightnutrition""
    ]
}}";
        }

        public static string GenerateLocalBusinessStructuredData(string baseUrl, string siteName, string description, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var business = configuration.GetSection("SiteSettings:Business");
            var phone = business["Phone"] ?? "+971-50-770-0559";
            var email = business["Email"] ?? "info@idealweightnutrition.ae";
            var streetAddress = business["Address:StreetAddress"] ?? "UAE";
            var city = business["Address:City"] ?? "Dubai";
            var state = business["Address:State"] ?? "Dubai";
            var country = business["Address:Country"] ?? "AE";
            var priceRange = business["PriceRange"] ?? "$$";
            var paymentAccepted = business["PaymentAccepted"] ?? "Cash, Credit Card, Debit Card, Online Payment";
            var currenciesAccepted = business["CurrenciesAccepted"] ?? "AED";
            
            // Enhanced description with target keywords
            var enhancedDescription = description + " Ideal Weight Nutrition specializes in ideal weight solutions and comprehensive nutrition programs. Expert nutrition advice for achieving your ideal weight.";
            
            // Build opening hours
            var openingHours = new List<string>();
            var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            foreach (var day in days)
            {
                var hours = business[$"OpeningHours:{day}"];
                if (!string.IsNullOrEmpty(hours))
                {
                    openingHours.Add($"{day.Substring(0, 2)} {hours}");
                }
            }
            var openingHoursStr = string.Join(", ", openingHours);

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""LocalBusiness"",
    ""@id"": ""{baseUrl}#organization"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""alternateName"": ""وزن مثالي للتغذية"",
    ""url"": ""{baseUrl}"",
    ""logo"": ""{baseUrl}/Images/Products/logo_white_bg.png"",
    ""image"": ""{baseUrl}/Images/Products/logo_white_bg.png"",
    ""description"": ""{EscapeJson(enhancedDescription)}"",
    ""keywords"": ""ideal weight nutrition, ideal weight, nutrition, ideal weight solutions, nutrition supplements, UAE"",
    ""address"": {{
        ""@type"": ""PostalAddress"",
        ""streetAddress"": ""{EscapeJson(streetAddress)}"",
        ""addressLocality"": ""{EscapeJson(city)}"",
        ""addressRegion"": ""{EscapeJson(state)}"",
        ""postalCode"": """",
        ""addressCountry"": ""{country}"",
        ""addressCountryName"": ""United Arab Emirates""
    }},
    ""geo"": {{
        ""@type"": ""GeoCoordinates"",
        ""latitude"": ""25.2048"",
        ""longitude"": ""55.2708""
    }},
    ""telephone"": ""{phone}"",
    ""email"": ""{email}"",
    ""priceRange"": ""{priceRange}"",
    ""paymentAccepted"": ""{EscapeJson(paymentAccepted)}"",
    ""currenciesAccepted"": ""{currenciesAccepted}"",
    ""openingHoursSpecification"": [
        {{
            ""@type"": ""OpeningHoursSpecification"",
            ""dayOfWeek"": [
                ""Monday"",
                ""Tuesday"",
                ""Wednesday"",
                ""Thursday"",
                ""Friday"",
                ""Saturday"",
                ""Sunday""
            ],
            ""opens"": ""09:00"",
            ""closes"": ""22:00""
        }}
    ],
    ""areaServed"": [
        {{
            ""@type"": ""Country"",
            ""name"": ""United Arab Emirates"",
            ""alternateName"": ""UAE""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Dubai""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Abu Dhabi""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Sharjah""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Ajman""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Ras Al Khaimah""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Fujairah""
        }},
        {{
            ""@type"": ""City"",
            ""name"": ""Umm Al Quwain""
        }}
    ],
    ""hasOfferCatalog"": {{
        ""@type"": ""OfferCatalog"",
        ""name"": ""Nutrition Supplements"",
        ""itemListElement"": []
    }},
    ""sameAs"": [
        ""https://www.facebook.com/idealweightnutrition"",
        ""https://www.instagram.com/idealweightnutrition""
    ]
}}";
        }

        public static string GenerateWebsiteStructuredData(string baseUrl, string siteName)
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""WebSite"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""alternateName"": ""وزن مثالي للتغذية"",
    ""url"": ""{baseUrl}"",
    ""logo"": ""{baseUrl}/Images/Products/logo_white_bg.png"",
    ""description"": ""Ideal Weight Nutrition - Your trusted source for ideal weight solutions and premium nutrition supplements in UAE. Expert nutrition advice, ideal weight programs, and quality nutrition products."",
    ""keywords"": ""ideal weight nutrition, ideal weight, nutrition, ideal weight solutions, nutrition supplements, UAE"",
    ""potentialAction"": {{
        ""@type"": ""SearchAction"",
        ""target"": {{
            ""@type"": ""EntryPoint"",
            ""urlTemplate"": ""{baseUrl}/Customer/Home?searchTerm={{search_term_string}}""
        }},
        ""query-input"": ""required name=search_term_string""
    }}
}}";
        }

        public static string GenerateHomePageStructuredData(string baseUrl, string siteName, string description, string culture = "en")
        {
            var keywords = culture == "ar"
                ? "وزن مثالي للتغذية, وزن مثالي, التغذية, مكملات غذائية, بروتين, فيتامينات, مكملات رياضية, منتجات تخسيس, حمية غذائية, مكملات صحية, تغذية, مكملات الإمارات, برامج الوزن المثالي, حلول التغذية"
                : "ideal weight nutrition, ideal weight, nutrition, ideal weight nutrition UAE, ideal weight solutions, nutrition supplements, protein supplements, vitamins UAE, diet supplements, weight loss products, fitness nutrition, health supplements, ideal weight, nutrition store UAE, ideal weight program, nutrition advice";

            var aboutDescription = culture == "ar"
                ? "وزن مثالي للتغذية - حلول الوزن المثالي والتغذية المتميزة في الإمارات. مكملات غذائية عالية الجودة، بروتين، فيتامينات، منتجات تخسيس، ولياقة بدنية في الإمارات"
                : "Ideal Weight Nutrition - Ideal weight solutions and premium nutrition in UAE. High-quality nutrition supplements, protein, vitamins, diet products, and fitness nutrition in UAE";

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""WebPage"",
    ""@id"": ""{baseUrl}#webpage"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""url"": ""{baseUrl}"",
    ""inLanguage"": ""{culture}"",
    ""isPartOf"": {{
        ""@type"": ""WebSite"",
        ""name"": ""{EscapeJson(siteName)}"",
        ""url"": ""{baseUrl}""
    }},
    ""about"": {{
        ""@type"": ""Thing"",
        ""name"": ""{EscapeJson(culture == "ar" ? "وزن مثالي للتغذية" : "Ideal Weight Nutrition")}"",
        ""description"": ""{EscapeJson(aboutDescription)}"",
        ""alternateName"": ""{EscapeJson(culture == "ar" ? "التغذية والوزن المثالي" : "Ideal Weight and Nutrition")}""
    }},
    ""keywords"": ""{EscapeJson(keywords)}"",
    ""breadcrumb"": {{
        ""@type"": ""BreadcrumbList"",
        ""itemListElement"": [
            {{
                ""@type"": ""ListItem"",
                ""position"": 1,
                ""name"": ""{EscapeJson(culture == "ar" ? "الرئيسية" : "Home")}"",
                ""item"": ""{baseUrl}""
            }}
        ]
    }}
}}";
        }

        public static string GenerateServiceStructuredData(BulkyBook.Models.ServiceSubscription service, string baseUrl, string culture = "en")
        {
            var description = service.Description?.Length > 160 
                ? service.Description.Substring(0, 157) + "..." 
                : service.Description ?? "";

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Service"",
    ""name"": ""{EscapeJson(service.Title)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""provider"": {{
        ""@type"": ""Organization"",
        ""name"": ""Ideal Weight Nutrition"",
        ""url"": ""{baseUrl}""
    }},
    ""areaServed"": {{
        ""@type"": ""Country"",
        ""name"": ""United Arab Emirates""
    }},
    ""offers"": {{
        ""@type"": ""Offer"",
        ""price"": ""{service.Price:F2}"",
        ""priceCurrency"": ""AED"",
        ""url"": ""{baseUrl}/Customer/ServiceSubscription/Details/{service.Id}""
    }}
}}";
        }

        public static string GenerateFAQStructuredData(List<FAQItem> faqs, string baseUrl)
        {
            if (faqs == null || !faqs.Any()) return "";

            var faqItems = new StringBuilder();
            for (int i = 0; i < faqs.Count; i++)
            {
                var faq = faqs[i];
                faqItems.Append($@"{{
            ""@type"": ""Question"",
            ""name"": ""{EscapeJson(faq.Question)}"",
            ""acceptedAnswer"": {{
                ""@type"": ""Answer"",
                ""text"": ""{EscapeJson(faq.Answer)}""
            }}
        }}");
                if (i < faqs.Count - 1) faqItems.Append(",");
            }

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""FAQPage"",
    ""mainEntity"": [
        {faqItems}
    ]
}}";
        }

        public static string GenerateReviewStructuredData(string productName, double rating, int reviewCount, string baseUrl, string productUrl)
        {
            if (reviewCount == 0) return "";

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Product"",
    ""name"": ""{EscapeJson(productName)}"",
    ""aggregateRating"": {{
        ""@type"": ""AggregateRating"",
        ""ratingValue"": ""{rating:F1}"",
        ""reviewCount"": {reviewCount},
        ""bestRating"": ""5"",
        ""worstRating"": ""1""
    }},
    ""url"": ""{productUrl}""
}}";
        }

        public static string GenerateItemListStructuredData(List<ItemListItem> items, string baseUrl, string listName)
        {
            if (items == null || !items.Any()) return "";

            var itemList = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                itemList.Append($@"{{
            ""@type"": ""ListItem"",
            ""position"": {i + 1},
            ""name"": ""{EscapeJson(item.Name)}"",
            ""url"": ""{item.Url}"",
            ""image"": ""{item.ImageUrl ?? ""}"",
            ""description"": ""{EscapeJson(item.Description ?? "")}""
        }}");
                if (i < items.Count - 1) itemList.Append(",");
            }

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""ItemList"",
    ""name"": ""{EscapeJson(listName)}"",
    ""itemListElement"": [
        {itemList}
    ]
}}";
        }

        public static string GenerateWebPageStructuredData(string baseUrl, string pageTitle, string description, string pageUrl, string culture = "en")
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""WebPage"",
    ""@id"": ""{pageUrl}#webpage"",
    ""url"": ""{pageUrl}"",
    ""name"": ""{EscapeJson(pageTitle)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""inLanguage"": ""{culture}"",
    ""isPartOf"": {{
        ""@type"": ""WebSite"",
        ""name"": ""Ideal Weight Nutrition"",
        ""url"": ""{baseUrl}""
    }},
    ""about"": {{
        ""@type"": ""Thing"",
        ""name"": ""Nutrition Supplements""
    }}
}}";
        }

        public static string GenerateArticleStructuredData(ArticleData article, string baseUrl, string culture = "en")
        {
            var authorSchema = $@"{{
        ""@type"": ""Person"",
        ""name"": ""{EscapeJson(article.Author)}""
    }}";

            var publisherSchema = $@"{{
        ""@type"": ""Organization"",
        ""name"": ""Ideal Weight Nutrition"",
        ""logo"": {{
            ""@type"": ""ImageObject"",
            ""url"": ""{baseUrl}/Images/Products/logo_white_bg.png""
        }}
    }}";

            var imageSchema = !string.IsNullOrEmpty(article.ImageUrl)
                ? $@",
    ""image"": {{
        ""@type"": ""ImageObject"",
        ""url"": ""{article.ImageUrl}"",
        ""width"": 1200,
        ""height"": 630
    }}"
                : "";

            var keywordsSchema = !string.IsNullOrEmpty(article.Keywords)
                ? $@",
    ""keywords"": ""{EscapeJson(article.Keywords)}"""
                : "";

            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""BlogPosting"",
    ""@id"": ""{article.Url}#article"",
    ""headline"": ""{EscapeJson(article.Headline)}"",
    ""description"": ""{EscapeJson(article.Description)}"",
    ""url"": ""{article.Url}"",
    ""datePublished"": ""{article.DatePublished:yyyy-MM-ddTHH:mm:ssZ}"",
    ""dateModified"": ""{article.DateModified:yyyy-MM-ddTHH:mm:ssZ}"",
    ""author"": {authorSchema},
    ""publisher"": {publisherSchema},
    ""inLanguage"": ""{culture}"",
    ""articleSection"": ""{EscapeJson(article.Category ?? "Health & Wellness")}"",
    ""wordCount"": {article.WordCount ?? 0}{imageSchema}{keywordsSchema}
}}";
        }

        public static string GenerateBlogStructuredData(string baseUrl, string siteName, string description, string culture = "en")
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Blog"",
    ""name"": ""{EscapeJson(siteName)} Blog"",
    ""description"": ""{EscapeJson(description)}"",
    ""url"": ""{baseUrl}/Customer/Blog"",
    ""inLanguage"": ""{culture}"",
    ""publisher"": {{
        ""@type"": ""Organization"",
        ""name"": ""Ideal Weight Nutrition"",
        ""logo"": {{
            ""@type"": ""ImageObject"",
            ""url"": ""{baseUrl}/Images/Products/logo_white_bg.png""
        }}
    }}
}}";
        }

        public static string GenerateCollectionPageStructuredData(string baseUrl, string pageTitle, string description, string pageUrl, int itemCount, string culture = "en")
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""CollectionPage"",
    ""@id"": ""{pageUrl}#webpage"",
    ""url"": ""{pageUrl}"",
    ""name"": ""{EscapeJson(pageTitle)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""inLanguage"": ""{culture}"",
    ""numberOfItems"": {itemCount},
    ""isPartOf"": {{
        ""@type"": ""WebSite"",
        ""name"": ""Ideal Weight Nutrition"",
        ""url"": ""{baseUrl}""
    }}
}}";
        }

        private static string EscapeJson(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }
    }

    public class FAQItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class ItemListItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
    }

    public class BreadcrumbItem
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class ArticleData
    {
        public string Headline { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public DateTime DatePublished { get; set; }
        public DateTime DateModified { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string Keywords { get; set; }
        public int? WordCount { get; set; }
    }
}

