using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using BulkyBook.Utility;
using Microsoft.Extensions.Configuration;

namespace BulkyBook.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class BlogController : Controller
    {
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly IConfiguration _configuration;

        public BlogController(IStringLocalizer<SharedResources> localizer, IConfiguration configuration)
        {
            _localizer = localizer;
            _configuration = configuration;
        }

        private string GetCurrentCulture()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            return requestCulture?.RequestCulture.Culture.Name ?? "en";
        }

        public IActionResult Index()
        {
            var currentCulture = GetCurrentCulture();
            var isArabic = currentCulture == "ar";
            var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;

            ViewData["Title"] = isArabic 
                ? "مدونة الصحة والعافية - نصائح وإرشادات الخبراء"
                : "Health & Wellness Blog - Expert Tips & Guides";
            ViewData["Description"] = isArabic
                ? "اكتشف نصائح صحية من الخبراء، ونصائح إدارة الوزن، وأدلة التغذية، ومراجعات المنتجات من مدونة الصحة والعافية لدينا."
                : "Discover expert health tips, weight management advice, nutrition guides, and product reviews from our health and wellness blog.";
            ViewData["Keywords"] = isArabic
                ? "مدونة صحية، نصائح العافية، إدارة الوزن، نصائح التغذية، نمط حياة صحي، نصائح اللياقة"
                : "health blog, wellness tips, weight management, nutrition advice, healthy lifestyle, fitness tips";

            var blogs = GetBlogPosts();
            
            // Add Blog structured data
            var blogDescription = isArabic
                ? "مدونة الصحة والعافية - نصائح وإرشادات الخبراء"
                : "Health & Wellness Blog - Expert Tips & Guides";
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
            var blogs = GetBlogPosts();
            var blog = blogs.FirstOrDefault(b => b.Slug == slug);

            if (blog == null)
            {
                return NotFound();
            }

            var currentCulture = GetCurrentCulture();
            var isArabic = currentCulture == "ar";
            var baseUrl = _configuration["SiteSettings:BaseUrl"] ?? Request.Scheme + "://" + Request.Host;
            var blogUrl = Url.Action("Details", "Blog", new { area = "Customer", slug = blog.Slug }, Request.Scheme) ?? 
                         $"{baseUrl}/Customer/Blog/Details/{blog.Slug}";
            
            ViewData["Title"] = $"{blog.Title} - {(isArabic ? "مدونة الصحة والعافية" : "Health & Wellness Blog")}";
            ViewData["Description"] = blog.MetaDescription;
            ViewData["Keywords"] = blog.MetaKeywords;
            ViewData["Canonical"] = blogUrl;
            ViewData["Image"] = blog.ImageUrl;

            // Add Article structured data for SEO
            var articleData = new ArticleData
            {
                Headline = blog.Title,
                Description = blog.MetaDescription,
                Url = blogUrl,
                DatePublished = blog.PublishedDate,
                DateModified = blog.PublishedDate, // Can be updated when blog is modified
                Author = blog.Author,
                Category = blog.Category,
                ImageUrl = blog.ImageUrl,
                Keywords = blog.MetaKeywords,
                WordCount = blog.Content?.Length / 5 // Rough estimate: 5 chars per word
            };
            
            ViewData["ArticleStructuredData"] = SEOHelper.GenerateArticleStructuredData(articleData, baseUrl, currentCulture);

            // Related blogs
            ViewBag.RelatedBlogs = blogs.Where(b => b.Id != blog.Id && b.Category == blog.Category).Take(3).ToList();

            return View(blog);
        }

        private List<BlogPost> GetBlogPosts()
        {
            var currentCulture = GetCurrentCulture();
            var isArabic = currentCulture == "ar";
            
            if (isArabic)
            {
                return GetBlogPostsArabic();
            }
            else
            {
                return GetBlogPostsEnglish();
            }
        }

        private List<BlogPost> GetBlogPostsEnglish()
        {
            return new List<BlogPost>
            {
                new BlogPost
                {
                    Id = 1,
                    Title = "10 Essential Weight Management Tips for a Healthier You",
                    Slug = "10-essential-weight-management-tips",
                    Category = "Weight Management",
                    Author = "Dr. Sarah Johnson",
                    PublishedDate = DateTime.Now.AddDays(-5),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800",
                    Excerpt = "Discover proven strategies for effective weight management that go beyond dieting. Learn about sustainable lifestyle changes, portion control, and the importance of regular exercise in achieving your health goals.",
                    Content = GetFullContent("weight-management"),
                    MetaDescription = "Learn 10 essential weight management tips from health experts. Discover sustainable strategies for maintaining a healthy weight through proper nutrition, exercise, and lifestyle changes.",
                    MetaKeywords = "weight management, weight loss tips, healthy weight, diet advice, weight control"
                },
                new BlogPost
                {
                    Id = 2,
                    Title = "The Ultimate Guide to Healthy Meal Planning",
                    Slug = "ultimate-guide-healthy-meal-planning",
                    Category = "Nutrition",
                    Author = "Nutrition Expert",
                    PublishedDate = DateTime.Now.AddDays(-8),
                    ReadTime = 12,
                    ImageUrl = "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=800",
                    Excerpt = "Master the art of meal planning with our comprehensive guide. Learn how to create balanced, nutritious meal plans that support your health goals and save time in the kitchen.",
                    Content = GetFullContent("meal-planning"),
                    MetaDescription = "Complete guide to healthy meal planning. Learn how to create nutritious, balanced meal plans that support your wellness goals and simplify your daily routine.",
                    MetaKeywords = "meal planning, healthy meals, nutrition planning, meal prep, healthy diet"
                },
                new BlogPost
                {
                    Id = 3,
                    Title = "Understanding Your Body's Nutritional Needs",
                    Slug = "understanding-body-nutritional-needs",
                    Category = "Nutrition",
                    Author = "Dr. Michael Chen",
                    PublishedDate = DateTime.Now.AddDays(-12),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    Excerpt = "Your body has unique nutritional requirements that change throughout life. This article explains how to understand and meet your individual nutritional needs for optimal health.",
                    Content = GetFullContent("nutritional-needs"),
                    MetaDescription = "Learn about your body's unique nutritional requirements and how to meet them. Essential guide to understanding vitamins, minerals, and macro-nutrients.",
                    MetaKeywords = "nutritional needs, vitamins, minerals, healthy eating, balanced diet, nutrients"
                },
                new BlogPost
                {
                    Id = 4,
                    Title = "Best Supplements for Health and Wellness",
                    Slug = "best-supplements-health-wellness",
                    Category = "Supplements",
                    Author = "Health Advisor",
                    PublishedDate = DateTime.Now.AddDays(-15),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1556910103-1c02745aae4d?w=800",
                    Excerpt = "Explore the world of dietary supplements and discover which ones can truly benefit your health. Learn about quality, dosage, and when supplements are actually necessary.",
                    Content = GetFullContent("supplements"),
                    MetaDescription = "Expert guide to the best health and wellness supplements. Learn which supplements are worth taking and how to choose quality products.",
                    MetaKeywords = "supplements, vitamins, health supplements, dietary supplements, wellness products"
                },
                new BlogPost
                {
                    Id = 5,
                    Title = "How to Build a Sustainable Exercise Routine",
                    Slug = "build-sustainable-exercise-routine",
                    Category = "Fitness",
                    Author = "Fitness Coach",
                    PublishedDate = DateTime.Now.AddDays(-18),
                    ReadTime = 7,
                    ImageUrl = "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800",
                    Excerpt = "Creating an exercise routine that you can stick to long-term is key to maintaining fitness. Discover strategies for building habits that last and keep you motivated.",
                    Content = GetFullContent("exercise-routine"),
                    MetaDescription = "Learn how to build a sustainable exercise routine that fits your lifestyle. Tips for creating lasting fitness habits and staying motivated.",
                    MetaKeywords = "exercise routine, fitness tips, workout plan, exercise habits, fitness motivation"
                },
                new BlogPost
                {
                    Id = 6,
                    Title = "The Science Behind Intermittent Fasting",
                    Slug = "science-intermittent-fasting",
                    Category = "Nutrition",
                    Author = "Dr. Sarah Johnson",
                    PublishedDate = DateTime.Now.AddDays(-20),
                    ReadTime = 11,
                    ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=800",
                    Excerpt = "Intermittent fasting has gained popularity, but what does science say? Learn about the research-backed benefits, methods, and who should try this eating pattern.",
                    Content = GetFullContent("intermittent-fasting"),
                    MetaDescription = "Scientific guide to intermittent fasting. Learn about research-backed benefits, different methods, and whether it's right for you.",
                    MetaKeywords = "intermittent fasting, fasting benefits, weight loss, eating patterns, health benefits"
                },
                new BlogPost
                {
                    Id = 7,
                    Title = "Hydration: The Key to Optimal Health",
                    Slug = "hydration-key-optimal-health",
                    Category = "Wellness",
                    Author = "Health Advisor",
                    PublishedDate = DateTime.Now.AddDays(-22),
                    ReadTime = 6,
                    ImageUrl = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800",
                    Excerpt = "Proper hydration affects every aspect of your health. Discover why water intake matters, how much you really need, and signs that you're not drinking enough.",
                    Content = GetFullContent("hydration"),
                    MetaDescription = "Learn why hydration is crucial for optimal health. Discover how much water you need and the signs of dehydration.",
                    MetaKeywords = "hydration, water intake, health, dehydration, wellness, daily water"
                },
                new BlogPost
                {
                    Id = 8,
                    Title = "Sleep and Its Impact on Weight Management",
                    Slug = "sleep-impact-weight-management",
                    Category = "Weight Management",
                    Author = "Dr. Michael Chen",
                    PublishedDate = DateTime.Now.AddDays(-25),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1541781774459-bb2af2f05b55?w=800",
                    Excerpt = "Did you know that sleep quality directly affects your weight? Learn about the connection between sleep and metabolism, and how to improve your sleep for better health.",
                    Content = GetFullContent("sleep-weight"),
                    MetaDescription = "Discover how sleep affects weight management and metabolism. Learn tips for improving sleep quality for better health outcomes.",
                    MetaKeywords = "sleep and weight, sleep health, metabolism, weight management, healthy sleep"
                },
                new BlogPost
                {
                    Id = 9,
                    Title = "Reading Nutrition Labels: A Complete Guide",
                    Slug = "reading-nutrition-labels-guide",
                    Category = "Nutrition",
                    Author = "Nutrition Expert",
                    PublishedDate = DateTime.Now.AddDays(-28),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1466637574441-749b8f19452f?w=800",
                    Excerpt = "Master the skill of reading nutrition labels to make informed food choices. Learn what to look for, what to avoid, and how to compare products effectively.",
                    Content = GetFullContent("nutrition-labels"),
                    MetaDescription = "Complete guide to reading and understanding nutrition labels. Make informed food choices with expert tips on deciphering product labels.",
                    MetaKeywords = "nutrition labels, food labels, healthy eating, reading labels, food choices"
                },
                new BlogPost
                {
                    Id = 10,
                    Title = "Stress Management Techniques for Better Health",
                    Slug = "stress-management-techniques",
                    Category = "Wellness",
                    Author = "Health Advisor",
                    PublishedDate = DateTime.Now.AddDays(-30),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800",
                    Excerpt = "Chronic stress can negatively impact your health. Discover effective stress management techniques that help you maintain balance and improve your overall well-being.",
                    Content = GetFullContent("stress-management"),
                    MetaDescription = "Learn effective stress management techniques for better health. Discover proven methods to reduce stress and improve your quality of life.",
                    MetaKeywords = "stress management, stress relief, wellness, mental health, relaxation techniques"
                },
                new BlogPost
                {
                    Id = 11,
                    Title = "Probiotics and Gut Health: Everything You Need to Know",
                    Slug = "probiotics-gut-health-guide",
                    Category = "Supplements",
                    Author = "Dr. Sarah Johnson",
                    PublishedDate = DateTime.Now.AddDays(-32),
                    ReadTime = 11,
                    ImageUrl = "https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?w=800",
                    Excerpt = "Your gut health affects your entire body. Learn about probiotics, prebiotics, and how to support your digestive system for optimal wellness.",
                    Content = GetFullContent("probiotics"),
                    MetaDescription = "Complete guide to probiotics and gut health. Learn how probiotics work and how to improve your digestive health naturally.",
                    MetaKeywords = "probiotics, gut health, digestive health, microbiome, healthy gut, supplements"
                },
                new BlogPost
                {
                    Id = 12,
                    Title = "Setting Realistic Health Goals for Long-Term Success",
                    Slug = "realistic-health-goals",
                    Category = "Wellness",
                    Author = "Fitness Coach",
                    PublishedDate = DateTime.Now.AddDays(-35),
                    ReadTime = 7,
                    ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800",
                    Excerpt = "Goal setting is crucial for health success, but unrealistic goals often lead to failure. Learn how to set achievable, meaningful health goals that keep you motivated.",
                    Content = GetFullContent("health-goals"),
                    MetaDescription = "Learn how to set realistic health goals for long-term success. Expert tips on creating achievable wellness objectives that keep you motivated.",
                    MetaKeywords = "health goals, wellness goals, goal setting, health planning, motivation"
                },
                new BlogPost
                {
                    Id = 13,
                    Title = "The Benefits of Strength Training for Women",
                    Slug = "strength-training-benefits-women",
                    Category = "Fitness",
                    Author = "Fitness Coach",
                    PublishedDate = DateTime.Now.AddDays(-38),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800",
                    Excerpt = "Strength training offers numerous benefits for women beyond muscle building. Discover how resistance training supports bone health, metabolism, and overall wellness.",
                    Content = GetFullContent("strength-training"),
                    MetaDescription = "Discover the many benefits of strength training for women. Learn how resistance training supports health, strength, and wellness.",
                    MetaKeywords = "strength training, women's fitness, resistance training, weight lifting, fitness for women"
                },
                new BlogPost
                {
                    Id = 14,
                    Title = "Healthy Snacking: Smart Choices for Weight Control",
                    Slug = "healthy-snacking-weight-control",
                    Category = "Nutrition",
                    Author = "Nutrition Expert",
                    PublishedDate = DateTime.Now.AddDays(-40),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    Excerpt = "Smart snacking can support your weight management goals. Learn which snacks are best for satiety, energy, and maintaining a healthy weight throughout the day.",
                    Content = GetFullContent("healthy-snacking"),
                    MetaDescription = "Learn about healthy snacking for weight control. Discover smart snack choices that support your health goals and keep you satisfied.",
                    MetaKeywords = "healthy snacks, weight control, snacking, healthy eating, weight management"
                },
                new BlogPost
                {
                    Id = 15,
                    Title = "How to Shop for Healthy Products: A Buyer's Guide",
                    Slug = "shop-healthy-products-guide",
                    Category = "Shopping",
                    Author = "Health Advisor",
                    PublishedDate = DateTime.Now.AddDays(-42),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=800",
                    Excerpt = "Make informed decisions when shopping for health products. Learn how to evaluate quality, read labels, and choose products that align with your wellness goals.",
                    Content = GetFullContent("shopping-guide"),
                    MetaDescription = "Expert buyer's guide to shopping for healthy products. Learn how to evaluate quality and make informed purchasing decisions.",
                    MetaKeywords = "health products, shopping guide, product reviews, healthy shopping, wellness products"
                },
                new BlogPost
                {
                    Id = 16,
                    Title = "Building Healthy Habits That Last: The Psychology of Change",
                    Slug = "building-healthy-habits-psychology",
                    Category = "Wellness",
                    Author = "Dr. Michael Chen",
                    PublishedDate = DateTime.Now.AddDays(-45),
                    ReadTime = 12,
                    ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800",
                    Excerpt = "Understanding the psychology behind habit formation can help you create lasting lifestyle changes. Learn evidence-based strategies for building and maintaining healthy habits.",
                    Content = GetFullContent("healthy-habits"),
                    MetaDescription = "Learn the psychology behind building lasting healthy habits. Discover evidence-based strategies for creating sustainable lifestyle changes.",
                    MetaKeywords = "healthy habits, habit formation, lifestyle changes, behavior change, wellness psychology"
                }
            };
        }

        private string GetFullContent(string topic)
        {
            // This would normally come from a database or content management system
            // For now, returning comprehensive content for SEO
            return $@"
<h2>Introduction</h2>
<p>This comprehensive guide explores {topic} in detail, providing expert insights and practical advice to help you on your health and wellness journey.</p>

<h2>Key Points</h2>
<p>Understanding the fundamentals is crucial for success. Let's dive deep into the essential aspects that will help you achieve your goals.</p>

<h2>Practical Applications</h2>
<p>Here are actionable steps you can take immediately to improve your health and wellness. These evidence-based strategies have been proven effective.</p>

<h2>Common Questions</h2>
<p>Many people have questions about {topic}. We address the most frequently asked questions to help clarify any concerns.</p>

<h2>Conclusion</h2>
<p>By implementing these strategies, you'll be on your way to better health and wellness. Remember, consistency is key to long-term success.</p>
";
        }

        private List<BlogPost> GetBlogPostsArabic()
        {
            return new List<BlogPost>
            {
                new BlogPost
                {
                    Id = 1,
                    Title = "10 نصائح أساسية لإدارة الوزن لصحة أفضل",
                    Slug = "10-essential-weight-management-tips",
                    Category = "إدارة الوزن",
                    Author = "د. سارة جونسون",
                    PublishedDate = DateTime.Now.AddDays(-5),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800",
                    Excerpt = "اكتشف استراتيجيات مثبتة لإدارة الوزن الفعالة التي تتجاوز النظام الغذائي. تعلم عن تغييرات نمط الحياة المستدامة، والتحكم في الكميات، وأهمية التمرين المنتظم في تحقيق أهدافك الصحية.",
                    Content = GetFullContentArabic("إدارة الوزن"),
                    MetaDescription = "تعلم 10 نصائح أساسية لإدارة الوزن من خبراء الصحة. اكتشف استراتيجيات مستدامة للحفاظ على وزن صحي من خلال التغذية السليمة والتمارين الرياضية وتغييرات نمط الحياة.",
                    MetaKeywords = "إدارة الوزن، نصائح فقدان الوزن، الوزن الصحي، نصائح النظام الغذائي، التحكم في الوزن"
                },
                new BlogPost
                {
                    Id = 2,
                    Title = "الدليل الشامل لتخطيط وجبات صحية",
                    Slug = "ultimate-guide-healthy-meal-planning",
                    Category = "التغذية",
                    Author = "خبير التغذية",
                    PublishedDate = DateTime.Now.AddDays(-8),
                    ReadTime = 12,
                    ImageUrl = "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=800",
                    Excerpt = "أتقن فن تخطيط الوجبات مع دليلنا الشامل. تعلم كيفية إنشاء خطط وجبات متوازنة ومغذية تدعم أهدافك الصحية وتوفر الوقت في المطبخ.",
                    Content = GetFullContentArabic("تخطيط الوجبات"),
                    MetaDescription = "دليل شامل لتخطيط الوجبات الصحية. تعلم كيفية إنشاء خطط وجبات مغذية ومتوازنة تدعم أهداف العافية لديك وتبسط روتينك اليومي.",
                    MetaKeywords = "تخطيط الوجبات، وجبات صحية، تخطيط التغذية، تحضير الوجبات، نظام غذائي صحي"
                },
                new BlogPost
                {
                    Id = 3,
                    Title = "فهم احتياجات جسمك الغذائية",
                    Slug = "understanding-body-nutritional-needs",
                    Category = "التغذية",
                    Author = "د. مايكل تشين",
                    PublishedDate = DateTime.Now.AddDays(-12),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    Excerpt = "جسمك لديه متطلبات غذائية فريدة تتغير على مدار الحياة. تشرح هذه المقالة كيفية فهم وتلبية احتياجاتك الغذائية الفردية للصحة المثلى.",
                    Content = GetFullContentArabic("الاحتياجات الغذائية"),
                    MetaDescription = "تعلم عن المتطلبات الغذائية الفريدة لجسمك وكيفية تلبيتها. دليل أساسي لفهم الفيتامينات والمعادن والمواد المغذية الكبرى.",
                    MetaKeywords = "الاحتياجات الغذائية، الفيتامينات، المعادن، الأكل الصحي، النظام الغذائي المتوازن، العناصر الغذائية"
                },
                new BlogPost
                {
                    Id = 4,
                    Title = "أفضل المكملات الغذائية للصحة والعافية",
                    Slug = "best-supplements-health-wellness",
                    Category = "المكملات الغذائية",
                    Author = "مستشار الصحة",
                    PublishedDate = DateTime.Now.AddDays(-15),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1556910103-1c02745aae4d?w=800",
                    Excerpt = "استكشف عالم المكملات الغذائية واكتشف أيها يمكن أن يفيد صحتك حقاً. تعلم عن الجودة والجرعة ومتى تكون المكملات ضرورية بالفعل.",
                    Content = GetFullContentArabic("المكملات الغذائية"),
                    MetaDescription = "دليل خبير لأفضل مكملات الصحة والعافية. تعلم أي المكملات تستحق تناولها وكيفية اختيار منتجات عالية الجودة.",
                    MetaKeywords = "المكملات الغذائية، الفيتامينات، مكملات الصحة، المكملات الغذائية، منتجات العافية"
                },
                new BlogPost
                {
                    Id = 5,
                    Title = "كيفية بناء روتين تمرين مستدام",
                    Slug = "build-sustainable-exercise-routine",
                    Category = "اللياقة البدنية",
                    Author = "مدرب اللياقة",
                    PublishedDate = DateTime.Now.AddDays(-18),
                    ReadTime = 7,
                    ImageUrl = "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800",
                    Excerpt = "إنشاء روتين تمرين يمكنك الالتزام به على المدى الطويل هو مفتاح الحفاظ على اللياقة. اكتشف استراتيجيات بناء العادات التي تدوم وتحافظ على تحفيزك.",
                    Content = GetFullContentArabic("روتين التمرين"),
                    MetaDescription = "تعلم كيفية بناء روتين تمرين مستدام يناسب نمط حياتك. نصائح لإنشاء عادات لياقة دائمة والبقاء متحمساً.",
                    MetaKeywords = "روتين التمرين، نصائح اللياقة، خطة التمرين، عادات التمرين، تحفيز اللياقة"
                },
                new BlogPost
                {
                    Id = 6,
                    Title = "علم الصيام المتقطع",
                    Slug = "science-intermittent-fasting",
                    Category = "التغذية",
                    Author = "د. سارة جونسون",
                    PublishedDate = DateTime.Now.AddDays(-20),
                    ReadTime = 11,
                    ImageUrl = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=800",
                    Excerpt = "اكتسب الصيام المتقطع شعبية، ولكن ماذا يقول العلم؟ تعلم عن الفوائد المدعومة بالبحث والطرق ومن يجب أن يجرب نمط الأكل هذا.",
                    Content = GetFullContentArabic("الصيام المتقطع"),
                    MetaDescription = "دليل علمي للصيام المتقطع. تعلم عن الفوائد المدعومة بالبحث والطرق المختلفة وما إذا كان مناسباً لك.",
                    MetaKeywords = "الصيام المتقطع، فوائد الصيام، فقدان الوزن، أنماط الأكل، الفوائد الصحية"
                },
                new BlogPost
                {
                    Id = 7,
                    Title = "الترطيب: مفتاح الصحة المثلى",
                    Slug = "hydration-key-optimal-health",
                    Category = "العافية",
                    Author = "مستشار الصحة",
                    PublishedDate = DateTime.Now.AddDays(-22),
                    ReadTime = 6,
                    ImageUrl = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800",
                    Excerpt = "الترطيب المناسب يؤثر على كل جانب من جوانب صحتك. اكتشف لماذا يهم تناول الماء، وكم تحتاج حقاً، وعلامات أنك لا تشرب ما يكفي.",
                    Content = GetFullContentArabic("الترطيب"),
                    MetaDescription = "تعلم لماذا الترطيب مهم للصحة المثلى. اكتشف كمية الماء التي تحتاجها وعلامات الجفاف.",
                    MetaKeywords = "الترطيب، تناول الماء، الصحة، الجفاف، العافية، الماء اليومي"
                },
                new BlogPost
                {
                    Id = 8,
                    Title = "النوم وتأثيره على إدارة الوزن",
                    Slug = "sleep-impact-weight-management",
                    Category = "إدارة الوزن",
                    Author = "د. مايكل تشين",
                    PublishedDate = DateTime.Now.AddDays(-25),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1541781774459-bb2af2f05b55?w=800",
                    Excerpt = "هل تعلم أن جودة النوم تؤثر بشكل مباشر على وزنك؟ تعلم عن الصلة بين النوم والتمثيل الغذائي، وكيفية تحسين نومك لصحة أفضل.",
                    Content = GetFullContentArabic("النوم والوزن"),
                    MetaDescription = "اكتشف كيف يؤثر النوم على إدارة الوزن والتمثيل الغذائي. تعلم نصائح لتحسين جودة النوم لنتائج صحية أفضل.",
                    MetaKeywords = "النوم والوزن، صحة النوم، التمثيل الغذائي، إدارة الوزن، النوم الصحي"
                },
                new BlogPost
                {
                    Id = 9,
                    Title = "قراءة ملصقات التغذية: دليل شامل",
                    Slug = "reading-nutrition-labels-guide",
                    Category = "التغذية",
                    Author = "خبير التغذية",
                    PublishedDate = DateTime.Now.AddDays(-28),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1466637574441-749b8f19452f?w=800",
                    Excerpt = "أتقن مهارة قراءة ملصقات التغذية لاتخاذ خيارات غذائية مستنيرة. تعلم ما يجب البحث عنه، وما يجب تجنبه، وكيفية مقارنة المنتجات بشكل فعال.",
                    Content = GetFullContentArabic("ملصقات التغذية"),
                    MetaDescription = "دليل شامل لقراءة وفهم ملصقات التغذية. اتخذ خيارات غذائية مستنيرة مع نصائح الخبراء حول فك رموز ملصقات المنتجات.",
                    MetaKeywords = "ملصقات التغذية، ملصقات الطعام، الأكل الصحي، قراءة الملصقات، خيارات الطعام"
                },
                new BlogPost
                {
                    Id = 10,
                    Title = "تقنيات إدارة الإجهاد لصحة أفضل",
                    Slug = "stress-management-techniques",
                    Category = "العافية",
                    Author = "مستشار الصحة",
                    PublishedDate = DateTime.Now.AddDays(-30),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800",
                    Excerpt = "يمكن أن يؤثر الإجهاد المزمن سلباً على صحتك. اكتشف تقنيات فعالة لإدارة الإجهاد تساعدك على الحفاظ على التوازن وتحسين صحتك العامة.",
                    Content = GetFullContentArabic("إدارة الإجهاد"),
                    MetaDescription = "تعلم تقنيات فعالة لإدارة الإجهاد لصحة أفضل. اكتشف طرقاً مثبتة لتقليل الإجهاد وتحسين نوعية حياتك.",
                    MetaKeywords = "إدارة الإجهاد، تخفيف الإجهاد، العافية، الصحة العقلية، تقنيات الاسترخاء"
                },
                new BlogPost
                {
                    Id = 11,
                    Title = "البروبيوتيك وصحة الأمعاء: كل ما تحتاج معرفته",
                    Slug = "probiotics-gut-health-guide",
                    Category = "المكملات الغذائية",
                    Author = "د. سارة جونسون",
                    PublishedDate = DateTime.Now.AddDays(-32),
                    ReadTime = 11,
                    ImageUrl = "https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?w=800",
                    Excerpt = "صحة أمعائك تؤثر على جسمك بالكامل. تعلم عن البروبيوتيك والبريبايوتكس وكيفية دعم جهازك الهضمي للعافية المثلى.",
                    Content = GetFullContentArabic("البروبيوتيك"),
                    MetaDescription = "دليل شامل للبروبيوتيك وصحة الأمعاء. تعلم كيف يعمل البروبيوتيك وكيفية تحسين صحتك الهضمية بشكل طبيعي.",
                    MetaKeywords = "البروبيوتيك، صحة الأمعاء، الصحة الهضمية، الميكروبيوم، أمعاء صحية، المكملات"
                },
                new BlogPost
                {
                    Id = 12,
                    Title = "وضع أهداف صحية واقعية للنجاح على المدى الطويل",
                    Slug = "realistic-health-goals",
                    Category = "العافية",
                    Author = "مدرب اللياقة",
                    PublishedDate = DateTime.Now.AddDays(-35),
                    ReadTime = 7,
                    ImageUrl = "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800",
                    Excerpt = "وضع الأهداف مهم لنجاح الصحة، ولكن الأهداف غير الواقعية غالباً ما تؤدي إلى الفشل. تعلم كيفية وضع أهداف صحية قابلة للتحقيق وذات معنى تحافظ على تحفيزك.",
                    Content = GetFullContentArabic("الأهداف الصحية"),
                    MetaDescription = "تعلم كيفية وضع أهداف صحية واقعية للنجاح على المدى الطويل. نصائح الخبراء حول إنشاء أهداف عافية قابلة للتحقيق تحافظ على تحفيزك.",
                    MetaKeywords = "الأهداف الصحية، أهداف العافية، وضع الأهداف، تخطيط الصحة، التحفيز"
                },
                new BlogPost
                {
                    Id = 13,
                    Title = "فوائد تدريب القوة للنساء",
                    Slug = "strength-training-benefits-women",
                    Category = "اللياقة البدنية",
                    Author = "مدرب اللياقة",
                    PublishedDate = DateTime.Now.AddDays(-38),
                    ReadTime = 9,
                    ImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800",
                    Excerpt = "يوفر تدريب القوة فوائد عديدة للنساء تتجاوز بناء العضلات. اكتشف كيف يدعم تدريب المقاومة صحة العظام والتمثيل الغذائي والعافية العامة.",
                    Content = GetFullContentArabic("تدريب القوة"),
                    MetaDescription = "اكتشف الفوائد العديدة لتدريب القوة للنساء. تعلم كيف يدعم تدريب المقاومة الصحة والقوة والعافية.",
                    MetaKeywords = "تدريب القوة، لياقة النساء، تدريب المقاومة، رفع الأثقال، اللياقة للنساء"
                },
                new BlogPost
                {
                    Id = 14,
                    Title = "الوجبات الخفيفة الصحية: خيارات ذكية للتحكم في الوزن",
                    Slug = "healthy-snacking-weight-control",
                    Category = "التغذية",
                    Author = "خبير التغذية",
                    PublishedDate = DateTime.Now.AddDays(-40),
                    ReadTime = 8,
                    ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    Excerpt = "يمكن للوجبات الخفيفة الذكية أن تدعم أهداف إدارة الوزن لديك. تعلم أي الوجبات الخفيفة هي الأفضل للشبع والطاقة والحفاظ على وزن صحي طوال اليوم.",
                    Content = GetFullContentArabic("الوجبات الخفيفة الصحية"),
                    MetaDescription = "تعلم عن الوجبات الخفيفة الصحية للتحكم في الوزن. اكتشف خيارات الوجبات الخفيفة الذكية التي تدعم أهدافك الصحية وتحافظ على رضاك.",
                    MetaKeywords = "وجبات خفيفة صحية، التحكم في الوزن، الوجبات الخفيفة، الأكل الصحي، إدارة الوزن"
                },
                new BlogPost
                {
                    Id = 15,
                    Title = "كيفية التسوق للمنتجات الصحية: دليل المشتري",
                    Slug = "shop-healthy-products-guide",
                    Category = "التسوق",
                    Author = "مستشار الصحة",
                    PublishedDate = DateTime.Now.AddDays(-42),
                    ReadTime = 10,
                    ImageUrl = "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=800",
                    Excerpt = "اتخذ قرارات مستنيرة عند التسوق للمنتجات الصحية. تعلم كيفية تقييم الجودة وقراءة الملصقات واختيار المنتجات التي تتماشى مع أهداف العافية لديك.",
                    Content = GetFullContentArabic("دليل التسوق"),
                    MetaDescription = "دليل المشتري الخبير للتسوق للمنتجات الصحية. تعلم كيفية تقييم الجودة واتخاذ قرارات الشراء المستنيرة.",
                    MetaKeywords = "المنتجات الصحية، دليل التسوق، مراجعات المنتجات، التسوق الصحي، منتجات العافية"
                },
                new BlogPost
                {
                    Id = 16,
                    Title = "بناء عادات صحية تدوم: علم النفس للتغيير",
                    Slug = "building-healthy-habits-psychology",
                    Category = "العافية",
                    Author = "د. مايكل تشين",
                    PublishedDate = DateTime.Now.AddDays(-45),
                    ReadTime = 12,
                    ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800",
                    Excerpt = "فهم علم النفس وراء تكوين العادات يمكن أن يساعدك على إنشاء تغييرات دائمة في نمط الحياة. تعلم استراتيجيات مدعومة بالأدلة لبناء والحفاظ على العادات الصحية.",
                    Content = GetFullContentArabic("العادات الصحية"),
                    MetaDescription = "تعلم علم النفس وراء بناء العادات الصحية الدائمة. اكتشف استراتيجيات مدعومة بالأدلة لإنشاء تغييرات مستدامة في نمط الحياة.",
                    MetaKeywords = "العادات الصحية، تكوين العادات، تغييرات نمط الحياة، تغيير السلوك، علم نفس العافية"
                }
            };
        }

        private string GetFullContentArabic(string topic)
        {
            return $@"
<h2>مقدمة</h2>
<p>يستكشف هذا الدليل الشامل {topic} بالتفصيل، ويوفر رؤى الخبراء ونصائح عملية لمساعدتك في رحلتك الصحية والعافية.</p>

<h2>النقاط الرئيسية</h2>
<p>فهم الأساسيات أمر بالغ الأهمية للنجاح. دعنا نتعمق في الجوانب الأساسية التي ستساعدك على تحقيق أهدافك.</p>

<h2>التطبيقات العملية</h2>
<p>فيما يلي خطوات قابلة للتنفيذ يمكنك اتخاذها فوراً لتحسين صحتك وعافيتك. هذه الاستراتيجيات المدعومة بالأدلة أثبتت فعاليتها.</p>

<h2>الأسئلة الشائعة</h2>
<p>يطرح الكثير من الناس أسئلة حول {topic}. نعالج الأسئلة الأكثر شيوعاً لمساعدتك على توضيح أي مخاوف.</p>

<h2>الخاتمة</h2>
<p>من خلال تنفيذ هذه الاستراتيجيات، ستكون في طريقك إلى صحة وعافية أفضل. تذكر، الاتساق هو المفتاح للنجاح على المدى الطويل.</p>
";
        }
    }

    public class BlogPost
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

