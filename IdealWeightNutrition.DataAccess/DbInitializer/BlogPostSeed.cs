using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;

namespace IdealWeightNutrition.DataAccess.DbInitializer
{
    /// <summary>
    /// Seeds all existing static blog content from the original BlogController
    /// </summary>
    public static class BlogPostSeed
    {
        public static void Seed(ApplicationDBContext db)
        {
            if (db.BlogPosts.Any())
                return;

            var blogs = GetSeedBlogs();
            db.BlogPosts.AddRange(blogs);
            db.SaveChanges();
        }

        private static List<BlogPost> GetSeedBlogs()
        {
            var now = DateTime.UtcNow;
            return new List<BlogPost>
            {
                Create("10-essential-weight-management-tips", "10 Essential Weight Management Tips for a Healthier You", "10 نصائح أساسية لإدارة الوزن لصحة أفضل",
                    "Weight Management", "إدارة الوزن", "Dr. Sarah Johnson", "د. سارة جونسون",
                    now.AddDays(-5), 8, "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800",
                    "Discover proven strategies for effective weight management that go beyond dieting. Learn about sustainable lifestyle changes, portion control, and the importance of regular exercise in achieving your health goals.",
                    "اكتشف استراتيجيات مثبتة لإدارة الوزن الفعالة التي تتجاوز النظام الغذائي. تعلم عن تغييرات نمط الحياة المستدامة، والتحكم في الكميات، وأهمية التمرين المنتظم في تحقيق أهدافك الصحية.",
                    GetContentEn("weight-management"), GetContentAr("إدارة الوزن"),
                    "Learn 10 essential weight management tips from health experts. Discover sustainable strategies for maintaining a healthy weight through proper nutrition, exercise, and lifestyle changes.",
                    "تعلم 10 نصائح أساسية لإدارة الوزن من خبراء الصحة. اكتشف استراتيجيات مستدامة للحفاظ على وزن صحي من خلال التغذية السليمة والتمارين الرياضية وتغييرات نمط الحياة.",
                    "weight management, weight loss tips, healthy weight, diet advice, weight control",
                    "إدارة الوزن، نصائح فقدان الوزن، الوزن الصحي، نصائح النظام الغذائي، التحكم في الوزن"),
                Create("ultimate-guide-healthy-meal-planning", "The Ultimate Guide to Healthy Meal Planning", "الدليل الشامل لتخطيط وجبات صحية",
                    "Nutrition", "التغذية", "Nutrition Expert", "خبير التغذية",
                    now.AddDays(-8), 12, "https://images.unsplash.com/photo-1495521821757-a1efb6729352?w=800",
                    "Master the art of meal planning with our comprehensive guide. Learn how to create balanced, nutritious meal plans that support your health goals and save time in the kitchen.",
                    "أتقن فن تخطيط الوجبات مع دليلنا الشامل. تعلم كيفية إنشاء خطط وجبات متوازنة ومغذية تدعم أهدافك الصحية وتوفر الوقت في المطبخ.",
                    GetContentEn("meal planning"), GetContentAr("تخطيط الوجبات"),
                    "Complete guide to healthy meal planning. Learn how to create nutritious, balanced meal plans that support your wellness goals and simplify your daily routine.",
                    "دليل شامل لتخطيط الوجبات الصحية. تعلم كيفية إنشاء خطط وجبات مغذية ومتوازنة تدعم أهداف العافية لديك وتبسط روتينك اليومي.",
                    "meal planning, healthy meals, nutrition planning, meal prep, healthy diet",
                    "تخطيط الوجبات، وجبات صحية، تخطيط التغذية، تحضير الوجبات، نظام غذائي صحي"),
                Create("understanding-body-nutritional-needs", "Understanding Your Body's Nutritional Needs", "فهم احتياجات جسمك الغذائية",
                    "Nutrition", "التغذية", "Dr. Michael Chen", "د. مايكل تشين",
                    now.AddDays(-12), 10, "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    "Your body has unique nutritional requirements that change throughout life. This article explains how to understand and meet your individual nutritional needs for optimal health.",
                    "جسمك لديه متطلبات غذائية فريدة تتغير على مدار الحياة. تشرح هذه المقالة كيفية فهم وتلبية احتياجاتك الغذائية الفردية للصحة المثلى.",
                    GetContentEn("nutritional needs"), GetContentAr("الاحتياجات الغذائية"),
                    "Learn about your body's unique nutritional requirements and how to meet them. Essential guide to understanding vitamins, minerals, and macro-nutrients.",
                    "تعلم عن المتطلبات الغذائية الفريدة لجسمك وكيفية تلبيتها. دليل أساسي لفهم الفيتامينات والمعادن والمواد المغذية الكبرى.",
                    "nutritional needs, vitamins, minerals, healthy eating, balanced diet, nutrients",
                    "الاحتياجات الغذائية، الفيتامينات، المعادن، الأكل الصحي، النظام الغذائي المتوازن، العناصر الغذائية"),
                Create("best-supplements-health-wellness", "Best Supplements for Health and Wellness", "أفضل المكملات الغذائية للصحة والعافية",
                    "Supplements", "المكملات الغذائية", "Health Advisor", "مستشار الصحة",
                    now.AddDays(-15), 9, "https://images.unsplash.com/photo-1556910103-1c02745aae4d?w=800",
                    "Explore the world of dietary supplements and discover which ones can truly benefit your health. Learn about quality, dosage, and when supplements are actually necessary.",
                    "استكشف عالم المكملات الغذائية واكتشف أيها يمكن أن يفيد صحتك حقاً. تعلم عن الجودة والجرعة ومتى تكون المكملات ضرورية بالفعل.",
                    GetContentEn("supplements"), GetContentAr("المكملات الغذائية"),
                    "Expert guide to the best health and wellness supplements. Learn which supplements are worth taking and how to choose quality products.",
                    "دليل خبير لأفضل مكملات الصحة والعافية. تعلم أي المكملات تستحق تناولها وكيفية اختيار منتجات عالية الجودة.",
                    "supplements, vitamins, health supplements, dietary supplements, wellness products",
                    "المكملات الغذائية، الفيتامينات، مكملات الصحة، المكملات الغذائية، منتجات العافية"),
                Create("build-sustainable-exercise-routine", "How to Build a Sustainable Exercise Routine", "كيفية بناء روتين تمرين مستدام",
                    "Fitness", "اللياقة البدنية", "Fitness Coach", "مدرب اللياقة",
                    now.AddDays(-18), 7, "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=800",
                    "Creating an exercise routine that you can stick to long-term is key to maintaining fitness. Discover strategies for building habits that last and keep you motivated.",
                    "إنشاء روتين تمرين يمكنك الالتزام به على المدى الطويل هو مفتاح الحفاظ على اللياقة. اكتشف استراتيجيات بناء العادات التي تدوم وتحافظ على تحفيزك.",
                    GetContentEn("exercise routine"), GetContentAr("روتين التمرين"),
                    "Learn how to build a sustainable exercise routine that fits your lifestyle. Tips for creating lasting fitness habits and staying motivated.",
                    "تعلم كيفية بناء روتين تمرين مستدام يناسب نمط حياتك. نصائح لإنشاء عادات لياقة دائمة والبقاء متحمساً.",
                    "exercise routine, fitness tips, workout plan, exercise habits, fitness motivation",
                    "روتين التمرين، نصائح اللياقة، خطة التمرين، عادات التمرين، تحفيز اللياقة"),
                Create("science-intermittent-fasting", "The Science Behind Intermittent Fasting", "علم الصيام المتقطع",
                    "Nutrition", "التغذية", "Dr. Sarah Johnson", "د. سارة جونسون",
                    now.AddDays(-20), 11, "https://images.unsplash.com/photo-1542838132-92c53300491e?w=800",
                    "Intermittent fasting has gained popularity, but what does science say? Learn about the research-backed benefits, methods, and who should try this eating pattern.",
                    "اكتسب الصيام المتقطع شعبية، ولكن ماذا يقول العلم؟ تعلم عن الفوائد المدعومة بالبحث والطرق ومن يجب أن يجرب نمط الأكل هذا.",
                    GetContentEn("intermittent fasting"), GetContentAr("الصيام المتقطع"),
                    "Scientific guide to intermittent fasting. Learn about research-backed benefits, different methods, and whether it's right for you.",
                    "دليل علمي للصيام المتقطع. تعلم عن الفوائد المدعومة بالبحث والطرق المختلفة وما إذا كان مناسباً لك.",
                    "intermittent fasting, fasting benefits, weight loss, eating patterns, health benefits",
                    "الصيام المتقطع، فوائد الصيام، فقدان الوزن، أنماط الأكل، الفوائد الصحية"),
                Create("hydration-key-optimal-health", "Hydration: The Key to Optimal Health", "الترطيب: مفتاح الصحة المثلى",
                    "Wellness", "العافية", "Health Advisor", "مستشار الصحة",
                    now.AddDays(-22), 6, "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=800",
                    "Proper hydration affects every aspect of your health. Discover why water intake matters, how much you really need, and signs that you're not drinking enough.",
                    "الترطيب المناسب يؤثر على كل جانب من جوانب صحتك. اكتشف لماذا يهم تناول الماء، وكم تحتاج حقاً، وعلامات أنك لا تشرب ما يكفي.",
                    GetContentEn("hydration"), GetContentAr("الترطيب"),
                    "Learn why hydration is crucial for optimal health. Discover how much water you need and the signs of dehydration.",
                    "تعلم لماذا الترطيب مهم للصحة المثلى. اكتشف كمية الماء التي تحتاجها وعلامات الجفاف.",
                    "hydration, water intake, health, dehydration, wellness, daily water",
                    "الترطيب، تناول الماء، الصحة، الجفاف، العافية، الماء اليومي"),
                Create("sleep-impact-weight-management", "Sleep and Its Impact on Weight Management", "النوم وتأثيره على إدارة الوزن",
                    "Weight Management", "إدارة الوزن", "Dr. Michael Chen", "د. مايكل تشين",
                    now.AddDays(-25), 8, "https://images.unsplash.com/photo-1541781774459-bb2af2f05b55?w=800",
                    "Did you know that sleep quality directly affects your weight? Learn about the connection between sleep and metabolism, and how to improve your sleep for better health.",
                    "هل تعلم أن جودة النوم تؤثر بشكل مباشر على وزنك؟ تعلم عن الصلة بين النوم والتمثيل الغذائي، وكيفية تحسين نومك لصحة أفضل.",
                    GetContentEn("sleep-weight"), GetContentAr("النوم والوزن"),
                    "Discover how sleep affects weight management and metabolism. Learn tips for improving sleep quality for better health outcomes.",
                    "اكتشف كيف يؤثر النوم على إدارة الوزن والتمثيل الغذائي. تعلم نصائح لتحسين جودة النوم لنتائج صحية أفضل.",
                    "sleep and weight, sleep health, metabolism, weight management, healthy sleep",
                    "النوم والوزن، صحة النوم، التمثيل الغذائي، إدارة الوزن، النوم الصحي"),
                Create("reading-nutrition-labels-guide", "Reading Nutrition Labels: A Complete Guide", "قراءة ملصقات التغذية: دليل شامل",
                    "Nutrition", "التغذية", "Nutrition Expert", "خبير التغذية",
                    now.AddDays(-28), 9, "https://images.unsplash.com/photo-1466637574441-749b8f19452f?w=800",
                    "Master the skill of reading nutrition labels to make informed food choices. Learn what to look for, what to avoid, and how to compare products effectively.",
                    "أتقن مهارة قراءة ملصقات التغذية لاتخاذ خيارات غذائية مستنيرة. تعلم ما يجب البحث عنه، وما يجب تجنبه، وكيفية مقارنة المنتجات بشكل فعال.",
                    GetContentEn("nutrition labels"), GetContentAr("ملصقات التغذية"),
                    "Complete guide to reading and understanding nutrition labels. Make informed food choices with expert tips on deciphering product labels.",
                    "دليل شامل لقراءة وفهم ملصقات التغذية. اتخذ خيارات غذائية مستنيرة مع نصائح الخبراء حول فك رموز ملصقات المنتجات.",
                    "nutrition labels, food labels, healthy eating, reading labels, food choices",
                    "ملصقات التغذية، ملصقات الطعام، الأكل الصحي، قراءة الملصقات، خيارات الطعام"),
                Create("stress-management-techniques", "Stress Management Techniques for Better Health", "تقنيات إدارة الإجهاد لصحة أفضل",
                    "Wellness", "العافية", "Health Advisor", "مستشار الصحة",
                    now.AddDays(-30), 10, "https://images.unsplash.com/photo-1506126613408-eca07ce68773?w=800",
                    "Chronic stress can negatively impact your health. Discover effective stress management techniques that help you maintain balance and improve your overall well-being.",
                    "يمكن أن يؤثر الإجهاد المزمن سلباً على صحتك. اكتشف تقنيات فعالة لإدارة الإجهاد تساعدك على الحفاظ على التوازن وتحسين صحتك العامة.",
                    GetContentEn("stress management"), GetContentAr("إدارة الإجهاد"),
                    "Learn effective stress management techniques for better health. Discover proven methods to reduce stress and improve your quality of life.",
                    "تعلم تقنيات فعالة لإدارة الإجهاد لصحة أفضل. اكتشف طرقاً مثبتة لتقليل الإجهاد وتحسين نوعية حياتك.",
                    "stress management, stress relief, wellness, mental health, relaxation techniques",
                    "إدارة الإجهاد، تخفيف الإجهاد، العافية، الصحة العقلية، تقنيات الاسترخاء"),
                Create("probiotics-gut-health-guide", "Probiotics and Gut Health: Everything You Need to Know", "البروبيوتيك وصحة الأمعاء: كل ما تحتاج معرفته",
                    "Supplements", "المكملات الغذائية", "Dr. Sarah Johnson", "د. سارة جونسون",
                    now.AddDays(-32), 11, "https://images.unsplash.com/photo-1476718406336-bb5a9690ee2a?w=800",
                    "Your gut health affects your entire body. Learn about probiotics, prebiotics, and how to support your digestive system for optimal wellness.",
                    "صحة أمعائك تؤثر على جسمك بالكامل. تعلم عن البروبيوتيك والبريبايوتكس وكيفية دعم جهازك الهضمي للعافية المثلى.",
                    GetContentEn("probiotics"), GetContentAr("البروبيوتيك"),
                    "Complete guide to probiotics and gut health. Learn how probiotics work and how to improve your digestive health naturally.",
                    "دليل شامل للبروبيوتيك وصحة الأمعاء. تعلم كيف يعمل البروبيوتيك وكيفية تحسين صحتك الهضمية بشكل طبيعي.",
                    "probiotics, gut health, digestive health, microbiome, healthy gut, supplements",
                    "البروبيوتيك، صحة الأمعاء، الصحة الهضمية، الميكروبيوم، أمعاء صحية، المكملات"),
                Create("realistic-health-goals", "Setting Realistic Health Goals for Long-Term Success", "وضع أهداف صحية واقعية للنجاح على المدى الطويل",
                    "Wellness", "العافية", "Fitness Coach", "مدرب اللياقة",
                    now.AddDays(-35), 7, "https://images.unsplash.com/photo-1576092768241-dec231879fc3?w=800",
                    "Goal setting is crucial for health success, but unrealistic goals often lead to failure. Learn how to set achievable, meaningful health goals that keep you motivated.",
                    "وضع الأهداف مهم لنجاح الصحة، ولكن الأهداف غير الواقعية غالباً ما تؤدي إلى الفشل. تعلم كيفية وضع أهداف صحية قابلة للتحقيق وذات معنى تحافظ على تحفيزك.",
                    GetContentEn("health goals"), GetContentAr("الأهداف الصحية"),
                    "Learn how to set realistic health goals for long-term success. Expert tips on creating achievable wellness objectives that keep you motivated.",
                    "تعلم كيفية وضع أهداف صحية واقعية للنجاح على المدى الطويل. نصائح الخبراء حول إنشاء أهداف عافية قابلة للتحقيق تحافظ على تحفيزك.",
                    "health goals, wellness goals, goal setting, health planning, motivation",
                    "الأهداف الصحية، أهداف العافية، وضع الأهداف، تخطيط الصحة، التحفيز"),
                Create("strength-training-benefits-women", "The Benefits of Strength Training for Women", "فوائد تدريب القوة للنساء",
                    "Fitness", "اللياقة البدنية", "Fitness Coach", "مدرب اللياقة",
                    now.AddDays(-38), 9, "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=800",
                    "Strength training offers numerous benefits for women beyond muscle building. Discover how resistance training supports bone health, metabolism, and overall wellness.",
                    "يوفر تدريب القوة فوائد عديدة للنساء تتجاوز بناء العضلات. اكتشف كيف يدعم تدريب المقاومة صحة العظام والتمثيل الغذائي والعافية العامة.",
                    GetContentEn("strength training"), GetContentAr("تدريب القوة"),
                    "Discover the many benefits of strength training for women. Learn how resistance training supports health, strength, and wellness.",
                    "اكتشف الفوائد العديدة لتدريب القوة للنساء. تعلم كيف يدعم تدريب المقاومة الصحة والقوة والعافية.",
                    "strength training, women's fitness, resistance training, weight lifting, fitness for women",
                    "تدريب القوة، لياقة النساء، تدريب المقاومة، رفع الأثقال، اللياقة للنساء"),
                Create("healthy-snacking-weight-control", "Healthy Snacking: Smart Choices for Weight Control", "الوجبات الخفيفة الصحية: خيارات ذكية للتحكم في الوزن",
                    "Nutrition", "التغذية", "Nutrition Expert", "خبير التغذية",
                    now.AddDays(-40), 8, "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800",
                    "Smart snacking can support your weight management goals. Learn which snacks are best for satiety, energy, and maintaining a healthy weight throughout the day.",
                    "يمكن للوجبات الخفيفة الذكية أن تدعم أهداف إدارة الوزن لديك. تعلم أي الوجبات الخفيفة هي الأفضل للشبع والطاقة والحفاظ على وزن صحي طوال اليوم.",
                    GetContentEn("healthy snacking"), GetContentAr("الوجبات الخفيفة الصحية"),
                    "Learn about healthy snacking for weight control. Discover smart snack choices that support your health goals and keep you satisfied.",
                    "تعلم عن الوجبات الخفيفة الصحية للتحكم في الوزن. اكتشف خيارات الوجبات الخفيفة الذكية التي تدعم أهدافك الصحية وتحافظ على رضاك.",
                    "healthy snacks, weight control, snacking, healthy eating, weight management",
                    "وجبات خفيفة صحية، التحكم في الوزن، الوجبات الخفيفة، الأكل الصحي، إدارة الوزن"),
                Create("shop-healthy-products-guide", "How to Shop for Healthy Products: A Buyer's Guide", "كيفية التسوق للمنتجات الصحية: دليل المشتري",
                    "Shopping", "التسوق", "Health Advisor", "مستشار الصحة",
                    now.AddDays(-42), 10, "https://images.unsplash.com/photo-1556742049-0cfed4f6a45d?w=800",
                    "Make informed decisions when shopping for health products. Learn how to evaluate quality, read labels, and choose products that align with your wellness goals.",
                    "اتخذ قرارات مستنيرة عند التسوق للمنتجات الصحية. تعلم كيفية تقييم الجودة وقراءة الملصقات واختيار المنتجات التي تتماشى مع أهداف العافية لديك.",
                    GetContentEn("shopping guide"), GetContentAr("دليل التسوق"),
                    "Expert buyer's guide to shopping for healthy products. Learn how to evaluate quality and make informed purchasing decisions.",
                    "دليل المشتري الخبير للتسوق للمنتجات الصحية. تعلم كيفية تقييم الجودة واتخاذ قرارات الشراء المستنيرة.",
                    "health products, shopping guide, product reviews, healthy shopping, wellness products",
                    "المنتجات الصحية، دليل التسوق، مراجعات المنتجات، التسوق الصحي، منتجات العافية"),
                Create("building-healthy-habits-psychology", "Building Healthy Habits That Last: The Psychology of Change", "بناء عادات صحية تدوم: علم النفس للتغيير",
                    "Wellness", "العافية", "Dr. Michael Chen", "د. مايكل تشين",
                    now.AddDays(-45), 12, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800",
                    "Understanding the psychology behind habit formation can help you create lasting lifestyle changes. Learn evidence-based strategies for building and maintaining healthy habits.",
                    "فهم علم النفس وراء تكوين العادات يمكن أن يساعدك على إنشاء تغييرات دائمة في نمط الحياة. تعلم استراتيجيات مدعومة بالأدلة لبناء والحفاظ على العادات الصحية.",
                    GetContentEn("healthy habits"), GetContentAr("العادات الصحية"),
                    "Learn the psychology behind building lasting healthy habits. Discover evidence-based strategies for creating sustainable lifestyle changes.",
                    "تعلم علم النفس وراء بناء العادات الصحية الدائمة. اكتشف استراتيجيات مدعومة بالأدلة لإنشاء تغييرات مستدامة في نمط الحياة.",
                    "healthy habits, habit formation, lifestyle changes, behavior change, wellness psychology",
                    "العادات الصحية، تكوين العادات، تغييرات نمط الحياة، تغيير السلوك، علم نفس العافية")
            };
        }

