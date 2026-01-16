using IdealWeightNutrition.Models;
using IdealWeightNutrition.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IdealWeightNutrition.Utility
{
    public static class SEOHelper
    {
        /// <summary>
        /// Gets SEO data for Home Page - Brand + Commercial keywords
        /// </summary>
        public static SEOViewModel GetHomePageSEO(string baseUrl, string culture = "en")
        {
            var title = culture == "ar"
                ? "ايديال ويت للتغذية - مكملات غذائية أصلية في الإمارات | Ideal Weight Nutrition UAE"
                : "Ideal Weight Nutrition UAE - Premium Supplements Store Dubai | Buy Online";

            var description = culture == "ar"
                ? "ايديال ويت للتغذية - متجر المكملات الغذائية الرائد في الإمارات. شراء مكملات غذائية أصلية 100%، بروتين، فيتامينات، مكملات تخسيس أونلاين. توصيل مجاني في دبي وأبوظبي. Ideal Weight Nutrition - أفضل متجر مكملات غذائية في الإمارات."
                : "Ideal Weight Nutrition UAE - Leading supplements store in Dubai & UAE. Buy 100% authentic supplements online - protein powder, vitamins, weight loss supplements. Free delivery in Dubai & Abu Dhabi. Best nutrition supplements store UAE.";

            var keywords = culture == "ar"
                ? "Ideal Weight Nutrition, Ideal Weight Nutrition UAE, Ideal Weight Nutrition Dubai, Ideal Weight Nutrition LLC, Ideal Weight Nutrition supplements, Ideal Weight Nutrition store, Ideal Weight Nutrition website, Ideal Weight Nutrition online, Ideal Weight Nutrition products, شراء مكملات غذائية أونلاين الإمارات, متجر مكملات غذائية الإمارات, مكملات غذائية الإمارات, مكملات صحية الإمارات, مكملات غذائية دبي, أفضل مكملات غذائية الإمارات, بروتين الإمارات, شراء بروتين أونلاين الإمارات, فيتامينات الإمارات, مكملات تخسيس الإمارات, متجر تغذية دبي, مكملات صحية دبي"
                : "Ideal Weight Nutrition, Ideal Weight Nutrition UAE, Ideal Weight Nutrition Dubai, Ideal Weight Nutrition LLC, Ideal Weight Nutrition supplements, Ideal Weight Nutrition store, Ideal Weight Nutrition website, Ideal Weight Nutrition online, Ideal Weight Nutrition products, buy supplements online UAE, supplements store UAE, nutrition supplements UAE, health supplements UAE, dietary supplements UAE, sports supplements UAE, protein powder UAE, buy protein powder UAE, whey protein UAE, best protein powder UAE, vitamins UAE, weight loss supplements UAE, nutrition store Dubai, supplement store Dubai, protein shop Dubai";

            return new SEOViewModel
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                ImageUrl = $"{baseUrl}/Images/Products/logo_white_bg.png",
                CanonicalUrl = $"{baseUrl}/Customer/Home",
                PageType = "website"
            };
        }

        /// <summary>
        /// Gets SEO data for Category Page - Product type keywords
        /// </summary>
        public static SEOViewModel GetCategoryPageSEO(string categoryName, string baseUrl, string culture = "en")
        {
            // Determine category-specific keywords
            var categoryKeywords = GetCategorySpecificKeywords(categoryName, culture);

            var title = culture == "ar"
                ? $"{categoryName} - ايديال ويت للتغذية | Ideal Weight Nutrition"
                : $"{categoryName} - Ideal Weight Nutrition | Buy Online";

            var description = culture == "ar"
                ? $"شراء {categoryName} أصلية 100% من ايديال ويت للتغذية في الإمارات. توصيل مجاني في دبي وأبوظبي. أفضل {categoryName} أونلاين."
                : $"Buy authentic 100% {categoryName} from Ideal Weight Nutrition UAE. Free delivery in Dubai & Abu Dhabi. Best {categoryName} online.";

            var keywords = culture == "ar"
                ? $"{categoryName}, {categoryName} الإمارات, {categoryName} دبي, شراء {categoryName} أونلاين الإمارات, أفضل {categoryName} الإمارات, {categoryName} أصلية, Ideal Weight Nutrition, {string.Join(", ", categoryKeywords)}"
                : $"{categoryName}, {categoryName} UAE, {categoryName} Dubai, buy {categoryName} online UAE, best {categoryName} UAE, {categoryName} supplements, Ideal Weight Nutrition, {string.Join(", ", categoryKeywords)}";

            return new SEOViewModel
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                ImageUrl = $"{baseUrl}/Images/Products/logo_white_bg.png",
                CanonicalUrl = $"{baseUrl}/Customer/Home",
                PageType = "website",
                Category = categoryName
            };
        }

        /// <summary>
        /// Gets category-specific keywords based on category name
        /// </summary>
        private static List<string> GetCategorySpecificKeywords(string categoryName, string culture)
        {
            var categoryLower = categoryName.ToLower();
            var keywords = new List<string>();

            if (culture == "ar")
            {
                if (categoryLower.Contains("protein") || categoryLower.Contains("بروتين"))
                {
                    keywords.AddRange(new[] { "بروتين مسحوق الإمارات", "شراء بروتين مسحوق الإمارات", "بروتين مصل اللبن الإمارات", "بروتين مصل اللبن دبي", "أفضل بروتين مسحوق الإمارات", "مكملات بروتين الإمارات" });
                }
                else if (categoryLower.Contains("vitamin") || categoryLower.Contains("فيتامين"))
                {
                    keywords.AddRange(new[] { "فيتامينات الإمارات", "مكملات فيتامين الإمارات", "مكملات فيتامين دبي", "معادن مكملات الإمارات" });
                }
                else if (categoryLower.Contains("weight") || categoryLower.Contains("وزن") || categoryLower.Contains("تخسيس"))
                {
                    keywords.AddRange(new[] { "مكملات تخسيس الإمارات", "حرق دهون الإمارات", "مكملات غذائية الإمارات", "أفضل مكملات تخسيس الإمارات" });
                }
                else
                {
                    keywords.AddRange(new[] { "مكملات غذائية الإمارات", "مكملات صحية الإمارات", "مكملات غذائية دبي" });
                }
            }
            else
            {
                if (categoryLower.Contains("protein"))
                {
                    keywords.AddRange(new[] { "protein powder UAE", "buy protein powder UAE", "whey protein UAE", "whey protein Dubai", "best protein powder UAE", "protein supplements UAE" });
                }
                else if (categoryLower.Contains("vitamin"))
                {
                    keywords.AddRange(new[] { "vitamins UAE", "multivitamins UAE", "vitamin supplements Dubai", "minerals supplements UAE" });
                }
                else if (categoryLower.Contains("weight"))
                {
                    keywords.AddRange(new[] { "weight loss supplements UAE", "fat burner UAE", "diet supplements UAE", "slimming supplements UAE", "best weight loss supplements UAE" });
                }
                else
                {
                    keywords.AddRange(new[] { "supplements UAE", "nutrition supplements UAE", "health supplements UAE", "supplements Dubai" });
                }
            }

            return keywords;
        }

        /// <summary>
        /// Gets SEO data for Product Page - Product-specific keywords
        /// Format: [product name] UAE, buy [product name] online UAE, [brand name] supplements UAE, [product name] price UAE
        /// </summary>
        public static SEOViewModel GetProductSEO(Product product, string baseUrl, string culture = "en")
        {
            var description = product.Description?.Length > 160 
                ? product.Description.Substring(0, 157) + "..." 
                : product.Description ?? "";

            // Product-specific keywords - format: [product name] UAE, buy [product name] online UAE, etc.
            var productName = culture == "ar" && !string.IsNullOrEmpty(product.TitleAr) ? product.TitleAr : product.Title;
            var categoryName = product.categry?.Name ?? "";
            
            List<string> keywords;
            
            if (culture == "ar")
            {
                keywords = new List<string>
                {
                    productName,
                    $"{productName} الإمارات",
                    $"شراء {productName} أونلاين الإمارات",
                    categoryName
                };

                // Add brand-specific if product has brand
                if (product.Brand != null)
                {
                    keywords.Add($"{product.Brand.Name} مكملات الإمارات");
                }

                // Add price-related keyword
                keywords.Add($"{productName} سعر الإمارات");
                
                // Add enhanced long-tail keywords
                var enhancedKeywords = GetEnhancedProductKeywords(product, culture);
                keywords.AddRange(enhancedKeywords);
                
                // Add supporting keywords
                keywords.AddRange(new[]
                {
                    "مكملات غذائية الإمارات",
                    "شراء مكملات غذائية أونلاين الإمارات",
                    "مكملات صحية دبي",
                    "توصيل مجاني الإمارات",
                    "Ideal Weight Nutrition"
                });
            }
            else
            {
                keywords = new List<string>
                {
                    productName,
                    $"{productName} UAE",
                    $"buy {productName} online UAE",
                    categoryName
                };

                // Add brand-specific if product has brand
                if (product.Brand != null)
                {
                    keywords.Add($"{product.Brand.Name} supplements UAE");
                }

                // Add price-related keyword
                keywords.Add($"{productName} price UAE");
                
                // Add enhanced long-tail keywords
                var enhancedKeywords = GetEnhancedProductKeywords(product, culture);
                keywords.AddRange(enhancedKeywords);
                
                // Add supporting keywords
                keywords.AddRange(new[]
                {
                    "supplements UAE",
                    "buy supplements online UAE",
                    "nutrition supplements Dubai",
                    "free delivery UAE",
                    "Ideal Weight Nutrition"
                });
            }

            var imageUrl = product.ProductImages?.FirstOrDefault()?.ImageUrl 
                ?? product.ImageUrl 
                ?? $"{baseUrl}/images/no-image.png";

            return new SEOViewModel
            {
                Title = productName,
                Description = description,
                Keywords = string.Join(", ", keywords.Where(k => !string.IsNullOrEmpty(k))),
                ImageUrl = imageUrl.StartsWith("http") ? imageUrl : $"{baseUrl}{imageUrl}",
                CanonicalUrl = $"{baseUrl}/Customer/Home/Details/{product.GetSlug()}",
                PageType = "product",
                Price = (decimal?)product.Price,
                Currency = "AED",
                InStock = product.StockQuantity > 0,
                Brand = "Ideal Weight Nutrition",
                Category = categoryName,
                Rating = 0, // Will be updated from reviews
                ReviewCount = 0 // Will be updated from reviews
            };
        }

        /// <summary>
        /// Gets SEO data for Blog Page - Educational/Informational keywords
        /// </summary>
        public static SEOViewModel GetBlogPageSEO(string blogTitle, string blogDescription, string baseUrl, string culture = "en")
        {
            var title = culture == "ar"
                ? $"{blogTitle} - ايديال ويت للتغذية"
                : $"{blogTitle} - Ideal Weight Nutrition UAE";

            var description = blogDescription?.Length > 160 
                ? blogDescription.Substring(0, 157) + "..." 
                : blogDescription ?? "";

            var keywords = culture == "ar"
                ? "كيفية إنقاص الوزن بشكل طبيعي, أفضل مكملات للتخسيس, بروتين مقابل بروتين مصل اللبن, كم بروتين في اليوم, فيتامينات ضرورية للتخسيس, نصائح التغذية للوزن الصحي, خطة غذائية لإدارة الوزن, Ideal Weight Nutrition"
                : "how to lose weight naturally, best supplements for weight loss, protein vs whey protein, how much protein per day, vitamins needed for weight loss, nutrition tips for healthy weight, diet plan for weight management, Ideal Weight Nutrition";

            return new SEOViewModel
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                ImageUrl = $"{baseUrl}/images/brand/logo-google.png",
                CanonicalUrl = $"{baseUrl}/Customer/Blog",
                PageType = "article"
            };
        }

        /// <summary>
        /// Gets SEO data for Contact Page - Local keywords
        /// </summary>
        public static SEOViewModel GetContactPageSEO(string baseUrl, string culture = "en")
        {
            var title = culture == "ar"
                ? "اتصل بنا - ايديال ويت للتغذية | Ideal Weight Nutrition UAE"
                : "Contact Us - Ideal Weight Nutrition UAE | Dubai, Abu Dhabi";

            var description = culture == "ar"
                ? "اتصل بنا في ايديال ويت للتغذية - متجر المكملات الغذائية الرائد في الإمارات. نحن في دبي - توصيل في جميع أنحاء الإمارات. رقم الهاتف: +971-50-770-0559"
                : "Contact Ideal Weight Nutrition - Leading supplements store in UAE. Located in Dubai - delivery across UAE. Phone: +971-50-770-0559 | Email: info@idealweightnutrition.ae";

            var keywords = culture == "ar"
                ? "متجر تغذية دبي, متجر مكملات دبي, متجر بروتين دبي, متجر فيتامينات دبي, مكملات صحية دبي, متجر تغذية الإمارات, مكملات قريبة مني, متجر بروتين قريب مني, Ideal Weight Nutrition, اتصل بنا"
                : "nutrition store Dubai, supplement store Dubai, protein shop Dubai, vitamin shop Dubai, health supplements Dubai, nutrition shop UAE, supplements near me, protein store near me, Ideal Weight Nutrition, contact us";

            return new SEOViewModel
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                ImageUrl = $"{baseUrl}/images/brand/logo-google.png",
                CanonicalUrl = $"{baseUrl}/Customer/Home/Contact",
                PageType = "website"
            };
        }

        /// <summary>
        /// Gets SEO data for About Page - Brand trust keywords
        /// </summary>
        public static SEOViewModel GetAboutPageSEO(string baseUrl, string culture = "en")
        {
            var title = culture == "ar"
                ? "من نحن - ايديال ويت للتغذية | Ideal Weight Nutrition UAE"
                : "About Us - Ideal Weight Nutrition UAE | Trusted Supplements Store";

            var description = culture == "ar"
                ? "ايديال ويت للتغذية - متجر المكملات الغذائية الموثوق في الإمارات منذ 2020. مكملات أصلية 100%، توصيل مجاني، خدمة عملاء متميزة. Ideal Weight Nutrition LLC - ثقتكم هي أولويتنا."
                : "Ideal Weight Nutrition - Trusted supplements store in UAE since 2020. 100% authentic supplements, free delivery, excellent customer service. Ideal Weight Nutrition LLC - Your trust is our priority.";

            var keywords = culture == "ar"
                ? "Ideal Weight Nutrition, Ideal Weight Nutrition LLC, Ideal Weight Nutrition الإمارات, Ideal Weight Nutrition دبي, Ideal Weight Nutrition store, Ideal Weight Nutrition website, Ideal Weight Nutrition online, Ideal Weight Nutrition products, ايديال ويت للتغذية, من نحن"
                : "Ideal Weight Nutrition, Ideal Weight Nutrition LLC, Ideal Weight Nutrition UAE, Ideal Weight Nutrition Dubai, Ideal Weight Nutrition store, Ideal Weight Nutrition website, Ideal Weight Nutrition online, Ideal Weight Nutrition products, about us, trusted supplements store";

            return new SEOViewModel
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                ImageUrl = $"{baseUrl}/images/brand/logo-google.png",
                CanonicalUrl = $"{baseUrl}/Customer/Home/AboutUs",
                PageType = "website"
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
            // Enhanced description with key search terms
            var enhancedDescription = "Ideal Weight Nutrition is UAE's premier destination for ideal weight solutions and nutrition supplements. " +
                "We specialize in ideal weight management, nutrition advice, and premium nutrition products. " +
                "Expert nutrition guidance for achieving your ideal weight through proven nutrition programs.";
            
            // Ensure baseUrl uses HTTPS
            var secureBaseUrl = baseUrl.Replace("http://", "https://");
            var logoUrl = $"{secureBaseUrl}/images/brand/logo-google.png";
            var currentYear = DateTime.Now.Year;
            
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Organization"",
    ""@id"": ""{secureBaseUrl}#organization"",
    ""name"": ""Ideal Weight Nutrition"",
    ""alternateName"": [""وزن مثالي للتغذية"", ""Ideal Weight"", ""IWN"", ""ايديال ويت للتغذية""],
    ""legalName"": ""Ideal Weight Nutrition LLC"",
    ""url"": ""{secureBaseUrl}"",
    ""logo"": ""{logoUrl}"",

    ""image"": {{
        ""@type"": ""ImageObject"",
        ""url"": ""{logoUrl}"",
        ""width"": 1200,
        ""height"": 1200,
        ""caption"": ""Ideal Weight Nutrition"",
        ""encodingFormat"": ""image/png""
    }},
    ""description"": ""{EscapeJson(enhancedDescription)}"",
    ""slogan"": ""Your Trusted Source for Ideal Weight and Nutrition Solutions"",
    ""keywords"": ""ideal weight nutrition, ideal weight, nutrition, ideal weight solutions, ideal weight management, nutrition supplements, nutrition advice, ideal weight program, nutrition products, weight management, diet nutrition"",
    ""address"": {{
        ""@type"": ""PostalAddress"",
        ""streetAddress"": ""UAE"",
        ""addressLocality"": ""Dubai"",
        ""addressRegion"": ""Dubai"",
        ""postalCode"": """",
        ""addressCountry"": ""AE"",
        ""addressCountryName"": ""United Arab Emirates""
    }},
    ""contactPoint"": [
        {{
            ""@type"": ""ContactPoint"",
            ""telephone"": ""+971-50-770-0559"",
            ""contactType"": ""Customer Service"",
            ""email"": ""info@idealweightnutrition.ae"",
            ""availableLanguage"": [""Arabic"", ""English""],
            ""areaServed"": ""AE"",
            ""areaServedName"": ""United Arab Emirates""
        }},
        {{
            ""@type"": ""ContactPoint"",
            ""telephone"": ""+971-50-770-0559"",
            ""contactType"": ""Sales"",
            ""email"": ""info@idealweightnutrition.ae"",
            ""availableLanguage"": [""Arabic"", ""English""]
        }}
    ],
    ""areaServed"": {{
        ""@type"": ""Country"",
        ""name"": ""United Arab Emirates""
    }},
    ""founder"": {{
        ""@type"": ""Person"",
        ""name"": ""Ideal Weight Nutrition Team""
    }},
    ""foundingDate"": ""2020"",
    ""sameAs"": [
        ""https://www.facebook.com/share/1FBf7DDTc7/"",
        ""https://www.instagram.com/idealweightnutrition"",
        ""https://www.snapchat.com/add/ideal_weight""
    ],
    ""potentialAction"": {{
        ""@type"": ""SearchAction"",
        ""target"": {{
            ""@type"": ""EntryPoint"",
            ""urlTemplate"": ""{secureBaseUrl}/Customer/Home?searchTerm={{search_term_string}}""
        }},
        ""query-input"": ""required name=search_term_string""
    }}
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
            
            // Ensure baseUrl uses HTTPS
            var secureBaseUrl = baseUrl.Replace("http://", "https://");
            var logoUrl = $"{secureBaseUrl}/images/brand/logo-google.png";
            var currentYear = DateTime.Now.Year;
            
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
    ""@id"": ""{secureBaseUrl}#localbusiness"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""alternateName"": ""وزن مثالي للتغذية"",
    ""url"": ""{secureBaseUrl}"",
    ""logo"": {{
        ""@type"": ""ImageObject"",
        ""@id"": ""{secureBaseUrl}#business-logo"",
        ""url"": ""{logoUrl}"",
        ""contentUrl"": ""{logoUrl}"",
        ""width"": 1200,
        ""height"": 1200,
        ""caption"": ""Ideal Weight Nutrition Logo"",
        ""encodingFormat"": ""image/png"",
        ""name"": ""Ideal Weight Nutrition Logo"",
        ""description"": ""Official logo of Ideal Weight Nutrition - UAE's premier nutrition supplements store"",
        ""creditText"": ""Ideal Weight Nutrition"",
        ""copyrightNotice"": ""© {currentYear} Ideal Weight Nutrition. All rights reserved."",
        ""acquireLicensePage"": ""{secureBaseUrl}/Customer/Home/Terms""
    }},
    ""image"": {{
        ""@type"": ""ImageObject"",
        ""url"": ""{logoUrl}"",
        ""width"": 1200,
        ""height"": 1200,
        ""caption"": ""Ideal Weight Nutrition"",
        ""encodingFormat"": ""image/png""
    }},
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
        ""https://www.facebook.com/share/1FBf7DDTc7/"",
        ""https://www.instagram.com/idealweightnutrition"",
        ""https://www.snapchat.com/add/ideal_weight""
    ]
   
}}";
        }

        public static string GenerateWebsiteStructuredData(string baseUrl, string siteName)
        {
            // Ensure baseUrl uses HTTPS
            var secureBaseUrl = baseUrl.Replace("http://", "https://");
            var logoUrl = $"{secureBaseUrl}/images/brand/logo-google.png";
            var currentYear = DateTime.Now.Year;
            
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""WebSite"",
    ""@id"": ""{secureBaseUrl}#website"",
    ""name"": ""{EscapeJson(siteName)}"",
    ""alternateName"": ""وزن مثالي للتغذية"",
    ""url"": ""{secureBaseUrl}"",
    ""logo"": {{
        ""@type"": ""ImageObject"",
        ""@id"": ""{secureBaseUrl}#website-logo"",
        ""url"": ""{logoUrl}"",
        ""contentUrl"": ""{logoUrl}"",
        ""width"": 1200,
        ""height"": 1200,
        ""caption"": ""Ideal Weight Nutrition Logo"",
        ""encodingFormat"": ""image/png"",
        ""name"": ""Ideal Weight Nutrition Logo"",
        ""description"": ""Official logo of Ideal Weight Nutrition - UAE's premier nutrition supplements store"",
        ""creditText"": ""Ideal Weight Nutrition"",
        ""copyrightNotice"": ""© {currentYear} Ideal Weight Nutrition. All rights reserved."",
        ""acquireLicensePage"": ""{secureBaseUrl}/Customer/Home/Terms""
    }},
    ""description"": ""Ideal Weight Nutrition - Your trusted source for ideal weight solutions and premium nutrition supplements in UAE. Expert nutrition advice, ideal weight programs, and quality nutrition products."",
    ""keywords"": ""ideal weight nutrition, ideal weight, nutrition, ideal weight solutions, nutrition supplements, UAE"",
    ""inLanguage"": [""en-AE"", ""ar-AE""],
    ""publisher"": {{
        ""@type"": ""Organization"",
        ""name"": ""Ideal Weight Nutrition"",
        ""logo"": {{
            ""@type"": ""ImageObject"",
            ""url"": ""{logoUrl}""
        }}
    }},
    ""potentialAction"": {{
        ""@type"": ""SearchAction"",
        ""target"": {{
            ""@type"": ""EntryPoint"",
            ""urlTemplate"": ""{secureBaseUrl}/Customer/Home?searchTerm={{search_term_string}}""
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

        public static string GenerateServiceStructuredData(IdealWeightNutrition.Models.ServiceSubscription service, string baseUrl, string culture = "en")
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
            ""url"": ""{baseUrl}/images/brand/logo-google.png""
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
            ""url"": ""{baseUrl}/images/brand/logo-google.png""
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

        /// <summary>
        /// Gets enhanced long-tail keywords for product pages
        /// </summary>
        private static List<string> GetEnhancedProductKeywords(Product product, string culture)
        {
            var keywords = new List<string>();
            var productName = product.Title;
            var brandName = product.Brand?.Name ?? "";
            var categoryName = product.categry?.Name ?? "";
            
            if (culture == "en")
            {
                // Intent-based keywords
                keywords.AddRange(new[] {
                    $"buy {productName} UAE",
                    $"{productName} price Dubai",
                    $"{productName} delivery Abu Dhabi",
                    $"authentic {productName} UAE",
                    $"{productName} near me Dubai",
                    $"original {productName} online UAE",
                    $"best price {productName} UAE",
                    $"{productName} same day delivery Dubai",
                    $"{productName} cash on delivery UAE"
                });

                // Brand + product combinations
                if (!string.IsNullOrEmpty(brandName))
                {
                    keywords.AddRange(new[] {
                        $"{brandName} {productName} UAE",
                        $"{brandName} authorized dealer UAE"
                    });
                }

                // Use case keywords
                keywords.AddRange(new[] {
                    $"{productName} for weight loss",
                    $"{productName} for muscle gain",
                    $"{productName} for gym",
                    $"{productName} reviews UAE"
                });

                // Comparison keywords
                if (!string.IsNullOrEmpty(categoryName))
                {
                    keywords.Add($"{productName} vs alternatives");
                    keywords.Add($"best {categoryName} UAE");
                }

                // Location-specific
                keywords.AddRange(new[] {
                    $"{productName} Dubai Marina",
                    $"{productName} Jumeirah",
                    $"{productName} Business Bay"
                });
            }
            else // Arabic
            {
                keywords.AddRange(new[] {
                    $"شراء {productName} الإمارات",
                    $"{productName} سعر دبي",
                    $"{productName} توصيل أبوظبي",
                    $"{productName} أصلية الإمارات",
                    $"{productName} قريب مني دبي",
                    $"{productName} أونلاين الإمارات",
                    $"أفضل سعر {productName} الإمارات",
                    $"{productName} توصيل نفس اليوم دبي",
                    $"{productName} الدفع عند الاستلام الإمارات"
                });

                if (!string.IsNullOrEmpty(brandName))
                {
                    keywords.AddRange(new[] {
                        $"{brandName} {productName} الإمارات",
                        $"{brandName} موزع معتمد الإمارات"
                    });
                }
            }
            
            return keywords;
        }

        /// <summary>
        /// Gets question-based keywords for content
        /// </summary>
        public static List<string> GetQuestionBasedKeywords(string category, string culture)
        {
            var keywords = new List<string>();
            
            if (culture == "en")
            {
                keywords.AddRange(new[] {
                    // What questions
                    $"what is the best {category} in UAE",
                    $"what {category} should I buy",
                    $"what is {category} used for",
                    
                    // How questions
                    $"how to use {category}",
                    $"how much {category} per day",
                    $"how to choose {category}",
                    
                    // Where questions
                    $"where to buy {category} in Dubai",
                    $"where to get authentic {category} UAE",
                    
                    // When questions
                    $"when to take {category}",
                    $"when is best time for {category}",
                    
                    // Why questions
                    $"why buy {category} from Ideal Weight",
                    $"why {category} is important",
                    
                    // Which questions
                    $"which {category} is best for beginners",
                    $"which brand of {category} is good"
                });
            }
            else // Arabic
            {
                keywords.AddRange(new[] {
                    $"ما هو أفضل {category} في الإمارات",
                    $"ما {category} يجب أن أشتري",
                    $"كيفية استخدام {category}",
                    $"كم {category} في اليوم",
                    $"أين لشراء {category} في دبي",
                    $"متى أخذ {category}",
                    $"لماذا شراء {category} من Ideal Weight",
                    $"أي {category} أفضل للمبتدئين"
                });
            }
            
            return keywords;
        }

        /// <summary>
        /// Gets intent-based keywords organized by intent type
        /// </summary>
        public static Dictionary<string, List<string>> GetIntentBasedKeywords(string category = "", string culture = "en")
        {
            var keywords = new Dictionary<string, List<string>>();
            
            if (culture == "en")
            {
                keywords["Informational"] = new List<string>
                {
                    "what is whey protein",
                    "how protein powder works",
                    "benefits of BCAA supplements",
                    "difference between whey isolate and concentrate",
                    "protein supplements guide",
                    "nutrition supplements explained"
                };
                
                keywords["Commercial"] = new List<string>
                {
                    "best protein powder UAE",
                    "top rated supplements Dubai",
                    "authentic supplements review",
                    "Ideal Weight Nutrition vs competitors",
                    "best supplements store UAE"
                };
                
                keywords["Transactional"] = new List<string>
                {
                    "buy protein powder UAE",
                    "order supplements online Dubai",
                    "protein powder delivery Abu Dhabi",
                    "cheap supplements UAE with free delivery",
                    "supplements online store UAE"
                };
                
                keywords["Local"] = new List<string>
                {
                    "supplement store near me",
                    "nutrition shop Dubai Marina",
                    "protein powder delivery Business Bay",
                    "supplements store open now Dubai",
                    "vitamin shop near me"
                };
            }
            else // Arabic
            {
                keywords["Informational"] = new List<string>
                {
                    "ما هو بروتين مصل اللبن",
                    "كيف يعمل مسحوق البروتين",
                    "فوائد مكملات BCAA",
                    "الفرق بين بروتين مصل اللبن المعزول والمركز"
                };
                
                keywords["Commercial"] = new List<string>
                {
                    "أفضل مسحوق بروتين الإمارات",
                    "أفضل المكملات الغذائية دبي",
                    "مراجعة مكملات أصلية"
                };
                
                keywords["Transactional"] = new List<string>
                {
                    "شراء مسحوق بروتين الإمارات",
                    "طلب مكملات أونلاين دبي",
                    "توصيل بروتين أبوظبي"
                };
                
                keywords["Local"] = new List<string>
                {
                    "متجر مكملات قريب مني",
                    "متجر تغذية دبي مارينا",
                    "توصيل بروتين بيزنس باي"
                };
            }
            
            // Add category-specific keywords if provided
            if (!string.IsNullOrEmpty(category))
            {
                var categoryKeywords = GetQuestionBasedKeywords(category, culture);
                keywords["Informational"].AddRange(categoryKeywords);
            }
            
            return keywords;
        }

        /// <summary>
        /// Gets voice search optimized keywords
        /// </summary>
        private static List<string> GetVoiceSearchKeywords(string productName, string culture)
        {
            var keywords = new List<string>();
            
            if (culture == "en")
            {
                keywords.AddRange(new[] {
                    $"Hey Google where can I buy {productName} in Dubai",
                    $"Alexa find {productName} near me",
                    $"what's the best place to buy {productName} in UAE",
                    $"I need {productName} delivered today in Dubai",
                    $"show me authentic {productName} stores in Abu Dhabi",
                    $"where to buy {productName} online UAE",
                    $"best {productName} price in Dubai"
                });
            }
            else // Arabic
            {
                keywords.AddRange(new[] {
                    $"أين يمكنني شراء {productName} في دبي",
                    $"أبحث عن {productName} قريب مني",
                    $"أفضل مكان لشراء {productName} في الإمارات",
                    $"أحتاج {productName} توصيل اليوم في دبي"
                });
            }
            
            return keywords;
        }

        /// <summary>
        /// Generates optimized image alt text for SEO
        /// </summary>
        public static string GenerateImageAltText(Product product)
        {
            var brandName = product.Brand?.Name ?? "";
            var categoryName = product.categry?.Name ?? "";
            var altText = $"{product.Title}";
            
            if (!string.IsNullOrEmpty(brandName))
                altText += $" - {brandName}";
            
            altText += " - Buy Online UAE - Ideal Weight Nutrition";
            
            if (!string.IsNullOrEmpty(categoryName))
                altText += $" - {categoryName}";
            
            altText += " - Free Delivery Dubai";
            
            return altText;
        }

        /// <summary>
        /// Generates optimized image title for SEO
        /// </summary>
        public static string GenerateImageTitle(Product product)
        {
            var brandName = product.Brand?.Name ?? "";
            var title = $"{product.Title}";
            
            if (!string.IsNullOrEmpty(brandName))
                title += $" {brandName}";
            
            title += $" - Price: {product.Price} AED - Shop Now";
            
            return title;
        }

        /// <summary>
        /// Generates optimized URL slug for products
        /// </summary>
        public static string GenerateOptimizedSlug(Product product)
        {
            var parts = new List<string>();
            
            // Include brand if available
            if (product.Brand != null && !string.IsNullOrEmpty(product.Brand.Name))
            {
                parts.Add(product.Brand.Name.ToLower());
            }
            
            // Add product title
            parts.Add(product.Title.ToLower());
            
            // Add category if available
            if (product.categry != null && !string.IsNullOrEmpty(product.categry.Name))
            {
                parts.Add(product.categry.Name.ToLower());
            }
            
            // Join parts with dashes
            var slug = string.Join("-", parts.Where(p => !string.IsNullOrEmpty(p)));
            
            // Replace spaces and special characters
            slug = slug.Replace(" ", "-")
                      .Replace("--", "-")
                      .Replace("---", "-");
            
            // Remove special characters, keep only alphanumeric and dashes
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "", RegexOptions.IgnoreCase);
            
            // Remove leading/trailing dashes
            slug = slug.Trim('-');
            
            // Limit length (max 100 chars for SEO)
            if (slug.Length > 100)
            {
                slug = slug.Substring(0, 100).TrimEnd('-');
            }
            
            return slug;
        }

        /// <summary>
        /// Generates Video structured data for product demonstrations
        /// </summary>
        public static string GenerateVideoStructuredData(string videoUrl, string title, 
            string description, string thumbnailUrl, DateTime uploadDate, int duration)
        {
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""VideoObject"",
    ""name"": ""{EscapeJson(title)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""thumbnailUrl"": ""{thumbnailUrl}"",
    ""uploadDate"": ""{uploadDate:yyyy-MM-ddTHH:mm:ssZ}"",
    ""duration"": ""PT{duration}S"",
    ""contentUrl"": ""{videoUrl}"",
    ""embedUrl"": ""{videoUrl}"",
    ""publisher"": {{
        ""@type"": ""Organization"",
        ""name"": ""Ideal Weight Nutrition"",
        ""logo"": {{
            ""@type"": ""ImageObject"",
            ""url"": ""{thumbnailUrl}""
        }}
    }}
}}";
        }

        /// <summary>
        /// Generates HowTo structured data for supplement usage guides
        /// </summary>
        public static string GenerateHowToStructuredData(string name, string description, 
            List<HowToStep> steps, string imageUrl, string baseUrl)
        {
            if (steps == null || !steps.Any()) return "";
            
            var stepsJson = new StringBuilder();
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                stepsJson.Append($@"{{
            ""@type"": ""HowToStep"",
            ""position"": {i + 1},
            ""name"": ""{EscapeJson(step.Name)}"",
            ""text"": ""{EscapeJson(step.Text)}""");
                
                if (!string.IsNullOrEmpty(step.ImageUrl))
                {
                    stepsJson.Append($@",
            ""image"": ""{step.ImageUrl}""");
                }
                else if (!string.IsNullOrEmpty(imageUrl))
                {
                    stepsJson.Append($@",
            ""image"": ""{imageUrl}""");
                }
                
                stepsJson.Append("}");
                if (i < steps.Count - 1) stepsJson.Append(",");
            }
            
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""HowTo"",
    ""name"": ""{EscapeJson(name)}"",
    ""description"": ""{EscapeJson(description)}"",
    ""image"": ""{imageUrl}"",
    ""step"": [{stepsJson}],
    ""totalTime"": ""PT{steps.Count * 2}M""
}}";
        }

        /// <summary>
        /// Generates Offer structured data for promotions
        /// </summary>
        public static string GenerateOfferStructuredData(string productName, decimal price, 
            decimal? oldPrice, DateTime validUntil, string productUrl, string baseUrl)
        {
            var priceValidUntil = validUntil.ToString("yyyy-MM-dd");
            var availability = "https://schema.org/InStock";
            
            var oldPriceJson = oldPrice.HasValue 
                ? $@",
        ""priceSpecification"": {{
            ""@type"": ""UnitPriceSpecification"",
            ""price"": ""{oldPrice.Value:F2}"",
            ""priceCurrency"": ""AED"",
            ""valueAddedTaxIncluded"": true
        }}"
                : "";
            
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""Offer"",
    ""itemOffered"": {{
        ""@type"": ""Product"",
        ""name"": ""{EscapeJson(productName)}""
    }},
    ""price"": ""{price:F2}"",
    ""priceCurrency"": ""AED"",
    ""priceValidUntil"": ""{priceValidUntil}"",
    ""availability"": ""{availability}"",
    ""url"": ""{productUrl}"",
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
        }}
    }}{oldPriceJson}
}}";
        }

        /// <summary>
        /// Generates Review snippet structured data
        /// </summary>
        public static string GenerateReviewSnippet(List<ReviewData> reviews, string baseUrl)
        {
            if (reviews == null || !reviews.Any()) return "";
            
            var reviewItems = new StringBuilder();
            var validReviews = reviews.Where(r => r != null && r.Rating > 0).Take(5).ToList();
            
            for (int i = 0; i < validReviews.Count; i++)
            {
                var review = validReviews[i];
                reviewItems.Append($@"{{
            ""@type"": ""Review"",
            ""author"": {{
                ""@type"": ""Person"",
                ""name"": ""{EscapeJson(review.AuthorName ?? "Customer")}""
            }},
            ""datePublished"": ""{review.DatePublished:yyyy-MM-dd}"",
            ""reviewBody"": ""{EscapeJson(review.ReviewBody ?? "")}"",
            ""reviewRating"": {{
                ""@type"": ""Rating"",
                ""ratingValue"": {review.Rating},
                ""bestRating"": 5,
                ""worstRating"": 1
            }}
        }}");
                if (i < validReviews.Count - 1) reviewItems.Append(",");
            }
            
            return $@"{{
    ""@context"": ""https://schema.org"",
    ""@type"": ""AggregateRating"",
    ""ratingValue"": ""{validReviews.Average(r => r.Rating):F1}"",
    ""reviewCount"": {validReviews.Count},
    ""bestRating"": ""5"",
    ""worstRating"": ""1"",
    ""review"": [{reviewItems}]
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

    public class HowToStep
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ReviewData
    {
        public string AuthorName { get; set; }
        public DateTime DatePublished { get; set; }
        public string ReviewBody { get; set; }
        public int Rating { get; set; }
    }
}

