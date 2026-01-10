using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;

namespace IdealWeightNutrition.Services
{
    /// <summary>
    /// Background service that runs daily at a specific time to check for expiring products
    /// Sends email to admin with Excel attachment of products expiring in next 10 days
    /// </summary>
    public class ExpiringProductsBackgroundService : BackgroundService
    {
        private readonly ILogger<ExpiringProductsBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private TimeSpan _checkTime;
        private int _daysBeforeExpiry;
        private bool _enabled;

        public ExpiringProductsBackgroundService(
            ILogger<ExpiringProductsBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;

            // Read configuration
            _enabled = bool.Parse(_configuration["ExpiringProductsAlert:Enabled"] ?? "true");
            _daysBeforeExpiry = int.Parse(_configuration["ExpiringProductsAlert:DaysBeforeExpiry"] ?? "10");
            var checkTimeHour = int.Parse(_configuration["ExpiringProductsAlert:CheckTimeHour"] ?? "8");
            _checkTime = new TimeSpan(checkTimeHour, 0, 0);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Expiring Products Background Service is disabled in configuration");
                return;
            }

            _logger.LogInformation("Expiring Products Background Service started at {Time}. Will check daily at {CheckTime} for products expiring in next {Days} days", 
                DateTimeHelper.Now, _checkTime, _daysBeforeExpiry);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate next run time (today at checkTime or tomorrow at checkTime)
                    var now = DateTimeHelper.Now;
                    var nextRun = now.Date.Add(_checkTime);
                    
                    // If we've already passed today's check time, schedule for tomorrow
                    if (now.TimeOfDay > _checkTime)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    var delay = nextRun - now;
                    
                    _logger.LogInformation("Next expiring products check scheduled for {NextRun} (in {Hours} hours)", 
                        nextRun, delay.TotalHours);

                    // Wait until next run time
                    await Task.Delay(delay, stoppingToken);

                    // Execute the job
                    _logger.LogInformation("Starting expiring products check at {Time}", DateTimeHelper.Now);
                    await CheckAndNotifyExpiringProductsAsync(stoppingToken);
                    _logger.LogInformation("Completed expiring products check at {Time}", DateTimeHelper.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking expiring products at {Time}", DateTimeHelper.Now);
                    
                    // Wait a bit before retrying to avoid rapid failure loops
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Expiring Products Background Service stopped at {Time}", DateTimeHelper.Now);
        }

        private async Task CheckAndNotifyExpiringProductsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            // Calculate date range (today + configured days)
            var today = DateTimeHelper.Now.Date;
            var expiryThresholdDate = today.AddDays(_daysBeforeExpiry);

            _logger.LogInformation("Checking for products expiring between {Today} and {ThresholdDate} ({Days} days)", 
                today, expiryThresholdDate, _daysBeforeExpiry);

            var expiringProducts = new List<ExpiringProductInfo>();

            // Get all products (including those with variants)
            var products = await db.Products
                .Include(p => p.ProductVariants)
                .Include(p => p.categry)
                .Include(p => p.Brand)
                .Where(p => !p.IsDeleted)
                .AsNoTracking()
                .ToListAsync(stoppingToken);

            foreach (var product in products)
            {
                // Check if product has variants
                if (product.HasVariants && product.ProductVariants != null && product.ProductVariants.Any())
                {
                    // Check each variant's expiry date
                    foreach (var variant in product.ProductVariants.Where(v => !v.IsDeleted))
                    {
                        if (variant.ExpiryDate.HasValue && 
                            variant.ExpiryDate.Value.Date >= today && 
                            variant.ExpiryDate.Value.Date <= expiryThresholdDate)
                        {
                            expiringProducts.Add(new ExpiringProductInfo
                            {
                                ProductId = product.Id,
                                ProductTitle = product.Title,
                                ProductTitleAr = product.TitleAr,
                                Category = product.categry?.Name ?? "N/A",
                                Brand = product.Brand?.Name ?? "N/A",
                                SKU = variant.SKU ?? $"VAR-{variant.Id}",
                                VariantInfo = GetVariantName(variant),
                                StockQuantity = variant.StockQuantity,
                                ExpiryDate = variant.ExpiryDate.Value,
                                DaysUntilExpiry = (variant.ExpiryDate.Value.Date - today).Days,
                                IsVariant = true,
                                VariantId = variant.Id
                            });
                        }
                    }
                }
                else
                {
                    // Simple product - check main product expiry date
                    if (product.ExpiryDate.HasValue && 
                        product.ExpiryDate.Value.Date >= today && 
                        product.ExpiryDate.Value.Date <= expiryThresholdDate)
                    {
                        expiringProducts.Add(new ExpiringProductInfo
                        {
                            ProductId = product.Id,
                            ProductTitle = product.Title,
                            ProductTitleAr = product.TitleAr,
                            Category = product.categry?.Name ?? "N/A",
                            Brand = product.Brand?.Name ?? "N/A",
                            SKU = product.ISBN ?? $"PRD-{product.Id}",
                            VariantInfo = "N/A",
                            StockQuantity = product.StockQuantity,
                            ExpiryDate = product.ExpiryDate.Value,
                            DaysUntilExpiry = (product.ExpiryDate.Value.Date - today).Days,
                            IsVariant = false,
                            VariantId = null
                        });
                    }
                }
            }

