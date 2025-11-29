using Microsoft.EntityFrameworkCore;
using BulkyBook.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


namespace BulkyBook.DataAccess.Data
{
    public class ApplicationDBContext:IdentityDbContext<IdentityUser>
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options):base(options)
        {
                
        }
        public DbSet<Categry> Categries { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ApplicationUser> applicationUsers { get; set; }
        public DbSet<Company> Companys { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> orderHeaders { get; set; }
        public object OrderHeaders { get; internal set; }
        public DbSet<OrderDetail> orderDetails { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<FlashSale> FlashSales { get; set; }
        public DbSet<FlashSaleItem> FlashSaleItems { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<ServiceOffer> ServiceOffers { get; set; }
        public DbSet<ServicePurchase> ServicePurchases { get; set; }
        public DbSet<ServiceSubscription> ServiceSubscriptions { get; set; }
        public DbSet<ServiceImage> ServiceImages { get; set; }
        public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<ProductOptionValue> ProductOptionValues { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantOptionValue> ProductVariantOptionValues { get; set; }
        public DbSet<StockNotification> StockNotifications { get; set; }



		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

			base.OnModelCreating(modelBuilder);

			// ==========================================
			// PERFORMANCE OPTIMIZATION: Database Indexes
			// ==========================================
			
			// Index on IsDeleted for all BaseEntity tables (CRITICAL for performance)
			modelBuilder.Entity<Product>().HasIndex(p => p.IsDeleted);
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.CategryId }); // Composite index
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.StockQuantity }); // For stock queries
			
			modelBuilder.Entity<Categry>().HasIndex(c => c.IsDeleted);
			modelBuilder.Entity<ProductOption>().HasIndex(o => new { o.IsDeleted, o.ProductId });
			modelBuilder.Entity<ProductOptionValue>().HasIndex(ov => new { ov.IsDeleted, ov.ProductOptionId });
			modelBuilder.Entity<ProductVariant>().HasIndex(v => new { v.IsDeleted, v.ProductId });
			modelBuilder.Entity<FlashSale>().HasIndex(f => new { f.IsDeleted, f.IsActive, f.StartDate, f.EndDate });
			modelBuilder.Entity<FlashSaleItem>().HasIndex(i => new { i.IsDeleted, i.FlashSaleId, i.ProductId });
			
			// Index on foreign keys for faster joins
			modelBuilder.Entity<Product>().HasIndex(p => p.CategryId);
			modelBuilder.Entity<ShoppingCart>().HasIndex(c => new { c.ApplicationUserId, c.ProductId, c.ProductVariantId });
			modelBuilder.Entity<OrderDetail>().HasIndex(od => new { od.OrderHeaderId, od.ProductId });
			modelBuilder.Entity<ProductImage>().HasIndex(pi => pi.ProductId);
			modelBuilder.Entity<Review>().HasIndex(r => new { r.ProductId, r.UserId });
			modelBuilder.Entity<Wishlist>().HasIndex(w => new { w.ApplicationUserId, w.ProductId });
			modelBuilder.Entity<ProductVariantOptionValue>().HasIndex(vov => new { vov.ProductVariantId, vov.ProductOptionValueId });
			
			// Index on frequently queried fields
			modelBuilder.Entity<Product>().HasIndex(p => p.StockQuantity);
			modelBuilder.Entity<Product>().HasIndex(p => p.Price); // For price sorting and filtering
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.Price }); // Composite for price filtering
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.StockQuantity, p.MinimumStockAlert }); // For low stock queries
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.CategryId, p.StockQuantity }); // For category + stock queries
			
			modelBuilder.Entity<ProductVariant>().HasIndex(v => v.StockQuantity);
			modelBuilder.Entity<ProductVariant>().HasIndex(v => new { v.IsDeleted, v.ProductId, v.StockQuantity }); // Composite for variant queries
			
			modelBuilder.Entity<FlashSale>().HasIndex(f => new { f.IsActive, f.StartDate, f.EndDate });
			modelBuilder.Entity<FlashSaleItem>().HasIndex(i => new { i.IsDeleted, i.FlashSaleQuantity }); // For flash sale item filtering
			
			// Reviews indexes - CRITICAL for performance
			modelBuilder.Entity<Review>().HasIndex(r => new { r.ProductId, r.IsApproved }); // Most common query pattern
			modelBuilder.Entity<Review>().HasIndex(r => r.IsApproved); // For filtering approved reviews
			modelBuilder.Entity<Review>().HasIndex(r => new { r.ProductId, r.IsApproved, r.CreatedAt }); // For ordered review queries
			modelBuilder.Entity<Review>().HasIndex(r => r.UserId); // For user review queries
			
			// OrderHeader indexes - CRITICAL for order queries
			modelBuilder.Entity<OrderHeader>().HasIndex(o => o.ApplicationUserId); // For user orders
			modelBuilder.Entity<OrderHeader>().HasIndex(o => new { o.ApplicationUserId, o.OrderStatus }); // Composite for user + status
			modelBuilder.Entity<OrderHeader>().HasIndex(o => o.OrderStatus); // For status filtering
			modelBuilder.Entity<OrderHeader>().HasIndex(o => o.Email); // For guest order lookup
			modelBuilder.Entity<OrderHeader>().HasIndex(o => new { o.Email, o.OrderStatus }); // For guest + status
			modelBuilder.Entity<OrderHeader>().HasIndex(o => o.OrderDate); // For date sorting
			modelBuilder.Entity<OrderHeader>().HasIndex(o => new { o.ApplicationUserId, o.OrderDate }); // For user orders by date
			
			// OrderDetail indexes - CRITICAL for order detail queries
			modelBuilder.Entity<OrderDetail>().HasIndex(od => od.ProductId); // For product order history
			modelBuilder.Entity<OrderDetail>().HasIndex(od => new { od.ProductId, od.OrderHeaderId }); // Composite (already exists but ensure)
			modelBuilder.Entity<OrderDetail>().HasIndex(od => od.ProductVariantId); // For variant orders
			
			// ProductImage indexes
			modelBuilder.Entity<ProductImage>().HasIndex(pi => new { pi.ProductId, pi.DisplayOrder }); // For ordered image queries
			modelBuilder.Entity<ProductImage>().HasIndex(pi => new { pi.ProductId, pi.ImageInfo }); // For filtering by ImageInfo
			
			// Notification indexes
			modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead }); // Most common query
			modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }); // For ordered notifications
			
			// ShoppingCart indexes (enhance existing)
			modelBuilder.Entity<ShoppingCart>().HasIndex(c => c.ApplicationUserId); // Single column for user cart
			
			// Wishlist indexes (enhance existing)
			modelBuilder.Entity<Wishlist>().HasIndex(w => w.ApplicationUserId); // Single column for user wishlist
			
			// PromoCode indexes
			modelBuilder.Entity<PromoCode>().HasIndex(pc => new { pc.Code, pc.IsActive }); // For code lookup
			modelBuilder.Entity<PromoCodeUsage>().HasIndex(pcu => pcu.OrderId); // For order promo lookup
			modelBuilder.Entity<PromoCodeUsage>().HasIndex(pcu => new { pcu.PromoCodeId, pcu.UserId }); // For usage tracking
			
			// StockNotification indexes
			modelBuilder.Entity<StockNotification>().HasIndex(sn => new { sn.ProductId, sn.IsNotified }); // For product notifications
			modelBuilder.Entity<StockNotification>().HasIndex(sn => sn.ApplicationUserId); // For user notifications
			
			// ServiceSubscription indexes
			modelBuilder.Entity<ServiceSubscription>().HasIndex(ss => ss.IsActive); // For active filtering
			
			// NewsletterSubscription indexes
			modelBuilder.Entity<NewsletterSubscription>().HasIndex(ns => ns.Email); // For email lookup
			modelBuilder.Entity<NewsletterSubscription>().HasIndex(ns => new { ns.Email, ns.IsActive }); // For active subscriptions
			
			// ProductOptionValue indexes (enhance existing)
			modelBuilder.Entity<ProductOptionValue>().HasIndex(ov => new { ov.IsDeleted, ov.DisplayOrder }); // For ordered option values
			
			// ServicePurchase indexes
			modelBuilder.Entity<ServicePurchase>().HasIndex(sp => sp.ApplicationUserId); // For user purchases
			modelBuilder.Entity<ServicePurchase>().HasIndex(sp => new { sp.ApplicationUserId, sp.ServiceSubscriptionId }); // Composite
			
			// ServiceImage indexes
			modelBuilder.Entity<ServiceImage>().HasIndex(si => si.ServiceSubscriptionId); // For service images
			
			// Additional performance indexes for common query patterns
			// Note: Full-text search indexes require special SQL Server setup, but these help with exact/prefix matches
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.Id }); // For newest/oldest sorting
			modelBuilder.Entity<Product>().HasIndex(p => new { p.IsDeleted, p.StockQuantity, p.Price }); // For stock + price queries
			
			// FlashSaleItem additional indexes
			modelBuilder.Entity<FlashSaleItem>().HasIndex(i => i.ProductId); // Single column for product lookup
			
			// ProductVariantOptionValue indexes (enhance existing)
			modelBuilder.Entity<ProductVariantOptionValue>().HasIndex(vov => vov.ProductOptionValueId); // Reverse lookup

			//modelBuilder.Entity<Company>().HasData(
			//	new Company
			//	{
			//		Id = 1,
			//		Name = "Tech Solution",
			//		StreetAddress = "123 Tech St",
			//		City = "Tech City",
			//		PostalCode = "12121",
			//		State = "IL",
			//		PhoneNumber = "6669990000"
			//	},
			//	new Company
			//	{
			//		Id = 2,
			//		Name = "Vivid Books",
			//		StreetAddress = "999 Vid St",
			//		City = "Vid City",
			//		PostalCode = "66666",
			//		State = "IL",
			//		PhoneNumber = "7779990000"
			//	},
			//	new Company
			//	{
			//		Id = 3,
			//		Name = "Readers Club",
			//		StreetAddress = "999 Main St",
			//		City = "Lala land",
			//		PostalCode = "99999",
			//		State = "NY",
			//		PhoneNumber = "1113335555"
			//	}
			//	);


			//modelBuilder.Entity<Product>().HasData(
			//	new Product
			//	{
			//		Id = 1,
			//		Title = "Fortune of Time",
			//		Author = "Billy Spark",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "SWD9999001",
			//		ListPrice = 99,
			//		Price = 90,
			//		Price50 = 85,
			//		Price100 = 80,
			//		CategryId = 1
			//	},
			//	new Product
			//	{
			//		Id = 2,
			//		Title = "Dark Skies",
			//		Author = "Nancy Hoover",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "CAW777777701",
			//		ListPrice = 40,
			//		Price = 30,
			//		Price50 = 25,
			//		Price100 = 20,
			//		CategryId = 1
			//	},
			//	new Product
			//	{
			//		Id = 3,
			//		Title = "Vanish in the Sunset",
			//		Author = "Julian Button",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "RITO5555501",
			//		ListPrice = 55,
			//		Price = 50,
			//		Price50 = 40,
			//		Price100 = 35,
			//		CategryId = 1
			//	},
			//	new Product
			//	{
			//		Id = 4,
			//		Title = "Cotton Candy",
			//		Author = "Abby Muscles",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "WS3333333301",
			//		ListPrice = 70,
			//		Price = 65,
			//		Price50 = 60,
			//		Price100 = 55,
			//		CategryId = 2
			//	},
			//	new Product
			//	{
			//		Id = 5,
			//		Title = "Rock in the Ocean",
			//		Author = "Ron Parker",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "SOTJ1111111101",
			//		ListPrice = 30,
			//		Price = 27,
			//		Price50 = 25,
			//		Price100 = 20,
			//		CategryId = 2
			//	},
			//	new Product
			//	{
			//		Id = 6,
			//		Title = "Leaves and Wonders",
			//		Author = "Laura Phantom",
			//		Description = "Praesent vitae sodales libero. Praesent molestie orci augue, vitae euismod velit sollicitudin ac. Praesent vestibulum facilisis nibh ut ultricies.\r\n\r\nNunc malesuada viverra ipsum sit amet tincidunt. ",
			//		ISBN = "FOT000000001",
			//		ListPrice = 25,
			//		Price = 23,
			//		Price50 = 22,
			//		Price100 = 20,
			//		CategryId = 3
			//	}
				//);
		}
	}
}
