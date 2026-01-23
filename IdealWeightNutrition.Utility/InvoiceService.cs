using IdealWeightNutrition.Models;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdealWeightNutrition.Utility
{
    public class InvoiceService
    {
        private readonly IConfiguration _configuration;

        public InvoiceService(IConfiguration configuration)
        {
            _configuration = configuration;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerateInvoicePdf(OrderHeader orderHeader, List<Models.OrderDetail> orderDetails, ApplicationUser? customer)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Element(ComposeHeader);

                    page.Content()
                        .Element(container => ComposeContent(container, orderHeader, orderDetails, customer));

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container)
        {
            var businessName = _configuration["SiteSettings:Business:Name"] ?? "Ideal Weight Nutrition";
            var businessEmail = _configuration["SiteSettings:Business:Email"] ?? "info@idealweightnutrition.ae";
            var businessPhone = _configuration["SiteSettings:Business:Phone"] ?? "+971 52 738 3841";
            var streetAddress = _configuration["SiteSettings:Business:Address:StreetAddress"] ?? "";
            var city = _configuration["SiteSettings:Business:Address:City"] ?? "";
            var country = _configuration["SiteSettings:Business:Address:CountryName"] ?? "";
            var vatNumber = _configuration["SiteSettings:Business:VATRegistrationNumber"] ?? "";

            // Build address
            var addressParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(streetAddress)) addressParts.Add(streetAddress);
            if (!string.IsNullOrWhiteSpace(city)) addressParts.Add(city);
            if (!string.IsNullOrWhiteSpace(country)) addressParts.Add(country);

            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item()
                            .Text(businessName)
                            .FontSize(22)
                            .Bold()
                            .FontColor(Colors.Blue.Darken3);

                        if (addressParts.Any())
                        {
                            left.Item()
                                .PaddingTop(4)
                                .Text(string.Join(", ", addressParts))
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        }

                        left.Item()
                            .PaddingTop(6)
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x =>
                                    x.FontSize(9).FontColor(Colors.Grey.Darken1));

                                text.Span("Email: ").SemiBold();
                                text.Span(businessEmail);
                            });

                        left.Item()
                            .Text(text =>
                            {
                                text.DefaultTextStyle(x =>
                                    x.FontSize(9).FontColor(Colors.Grey.Darken1));

                                text.Span("Phone: ").SemiBold();
                                text.Span(businessPhone);
                            });
                    });

                    row.ConstantItem(200).Column(right =>
                    {
                        right.Item()
                            .AlignRight()
                            .Text("Tax Invoice")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Grey.Darken3);

                        if (!string.IsNullOrWhiteSpace(vatNumber))
                        {
                            right.Item()
                                .PaddingTop(6)
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.DefaultTextStyle(x =>
                                        x.FontSize(9).FontColor(Colors.Grey.Darken2));

                                    text.Span("TRN: ").SemiBold();
                                    text.Span(vatNumber);
                                });
                        }
                    });
                });

                column.Item()
                    .PaddingVertical(10)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten2);
            });
        }

        private void ComposeContent(IContainer container,OrderHeader orderHeader, List<Models.OrderDetail> orderDetails,ApplicationUser? customer)
        {
            container.Column(column =>
            {
                column.Spacing(25);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item()
                            .Text("Bill To")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);

                        left.Item()
                            .PaddingTop(4)
                            .Text(customer?.Name ?? orderHeader.Name)
                            .FontSize(10)
                            .Bold();

                        if (!string.IsNullOrWhiteSpace(orderHeader.StreetAddress))
                            left.Item().Text(orderHeader.StreetAddress).FontSize(9);

                        if (!string.IsNullOrWhiteSpace(orderHeader.City))
                        {
                            left.Item()
                                .Text($"{orderHeader.City}, {orderHeader.State}")
                                .FontSize(9);
                        }

                        if (!string.IsNullOrWhiteSpace(orderHeader.PhoneNumber))
                        {
                            left.Item().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Phone: ").SemiBold();
                                text.Span(orderHeader.PhoneNumber);
                            });
                        }

                        var email = customer?.Email ?? orderHeader.Email;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            left.Item().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Email: ").SemiBold();
                                text.Span(email);
                            });
                        }
                    });

                    row.ConstantItem(220).Column(right =>
                    {
                        right.Item()
                            .AlignRight()
                            .Text("Invoice Details")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);

                        right.Item().PaddingTop(4).AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Invoice No: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text($"INV-{orderHeader.Id:D6}").FontSize(9).Bold();
                        });

                        right.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Order Date: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(orderHeader.OrderDate.ToString("dd MMM yyyy")).FontSize(9);
                        });

                        right.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Payment Status: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(orderHeader.PaymentStatus ?? "Pending").FontSize(9).Bold();
                        });
                    });
                });

                column.Item()
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten2);
                column.Item()
                    .Element(c => ComposeTable(c, orderHeader, orderDetails));

                column.Item()
                    .Element(c => ComposeSummary(c, orderHeader));
            });
        }

        private void ComposeTable(IContainer container, OrderHeader orderHeader, List<OrderDetail> orderDetails)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {  
                   
                    columns.RelativeColumn(3);      // Description
                    columns.ConstantColumn(45);   // Qty
                    columns.ConstantColumn(70);    // Price excl VAT
                    columns.ConstantColumn(45);   // VAT rate
                    columns.ConstantColumn(70);    // VAT amount
                    columns.ConstantColumn(75);    // Price incl VAT
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("Quantity").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("Price excl VAT (AED)").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("VAT rate").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignRight().Text("VAT amount (AED)").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignRight().Text("Price incl VAT (AED)").Bold().FontSize(8);
                           
                });

                // Items
                foreach (var item in orderDetails)
                {
                    var vatRate = 0.05m;
                    var itemName = string.Empty;

                    if (item.IsFromComboOffer && item.ComboOfferId.HasValue)
                    {
                         itemName = item.ComboOffer?.Name ?? "Combo Offer";
                    }
                    else if (item.ProductVariantId.HasValue && item.ProductVariant != null)
                    {
                        itemName= item.Product.Title+" , "+item?.ProductVariant?.VariantName ?? item.Product.Title;
                    }
                    else
                    {
                        itemName = item.Product.Title;
                    }
                     

                    var priceInclVat = (decimal)item.Price* item.Count;
                    var vatAmountPerUnit = priceInclVat * (vatRate / (1 + vatRate));
                    var priceExclVat = priceInclVat- vatAmountPerUnit;

                    

                    table.Cell().Element(CellStyle).Text(itemName).FontSize(7);
                    table.Cell().Element(CellStyle).AlignCenter().Text(item.Count.ToString()).FontSize(7);
                    table.Cell().Element(CellStyle).AlignCenter().Text($"AED {priceExclVat:N2}").FontSize(7);
                    table.Cell().Element(CellStyle).AlignCenter().Text($"{vatRate * 100:F2}%").FontSize(9);
                    table.Cell().Element(CellStyle).AlignCenter().Text($"AED {vatAmountPerUnit:N2}").FontSize(7);
                    table.Cell().Element(CellStyle).AlignRight().Text($"AED {priceInclVat:N2}").FontSize(7); 
                }
            });
        }

        private IContainer CellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(8)
                .PaddingHorizontal(5);
        }

        private void ComposeSummary(IContainer container, OrderHeader orderHeader)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                var vatRate = 0.05m;

                var subtotalInclVat = (orderHeader.OrderSubtotal ?? orderHeader.OrderTotal);
                var discount = orderHeader?.DiscountAmount ?? 0;

                var taxableAmountInclVat = subtotalInclVat -discount;

                var vatAmount = (decimal)taxableAmountInclVat * vatRate / (1 + vatRate);
                var subtotalExclVat = (decimal)taxableAmountInclVat - vatAmount;


                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Subtotal:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {subtotalExclVat:N2}").FontSize(10);
                });

                // Discount (if any)
                if (orderHeader.DiscountAmount.HasValue && orderHeader.DiscountAmount.Value > 0)
                {
                    column.Item().AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Discount:").FontSize(10).FontColor(Colors.Green.Darken2);
                        row.ConstantItem(100).AlignRight().Text($"- AED {orderHeader.DiscountAmount.Value:N2}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                    
                    if (!string.IsNullOrEmpty(orderHeader.PromoCodeText))
                    {
                        column.Item().AlignRight().Row(row =>
                        {
                            row.ConstantItem(150).Text($"Promo Code ({orderHeader.PromoCodeText}):").FontSize(9).FontColor(Colors.Grey.Darken1);
                            row.ConstantItem(100);
                        });
                    }
                }

                

                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text($"VAT ({vatRate * 100}%):").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {vatAmount:N2}").FontSize(10);
                });

                decimal? deliveryAmount = null;
                if (orderHeader.OrderSubtotal.HasValue && orderHeader.OrderSubtotal.Value > 0)
                {
                    deliveryAmount = (decimal)orderHeader.OrderTotal - (decimal)orderHeader.OrderSubtotal.Value;
                }
                if (deliveryAmount.HasValue && deliveryAmount.Value > 0)
                {

                    column.Item().AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Delivery:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(100).AlignRight().Text($"AED {deliveryAmount:N2}").FontSize(10);
                    });
                }
                else
                {
                    column.Item().AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Delivery:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(100).AlignRight().Text("Free").FontSize(10);
                    });
                }
                column.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Total Amount:").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                    row.ConstantItem(100).AlignRight().Text($"AED {orderHeader.OrderTotal:N2}").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                });

                // Payment Method
                //if (!string.IsNullOrEmpty(orderHeader.PaymentMethod))
                //{
                //    column.Item().PaddingTop(10).AlignRight().Row(row =>
                //    {
                //        row.ConstantItem(150).Text("Payment Method:").FontSize(9).FontColor(Colors.Grey.Darken1);
                //        row.ConstantItem(100).AlignRight().Text(orderHeader.PaymentMethod).FontSize(9);
                //    });
                //}
            });
        }

        public byte[] GenerateServicePurchaseInvoicePdf(ServicePurchase servicePurchase, ApplicationUser? customer)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Element(ComposeHeader);

                    page.Content()
                        .Element(container => ComposeServicePurchaseContent(container, servicePurchase, customer));

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeServicePurchaseContent(IContainer container, ServicePurchase servicePurchase, ApplicationUser? customer)
        {
            container.Column(column =>
            {
                column.Spacing(25);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item()
                            .Text("Bill To")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);

                        var customerName = customer?.Name ?? servicePurchase.GuestName ?? "Customer";
                        var customerEmail = customer?.Email ?? servicePurchase.GuestEmail ?? "";
                        var customerPhone = customer?.PhoneNumber ?? servicePurchase.GuestPhone ?? "";

                        left.Item()
                            .PaddingTop(4)
                            .Text(customerName)
                            .FontSize(10)
                            .Bold();

                        if (!string.IsNullOrWhiteSpace(customerPhone))
                        {
                            left.Item().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Phone: ").SemiBold();
                                text.Span(customerPhone);
                            });
                        }

                        if (!string.IsNullOrWhiteSpace(customerEmail))
                        {
                            left.Item().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(9));
                                text.Span("Email: ").SemiBold();
                                text.Span(customerEmail);
                            });
                        }
                    });

                    row.ConstantItem(220).Column(right =>
                    {
                        right.Item()
                            .AlignRight()
                            .Text("Invoice Details")
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.Grey.Darken2);

                        right.Item().PaddingTop(4).AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Invoice No: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text($"INV-SVC-{servicePurchase.Id:D6}").FontSize(9).Bold();
                        });

                        right.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Purchase Date: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(servicePurchase.PurchaseDate.ToString("dd MMM yyyy")).FontSize(9);
                        });

                        right.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Payment Status: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(servicePurchase.PaymentStatus ?? "Pending").FontSize(9).Bold();
                        });
                    });
                });

                column.Item()
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Lighten2);
                
                column.Item()
                    .Element(c => ComposeServicePurchaseTable(c, servicePurchase));

                column.Item()
                    .Element(c => ComposeServicePurchaseSummary(c, servicePurchase));
            });
        }

        private void ComposeServicePurchaseTable(IContainer container, ServicePurchase servicePurchase)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);      // Description
                    columns.ConstantColumn(45);   // Qty
                    columns.ConstantColumn(70);    // Price excl VAT
                    columns.ConstantColumn(45);   // VAT rate
                    columns.ConstantColumn(70);    // VAT amount
                    columns.ConstantColumn(75);    // Price incl VAT
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("Quantity").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("Price excl VAT (AED)").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignCenter().Text("VAT rate").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignRight().Text("VAT amount (AED)").Bold().FontSize(8);
                    header.Cell().Element(CellStyle).AlignRight().Text("Price incl VAT (AED)").Bold().FontSize(8);
                });

                // Service item
                var serviceName = servicePurchase.ServiceSubscription?.Title ?? "Service Subscription";
                var vatRate = 0.05m;
                var totalAmountInclVat = servicePurchase.TotalAmount;
                var vatAmount = totalAmountInclVat * (vatRate / (1 + vatRate));
                var priceExclVat = totalAmountInclVat - vatAmount;

                table.Cell().Element(CellStyle).Text(serviceName).FontSize(7);
                table.Cell().Element(CellStyle).AlignCenter().Text("1").FontSize(7);
                table.Cell().Element(CellStyle).AlignCenter().Text($"AED {priceExclVat:N2}").FontSize(7);
                table.Cell().Element(CellStyle).AlignCenter().Text($"{vatRate * 100:F2}%").FontSize(9);
                table.Cell().Element(CellStyle).AlignCenter().Text($"AED {vatAmount:N2}").FontSize(7);
                table.Cell().Element(CellStyle).AlignRight().Text($"AED {totalAmountInclVat:N2}").FontSize(7);
            });
        }

        private void ComposeServicePurchaseSummary(IContainer container, ServicePurchase servicePurchase)
        {
            container.Column(column =>
            {
                column.Spacing(5);

                var vatRate = 0.05m;
                var totalAmountInclVat = servicePurchase.TotalAmount;
                var discount = servicePurchase.DiscountAmount;
                var taxableAmountInclVat = totalAmountInclVat - discount;
                var vatAmount = taxableAmountInclVat * vatRate / (1 + vatRate);
                var subtotalExclVat = taxableAmountInclVat - vatAmount;

                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Subtotal:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {subtotalExclVat:N2}").FontSize(10);
                });

                // Discount (if any)
                if (discount > 0)
                {
                    column.Item().AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Discount:").FontSize(10).FontColor(Colors.Green.Darken2);
                        row.ConstantItem(100).AlignRight().Text($"- AED {discount:N2}").FontSize(10).FontColor(Colors.Green.Darken2);
                    });
                }

                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text($"VAT ({vatRate * 100}%):").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {vatAmount:N2}").FontSize(10);
                });

                column.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Total Amount:").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                    row.ConstantItem(100).AlignRight().Text($"AED {totalAmountInclVat:N2}").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                });

                // Amount Paid (for offline services)
                if (servicePurchase.ServiceSubscription?.ServiceType == ServiceType.Offline)
                {
                    column.Item().PaddingTop(10).AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Amount Paid:").FontSize(10).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(100).AlignRight().Text($"AED {servicePurchase.AmountPaid:N2}").FontSize(10).Bold();
                    });

                    var remainingAmount = totalAmountInclVat - servicePurchase.AmountPaid;
                    if (remainingAmount > 0)
                    {
                        column.Item().AlignRight().Row(row =>
                        {
                            row.ConstantItem(150).Text("Remaining Amount:").FontSize(10).FontColor(Colors.Orange.Darken2);
                            row.ConstantItem(100).AlignRight().Text($"AED {remainingAmount:N2}").FontSize(10).Bold().FontColor(Colors.Orange.Darken2);
                        });
                    }
                }
            });
        }
    }
}
