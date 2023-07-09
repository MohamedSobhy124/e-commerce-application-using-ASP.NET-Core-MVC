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
var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
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

builder.Services.AddRazorPages();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender,EmailSender>(); 



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
app.UseRouting();
app.UseAuthentication();                                                                    

app.UseAuthorization();

builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options => {
//	options.IdleTimeout = TimeSpan.FromMinutes(100);
//	options.Cookie.HttpOnly = true;
//	options.Cookie.IsEssential = true;
//});
app.MapRazorPages();
//app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

