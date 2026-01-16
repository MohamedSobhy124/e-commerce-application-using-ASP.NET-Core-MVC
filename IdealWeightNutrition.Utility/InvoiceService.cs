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

        public byte[] GenerateInvoicePdf(OrderHeader orderHeader, List<OrderDetail> orderDetails, ApplicationUser? customer)
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
            var businessPhone = _configuration["SiteSettings:Business:Phone"] ?? "+971-52-738-3841";
            var streetAddress = _configuration["SiteSettings:Business:Address:StreetAddress"] ?? "";
            var city = _configuration["SiteSettings:Business:Address:City"] ?? "";
            var state = _configuration["SiteSettings:Business:Address:State"] ?? "";
            var postalCode = _configuration["SiteSettings:Business:Address:PostalCode"] ?? "";
            var country = _configuration["SiteSettings:Business:Address:CountryName"] ?? "";
            var vatNumber = _configuration["SiteSettings:Business:VATRegistrationNumber"] ?? "";

            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(businessName).FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                    column.Item().Text("INVOICE").FontSize(18).Bold().FontColor(Colors.Grey.Darken2);
                    
                    // Build address string
                    var addressParts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(streetAddress)) addressParts.Add(streetAddress);
                    if (!string.IsNullOrWhiteSpace(city)) addressParts.Add(city);
                    if (!string.IsNullOrWhiteSpace(state)) addressParts.Add(state);
                    if (!string.IsNullOrWhiteSpace(postalCode)) addressParts.Add(postalCode);
                    if (!string.IsNullOrWhiteSpace(country)) addressParts.Add(country);
                    
                    if (addressParts.Count > 0)
                    {
                        column.Item().Text(string.Join(", ", addressParts)).FontSize(9).FontColor(Colors.Grey.Darken1);
                    }
                    
                    column.Item().Text($"Email: {businessEmail}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    column.Item().Text($"Phone: {businessPhone}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    
                    if (!string.IsNullOrEmpty(vatNumber))
                    {
                        column.Item().PaddingTop(5).Text($"VAT Registration Number: {vatNumber}").FontSize(9).Bold().FontColor(Colors.Grey.Darken2);
                    }
                });

                row.ConstantItem(50);
            });
        }

        private void ComposeContent(IContainer container, OrderHeader orderHeader, List<OrderDetail> orderDetails, ApplicationUser? customer)
        {
            container.Column(column =>
            {
                column.Spacing(20);

                // Invoice Details Section
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Bill To:").FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                        col.Item().Text(customer?.Name ?? orderHeader.Name).FontSize(10).Bold();
                        col.Item().Text(orderHeader.StreetAddress).FontSize(9);
                        col.Item().Text($"{orderHeader.City}, {orderHeader.State} {orderHeader.PostalCode}").FontSize(9);
                        col.Item().Text($"Phone: {orderHeader.PhoneNumber}").FontSize(9);
                        if (!string.IsNullOrEmpty(customer?.Email ?? orderHeader.Email))
                        {
                            col.Item().Text($"Email: {customer?.Email ?? orderHeader.Email}").FontSize(9);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text("Invoice Details").FontSize(11).Bold().FontColor(Colors.Grey.Darken2);
                        col.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Invoice #: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text($"INV-{orderHeader.Id:D6}").FontSize(9).Bold();
                        });
                        col.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Order Date: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(orderHeader.OrderDate.ToString("dd MMM yyyy")).FontSize(9);
                        });
                        col.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Payment Status: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(orderHeader.PaymentStatus ?? "Pending").FontSize(9).Bold();
                        });
                        col.Item().AlignRight().Row(r =>
                        {
                            r.AutoItem().Text("Order Status: ").FontSize(9).FontColor(Colors.Grey.Darken1);
                            r.AutoItem().Text(orderHeader.OrderStatus ?? "Pending").FontSize(9);
                        });
                    });
                });

                // Items Table
                column.Item().Element(container => ComposeTable(container, orderHeader, orderDetails));

                // Summary Section
                column.Item().Element(container => ComposeSummary(container, orderHeader));
            });
        }

        private void ComposeTable(IContainer container, OrderHeader orderHeader, List<OrderDetail> orderDetails)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Item Description").Bold().FontSize(10);
                    header.Cell().Element(CellStyle).AlignCenter().Text("Quantity").Bold().FontSize(10);
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").Bold().FontSize(10);
                    header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold().FontSize(10);
                });

                // Items
                foreach (var item in orderDetails)
                {
                    var isComboOffer = item.ComboOfferId.HasValue && item.ComboOffer != null;
                    var itemName = isComboOffer 
                        ? item.ComboOffer?.Name ?? "Combo Offer"
                        : item.Product?.Title ?? "Unknown Product";
                    
                    var unitPrice = (decimal)item.Price;
                    var totalPrice = unitPrice * item.Count;

                table.Cell().Element(CellStyle).Text(itemName).FontSize(9);
                table.Cell().Element(CellStyle).AlignCenter().Text(item.Count.ToString()).FontSize(9);
                table.Cell().Element(CellStyle).AlignRight().Text($"AED {unitPrice:N2}").FontSize(9);
                table.Cell().Element(CellStyle).AlignRight().Text($"AED {totalPrice:N2}").FontSize(9).Bold();
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

                // Subtotal
                var subtotal = orderHeader.OrderSubtotal ?? orderHeader.OrderTotal;
                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Subtotal:").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {subtotal:N2}").FontSize(10);
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

                // VAT Calculation (assuming 5% VAT in UAE)
                var vatRate = 0.05m;
                var taxableAmount = (decimal)subtotal - (decimal)(orderHeader.DiscountAmount ?? 0);
                var vatAmount = taxableAmount * vatRate;
                var total = taxableAmount + vatAmount;

                column.Item().AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text($"VAT ({vatRate * 100}%):").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(100).AlignRight().Text($"AED {vatAmount:N2}").FontSize(10);
                });

                // Total
                column.Item().PaddingTop(10).AlignRight().Row(row =>
                {
                    row.ConstantItem(150).Text("Total Amount:").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                    row.ConstantItem(100).AlignRight().Text($"AED {orderHeader.OrderTotal:N2}").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                });

                // Payment Method
                if (!string.IsNullOrEmpty(orderHeader.PaymentMethod))
                {
                    column.Item().PaddingTop(10).AlignRight().Row(row =>
                    {
                        row.ConstantItem(150).Text("Payment Method:").FontSize(9).FontColor(Colors.Grey.Darken1);
                        row.ConstantItem(100).AlignRight().Text(orderHeader.PaymentMethod).FontSize(9);
                    });
                }
            });
        }
    }
}