        private static BlogPost Create(string slug, string title, string titleAr,
            string category, string categoryAr, string author, string authorAr,
            DateTime publishedDate, int readTime, string imageUrl,
            string excerpt, string excerptAr, string content, string contentAr,
            string metaDesc, string metaDescAr, string metaKeys, string metaKeysAr)
        {
            return new BlogPost
            {
                Slug = slug,
                Title = title,
                TitleAr = titleAr,
                Category = category,
                CategoryAr = categoryAr,
                Author = author,
                AuthorAr = authorAr,
                PublishedDate = publishedDate,
                ReadTime = readTime,
                ImageUrl = imageUrl,
                Excerpt = excerpt,
                ExcerptAr = excerptAr,
                Content = content,
                ContentAr = contentAr,
                MetaDescription = metaDesc,
                MetaDescriptionAr = metaDescAr,
                MetaKeywords = metaKeys,
                MetaKeywordsAr = metaKeysAr,
                CreatedDate = DateTime.Now,
                IsDeleted = false
            };
        }

        private static string GetContentEn(string topic) => $@"
<h2>Introduction</h2>
<p>This comprehensive guide explores {topic} in detail, providing expert insights and practical advice to help you on your health and wellness journey.</p>

<h2>Key Points</h2>
<p>Understanding the fundamentals is crucial for success. Let's dive deep into the essential aspects that will help you achieve your goals.</p>

<h2>Practical Applications</h2>
<p>Here are actionable steps you can take immediately to improve your health and wellness. These evidence-based strategies have been proven effective.</p>

<h2>Common Questions</h2>
<p>Many people have questions about {topic}. We address the most frequently asked questions to help clarify any concerns.</p>

<h2>Conclusion</h2>
<p>By implementing these strategies, you'll be on your way to better health and wellness. Remember, consistency is key to long-term success.</p>";

