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
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
builder.Logging.AddFileLogger(logDirectory);

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 52428800; // 50MB for video uploads
            options.ValueLengthLimit = 52428800; // 50MB
            options.MultipartHeadersLengthLimit = 52428800; // 50MB
        });

        builder.Services.Configure<IISServerOptions>(options =>
        {
            options.MaxRequestBodySize = 52428800; // 50MB
        });

        builder.Services.AddControllersWithViews(options =>
        {
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
            
            options.ModelBinderProviders.Insert(0, new IdealWeightNutrition.ModelBinders.EncryptedIdModelBinderProvider());
        })
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();
        
        builder.Services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 64 * 1024; // 64 KB
            options.UseCaseSensitivePaths = false;
        });
        
        builder.Services.AddMemoryCache();
        
        builder.Services.AddLocalization();
        
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("ar"), // Arabic (default)
                new CultureInfo("en")  // English
            };

            options.DefaultRequestCulture = new RequestCulture(culture: "ar", uiCulture: "ar");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
            
            // Clear default providers and add in order of priority
            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider()); // URL ?culture=en
            options.RequestCultureProviders.Add(new CookieRequestCultureProvider()); // Cookie fallback
        });
        
        builder.Services.AddDbContext<ApplicationDBContext>(option => 
        {
            option.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.MaxBatchSize(100); // Batch multiple commands
                    sqlOptions.CommandTimeout(60); // 30 second timeout
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                });
            
                option.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            
        });

builder.Services.Configure<GeideaSettings>(builder.Configuration.GetSection("Geidea"));
builder.Services.Configure<TappySettings>(builder.Configuration.GetSection("Tappy"));
builder.Services.Configure<TamaraSettings>(builder.Configuration.GetSection("Tamara"));
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDBContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(option =>
{
	option.AccessDeniedPath = $"/Identity/Account/AccessDenied";
	option.LogoutPath = $"/Identity/Account/Logout";
	option.LoginPath = $"/Identity/Account/Login";
	option.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
	option.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthentication().AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    // Configure cookie settings to prevent correlation failures
    googleOptions.CorrelationCookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
    googleOptions.CorrelationCookie.HttpOnly = true;
    googleOptions.CorrelationCookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    
    // Save tokens for later use (if needed)
    googleOptions.SaveTokens = true;
});

builder.Services.AddRazorPages();
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365); // 1 year
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddScoped<IEmailSender,EmailSender>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<IdealWeightNutrition.Services.INotificationService, IdealWeightNutrition.Services.NotificationService>();
builder.Services.AddScoped<IdealWeightNutrition.Services.IStockService, IdealWeightNutrition.Services.StockService>();

builder.Services.AddScoped<TamaraHelper>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<TamaraSettings>>().Value;
    var logger = serviceProvider.GetService<ILogger<TamaraHelper>>();
    return new TamaraHelper(settings, logger);
});

builder.Services.AddSingleton<IdealWeightNutrition.SharedResources>();

builder.Services.AddSignalR(); 

// Configure Data Protection to persist keys (prevents correlation failures on app restart)
// In production, consider using a shared key storage (Azure Key Vault, Redis, etc.)
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys");
if (!Directory.Exists(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("IdealWeightNutrition")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(100);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
	options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
	options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddHostedService<IdealWeightNutrition.Services.PaymentVerificationBackgroundService>();

builder.Services.AddHostedService<IdealWeightNutrition.Services.ExpiringProductsBackgroundService>();

var app = builder.Build();

    // Run database initializer (roles, admin user, blog seed)
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
        app.UseHsts(); // HTTP Strict Transport Security
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Customer/Home/Error");
    }
    app.UseStatusCodePagesWithReExecute("/Customer/Home/Error", "?statusCode={0}");
 

var staticFileOptions = new StaticFileOptions
{
    ServeUnknownFileTypes = true, // Enable serving files without extensions
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name.ToLower();
        var extension = System.IO.Path.GetExtension(path).ToLower();
        var filePath = ctx.File.PhysicalPath?.ToLower() ?? "";
        
        // Handle .well-known folder files (e.g., Apple Pay domain association)
        if (filePath.Contains("\\.well-known\\") || filePath.Contains("/.well-known/"))
        {
            // Set content type to text/plain for domain association files
            if (path.Contains("apple-developer-merchantid-domain-association"))
            {
                ctx.Context.Response.ContentType = "text/plain";
            }
            // Minimal caching for .well-known files to ensure Apple can verify them
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600"); // 1 hour
            var etagGidea = ctx.File.Name + ctx.File.LastModified.Ticks.ToString();
            ctx.Context.Response.Headers.Append("ETag", $"\"{etagGidea}\"");
            return;
        }
        
        if (extension == ".css" || extension == ".js")
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
        else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".webp" || extension == ".svg")
        {
            if (path.Contains("video-banner-poster"))
            {
                // No cache for video banner poster to ensure fresh image
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
            else
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
            }
        }
        else if (extension == ".woff" || extension == ".woff2" || extension == ".ttf" || extension == ".eot")
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000,immutable");
        }
        else
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
        }
        var etag = ctx.File.Name + ctx.File.LastModified.Ticks.ToString();
        ctx.Context.Response.Headers.Append("ETag", $"\"{etag}\"");
    }
};

app.UseStaticFiles(staticFileOptions);

app.UseResponseCaching();
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseSession();
app.UseAuthentication();                                                                    

app.UseAuthorization();

app.MapRazorPages();

app.MapHub<IdealWeightNutrition.Hubs.NotificationHub>("/notificationHub");
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