            if (!expiringProducts.Any())
            {
                _logger.LogInformation("No products expiring in the next {Days} days. Email not sent.", _daysBeforeExpiry);
                return;
            }

            _logger.LogInformation("Found {Count} expiring products/variants", expiringProducts.Count);

            // Sort by days until expiry (most urgent first)
            expiringProducts = expiringProducts.OrderBy(p => p.DaysUntilExpiry).ToList();

            try
            {
                // Generate Excel file
                var excelBytes = GenerateExcelFile(expiringProducts);

                // Get admin email from configuration
                var adminEmail = _configuration["ExpiringProductsAlert:AdminEmail"];
                if (string.IsNullOrEmpty(adminEmail))
                {
                    _logger.LogWarning("ExpiringProductsAlert:AdminEmail not configured in appsettings. Cannot send expiring products notification.");
                    return;
                }

                // Send email with attachment
                var subject = $"⚠️ Expiring Products Alert - {expiringProducts.Count} Products Expiring in Next {_daysBeforeExpiry} Days";
                var body = GenerateEmailBody(expiringProducts);

                await SendEmailWithAttachmentAsync(emailSender, adminEmail, subject, body, excelBytes, 
                    $"Expiring-Products-{DateTimeHelper.Now:yyyy-MM-dd}.xlsx");

                _logger.LogInformation("Expiring products notification email sent successfully to {AdminEmail} with {Count} products", 
                    adminEmail, expiringProducts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating or sending expiring products notification");
            }
        }

        private string GetVariantName(ProductVariant variant)
        {
            if (variant.VariantOptionValues == null || !variant.VariantOptionValues.Any())
                return "Default Variant";

            // This would need to be populated if VariantOptionValues are loaded
            // For now, return SKU or ID-based name
            return !string.IsNullOrEmpty(variant.SKU) ? variant.SKU : $"Variant #{variant.Id}";
        }

        private byte[] GenerateExcelFile(List<ExpiringProductInfo> expiringProducts)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Expiring Products");

            // Headers
            worksheet.Cells[1, 1].Value = "Product ID";
            worksheet.Cells[1, 2].Value = "Product Title (EN)";
            worksheet.Cells[1, 3].Value = "Product Title (AR)";
            worksheet.Cells[1, 4].Value = "Category";
            worksheet.Cells[1, 5].Value = "Brand";
            worksheet.Cells[1, 6].Value = "SKU";
            worksheet.Cells[1, 7].Value = "Variant Info";
            worksheet.Cells[1, 8].Value = "Stock Quantity";
            worksheet.Cells[1, 9].Value = "Expiry Date";
            worksheet.Cells[1, 10].Value = "Days Until Expiry";
            worksheet.Cells[1, 11].Value = "Type";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            }

            // Data
            int row = 2;
            foreach (var product in expiringProducts)
            {
                worksheet.Cells[row, 1].Value = product.ProductId;
                worksheet.Cells[row, 2].Value = product.ProductTitle;
                worksheet.Cells[row, 3].Value = product.ProductTitleAr;
                worksheet.Cells[row, 4].Value = product.Category;
                worksheet.Cells[row, 5].Value = product.Brand;
                worksheet.Cells[row, 6].Value = product.SKU;
                worksheet.Cells[row, 7].Value = product.VariantInfo;
                worksheet.Cells[row, 8].Value = product.StockQuantity;
                worksheet.Cells[row, 9].Value = product.ExpiryDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 10].Value = product.DaysUntilExpiry;
                worksheet.Cells[row, 11].Value = product.IsVariant ? "Variant" : "Simple Product";

