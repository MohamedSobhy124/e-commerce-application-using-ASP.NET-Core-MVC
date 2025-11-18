using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using BulkyBook.Utility;
using Stripe;
using BulkyBook.Models;
using BulkyBook.DataAccess.DbInitializer;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization();
        
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
        
        builder.Services.AddDbContext<ApplicationDBContext>(option => 
        option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

IServiceCollection serviceCollection = builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));


builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

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
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender,EmailSender>();
builder.Services.AddScoped<BulkyBook.Services.INotificationService, BulkyBook.Services.NotificationService>();

// Register SharedResources for localization
builder.Services.AddSingleton<BulkyBook.SharedResources>();

// Add SignalR for real-time notifications
builder.Services.AddSignalR(); 

// Add Session support for guest cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
	options.IdleTimeout = TimeSpan.FromMinutes(100);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.

    app.UseExceptionHandler("/Home/Error");
 

app.UseStaticFiles();

// Configure Request Localization
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
app.UseRouting();
app.UseSession();
app.UseAuthentication();                                                                    

app.UseAuthorization();

app.MapRazorPages();

// Map SignalR Hub
app.MapHub<BulkyBook.Hubs.NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