        private static string GetContentAr(string topic) => $@"
<h2>مقدمة</h2>
<p>يستكشف هذا الدليل الشامل {topic} بالتفصيل، ويوفر رؤى الخبراء ونصائح عملية لمساعدتك في رحلتك الصحية والعافية.</p>

<h2>النقاط الرئيسية</h2>
<p>فهم الأساسيات أمر بالغ الأهمية للنجاح. دعنا نتعمق في الجوانب الأساسية التي ستساعدك على تحقيق أهدافك.</p>

<h2>التطبيقات العملية</h2>
<p>فيما يلي خطوات قابلة للتنفيذ يمكنك اتخاذها فوراً لتحسين صحتك وعافيتك. هذه الاستراتيجيات المدعومة بالأدلة أثبتت فعاليتها.</p>

<h2>الأسئلة الشائعة</h2>
<p>يطرح الكثير من الناس أسئلة حول {topic}. نعالج الأسئلة الأكثر شيوعاً لمساعدتك على توضيح أي مخاوف.</p>

<h2>الخاتمة</h2>
<p>من خلال تنفيذ هذه الاستراتيجيات، ستكون في طريقك إلى صحة وعافية أفضل. تذكر، الاتساق هو المفتاح للنجاح على المدى الطويل.</p>";
    }
}