                // Color code by urgency
                if (product.DaysUntilExpiry <= 3)
                {
                    // Red for 0-3 days
                    using var range = worksheet.Cells[row, 1, row, 11];
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 199, 206));
                }
                else if (product.DaysUntilExpiry <= 7)
                {
                    // Yellow for 4-7 days
                    using var range = worksheet.Cells[row, 1, row, 11];
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 235, 156));
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }

        private string GenerateEmailBody(List<ExpiringProductInfo> expiringProducts)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            sb.AppendLine("<h2 style='color: #dc2626;'>⚠️ Expiring Products Alert</h2>");
            sb.AppendLine($"<p>The following <strong>{expiringProducts.Count}</strong> products/variants are expiring in the next {_daysBeforeExpiry} days:</p>");

            // Group by urgency
            var critical = expiringProducts.Where(p => p.DaysUntilExpiry <= 3).ToList();
            var warning = expiringProducts.Where(p => p.DaysUntilExpiry > 3 && p.DaysUntilExpiry <= 7).ToList();
            var notice = expiringProducts.Where(p => p.DaysUntilExpiry > 7).ToList();

            if (critical.Any())
            {
                sb.AppendLine("<h3 style='color: #dc2626;'>🔴 Critical (0-3 days)</h3>");
                sb.AppendLine("<ul>");
                foreach (var product in critical.Take(10))
                {
                    sb.AppendLine($"<li><strong>{product.ProductTitle}</strong> - Expires in <strong>{product.DaysUntilExpiry}</strong> days ({product.ExpiryDate:yyyy-MM-dd}) - Stock: {product.StockQuantity}</li>");
                }
                if (critical.Count > 10)
                {
                    sb.AppendLine($"<li><em>... and {critical.Count - 10} more critical items</em></li>");
                }
                sb.AppendLine("</ul>");
            }

            if (warning.Any())
            {
                sb.AppendLine("<h3 style='color: #f59e0b;'>🟡 Warning (4-7 days)</h3>");
                sb.AppendLine("<ul>");
                foreach (var product in warning.Take(10))
                {
                    sb.AppendLine($"<li><strong>{product.ProductTitle}</strong> - Expires in <strong>{product.DaysUntilExpiry}</strong> days ({product.ExpiryDate:yyyy-MM-dd}) - Stock: {product.StockQuantity}</li>");
                }
                if (warning.Count > 10)
                {
                    sb.AppendLine($"<li><em>... and {warning.Count - 10} more warning items</em></li>");
                }
                sb.AppendLine("</ul>");
            }

            if (notice.Any())
            {
                sb.AppendLine("<h3 style='color: #3b82f6;'>🔵 Notice (8-10 days)</h3>");
                sb.AppendLine($"<p>{notice.Count} products expiring in 8-10 days. See attached Excel file for details.</p>");
            }

            sb.AppendLine("<hr/>");
            sb.AppendLine("<p><strong>Please review the attached Excel file for the complete list.</strong></p>");
            sb.AppendLine("<p>Take action to:</p>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Remove expiring products from inventory</li>");
            sb.AppendLine("<li>Apply discounts to sell before expiry</li>");
            sb.AppendLine("<li>Mark products as unavailable</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine($"<p style='color: #6b7280; font-size: 0.9em;'>Generated at: {DateTimeHelper.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        private async Task SendEmailWithAttachmentAsync(
            IEmailSender emailSender,
            string toEmail,
            string subject,
            string htmlBody,
            byte[] attachmentBytes,
            string attachmentFileName)
        {
            // Check if EmailSender is the custom EmailSender class with attachment support
            if (emailSender is EmailSender customEmailSender)
            {
                await customEmailSender.SendEmailWithAttachmentAsync(
                    toEmail,
                    subject,
                    htmlBody,
                    attachmentBytes,
                    attachmentFileName);
            }
            else
            {
                // Fallback: Save to file system if email sending with attachment not supported
                var attachmentPath = Path.Combine(Directory.GetCurrentDirectory(), "logs", attachmentFileName);
                await File.WriteAllBytesAsync(attachmentPath, attachmentBytes);
                
                _logger.LogWarning("Email attachment not supported. Excel file saved to: {Path}", attachmentPath);
                
                // Send email without attachment
                await emailSender.SendEmailAsync(toEmail, subject, 
                    htmlBody + $"<p><strong>Note: Excel file saved to server at: {attachmentPath}</strong></p>");
            }
        }
    }

    /// <summary>
    /// Data transfer object for expiring product information
    /// </summary>
    public class ExpiringProductInfo
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; } = string.Empty;
        public string ProductTitleAr { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string VariantInfo { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysUntilExpiry { get; set; }
        public bool IsVariant { get; set; }
        public int? VariantId { get; set; }
    }
}

