using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using IdealWeightNutrition.Utility;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.DataAccess.DbInitializer;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure file logging with daily rotation
var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
builder.Logging.AddFileLogger(logDirectory);

        // Configure file upload limits for video banner
        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 52428800; // 50MB for video uploads
            options.ValueLengthLimit = 52428800; // 50MB
            options.MultipartHeadersLengthLimit = 52428800; // 50MB
        });

        // Configure IIS server limits (for IIS hosting)
        builder.Services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = 52428800; // 50MB
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews(options =>
        {
            // Performance: Cache output for GET requests (except authenticated/admin pages)
            // IMPORTANT: All cache profiles vary by culture to prevent localization issues
            // Note: Culture is passed via query parameter, not header (cookie-based language switching)
            options.CacheProfiles.Add("DefaultCache", new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = 300, // 5 minutes
                VaryByQueryKeys = new[] { "*" } // Includes culture parameter when used
            });
            options.CacheProfiles.Add("LongCache", new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = 3600, // 1 hour for static content
                VaryByQueryKeys = new[] { "*" } // Includes culture parameter when used
            });
            
            // Register encrypted ID model binder
            options.ModelBinderProviders.Insert(0, new IdealWeightNutrition.ModelBinders.EncryptedIdModelBinderProvider());
        })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();
        
        // Performance: Add Response Caching for performance
        builder.Services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 64 * 1024; // 64 KB
            options.UseCaseSensitivePaths = false;
        });
        
        // Performance: Add Memory Cache for application-level caching
        builder.Services.AddMemoryCache();
        
        // Configure Localization
        builder.Services.AddLocalization();
        
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("ar"), // Arabic (default)
                new CultureInfo("en")  // English
            };

            // Set Arabic as default for both Culture and UICulture
            options.DefaultRequestCulture = new RequestCulture(culture: "ar", uiCulture: "ar");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            
            // Fallback to default culture (Arabic) if none is set
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            
            // Allow users to select language via cookie
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
        });
        
        // ==========================================
        // PERFORMANCE OPTIMIZATION: Database Configuration
        // ==========================================
        builder.Services.AddDbContext<ApplicationDBContext>(option => 
        {
            option.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    // Enable connection pooling (default is 128, increase for high traffic)
                    sqlOptions.MaxBatchSize(100); // Batch multiple commands
                    sqlOptions.CommandTimeout(60); // 30 second timeout
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            
            // Disable change tracking for read-only queries (use AsNoTracking explicitly)
            // This reduces memory usage and improves performance
           
                option.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            
        });

builder.Services.Configure<GeideaSettings>(builder.Configuration.GetSection("Geidea"));
builder.Services.Configure<TappySettings>(builder.Configuration.GetSection("Tappy"));
builder.Services.Configure<TamaraSettings>(builder.Configuration.GetSection("Tamara"));
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDBContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(option =>
{
	option.AccessDeniedPath = $"/Identity/Account/AccessDenied";
	option.LogoutPath = $"/Identity/Account/Logout";
	option.LoginPath = $"/Identity/Account/Login";

});

// Add Google Authentication
builder.Services.AddAuthentication().AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

builder.Services.AddRazorPages();

// Configure HSTS (HTTP Strict Transport Security) for production
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365); // 1 year
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender,EmailSender>();
builder.Services.AddScoped<IdealWeightNutrition.Services.INotificationService, IdealWeightNutrition.Services.NotificationService>();
builder.Services.AddScoped<IdealWeightNutrition.Services.IStockService, IdealWeightNutrition.Services.StockService>();

// Register TamaraHelper for dependency injection (optional - can still be instantiated directly)
builder.Services.AddScoped<TamaraHelper>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<TamaraSettings>>().Value;
    var logger = serviceProvider.GetService<ILogger<TamaraHelper>>();
    return new TamaraHelper(settings, logger);
});

// Register SharedResources for localization
builder.Services.AddSingleton<IdealWeightNutrition.SharedResources>();

// Add SignalR for real-time notifications
builder.Services.AddSignalR(); 

// Add Session support for guest cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(100);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

// Register Payment Verification Background Service
// This service runs every 5 minutes to verify pending payment orders
builder.Services.AddHostedService<IdealWeightNutrition.Services.PaymentVerificationBackgroundService>();

// Register Expiring Products Background Service
// This service runs daily at 8:00 AM to check for products expiring in next 10 days
builder.Services.AddHostedService<IdealWeightNutrition.Services.ExpiringProductsBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

    // HTTPS Redirect - Force HTTPS in production (prevent HTTP access)
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
        app.UseHsts(); // HTTP Strict Transport Security
    }

    // Global error handling - works in both Development and Production
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Customer/Home/Error");
    }
    
    // Global 404 and status code handling - works in ALL environments (including Development)
    // This catches all 404 errors and routes them to the Error action
    app.UseStatusCodePagesWithReExecute("/Customer/Home/Error", "?statusCode={0}");
 

// Configure Static Files with Aggressive Caching
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name.ToLower();
        var extension = System.IO.Path.GetExtension(path).ToLower();
        
        // Different cache durations based on file type
        if (extension == ".css" || extension == ".js")
        {
            // CSS/JS: 1 year (with versioning via asp-append-version)
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".webp" || extension == ".svg")
        {
            // Special handling for video banner poster - no cache
            if (path.Contains("video-banner-poster"))
            {
                // No cache for video banner poster to ensure fresh image
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
            else
            {
                // Other images: 1 year
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
            }
        }
        else if (extension == ".woff" || extension == ".woff2" || extension == ".ttf" || extension == ".eot")
        {
            // Fonts: 1 year
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
        else
        {
            // Other static files: 1 month
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
        }
        
        // Add ETag for cache validation
        var etag = ctx.File.Name + ctx.File.LastModified.Ticks.ToString();
        ctx.Context.Response.Headers.Append("ETag", $"\"{etag}\"");
    }
};

app.UseStaticFiles(staticFileOptions);

// Use Response Caching (must be before UseRouting)
app.UseResponseCaching();

// Configure Request Localization
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseSession();
app.UseAuthentication();                                                                    

app.UseAuthorization();

app.MapRazorPages();

// Map SignalR Hub
app.MapHub<IdealWeightNutrition.Hubs.NotificationHub>("/notificationHub");

// Custom route for product details with slug in path (SEO-friendly)
// This route must come BEFORE the default route to be matched first
app.MapControllerRoute(
    name: "productDetails",
    pattern: "Customer/Home/Details/{slug}",
    defaults: new { area = "Customer", controller = "Home", action = "Details" },
    constraints: new { slug = @"[^/]+" }); // Ensure slug doesn't contain slashes

 
app.MapControllerRoute(
    name: "tamaraWebhook",
    pattern: "customer/cart/TamaraWebhook",
    defaults: new { area = "Customer", controller = "Cart", action = "TamaraWebhook" });

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

