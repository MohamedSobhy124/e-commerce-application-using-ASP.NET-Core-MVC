using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace BulkyBook.Utility
{
    public class TappyHelper
    {
        private readonly TappySettings _settings;
        private readonly HttpClient _httpClient;

        public TappyHelper(TappySettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
        }

        /// <summary>
        /// Create a Tabby checkout session
        /// </summary>
        public async Task<TappyPaymentResponse> CreatePaymentAsync(TappyPaymentRequest request)
        {
            try
            {
                // Clean phone number - remove non-digits
                var cleanPhone = string.IsNullOrEmpty(request.CustomerPhone) 
                    ? "500000001" 
                    : new string(request.CustomerPhone.Where(char.IsDigit).ToArray());
                
                if (string.IsNullOrEmpty(cleanPhone))
                    cleanPhone = "500000001";

                // Build Tabby API request structure - matching exact format from documentation
                var tabbyRequest = new Dictionary<string, object>
                {
                    ["payment"] = new Dictionary<string, object>
                    {
                        ["amount"] = request.Amount.ToString("F2"),
                        ["currency"] = request.Currency,
                        ["description"] = request.Description ?? $"Order {request.OrderId}",
                        ["buyer"] = new Dictionary<string, object>
                        {
                            ["name"] = request.CustomerName ?? "Customer",
                            ["email"] = request.CustomerEmail ?? "",
                            ["phone"] = cleanPhone,
                            ["dob"] = request.BuyerDateOfBirth ?? "1990-01-20"
                        },
                        ["shipping_address"] = new Dictionary<string, object>
                        {
                            ["city"] = request.ShippingCity ?? "Dubai",
                            ["address"] = request.ShippingAddress ?? "Dubai",
                            ["zip"] = request.ShippingPostalCode ?? "1111"
                        },
                        ["order"] = new Dictionary<string, object>
                        {
                            ["reference_id"] = request.OrderId,
                            ["updated_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            ["tax_amount"] = (request.TaxAmount ?? 0).ToString("F2"),
                            ["shipping_amount"] = (request.ShippingAmount ?? 0).ToString("F2"),
                            ["discount_amount"] = (request.DiscountAmount ?? 0).ToString("F2"),
                            ["items"] = request.Items?.Select(item => new Dictionary<string, object>
                            {
                                ["reference_id"] = item.ReferenceId,
                                ["title"] = item.Title ?? "Product", // Required
                                ["description"] = item.Description ?? item.Title ?? "Product",
                                ["quantity"] = item.Quantity, // Required
                                ["unit_price"] = item.UnitPrice.ToString("F2"), // Required
                                ["discount_amount"] = (item.DiscountAmount ?? 0).ToString("F2"),
                                ["image_url"] = item.ImageUrl ?? "https://example.com/",
                                ["product_url"] = item.ProductUrl ?? "https://example.com/",
                                ["category"] = item.Category ?? "General" // Required
                            }).ToArray() ?? Array.Empty<Dictionary<string, object>>()
                        }
                    },
                    ["lang"] = request.Language ?? "en",
                    ["merchant_code"] = _settings.MerchantCode,
                    ["merchant_urls"] = new Dictionary<string, object>
                    {
                        ["success"] = request.ReturnUrl,
                        ["cancel"] = request.CancelUrl,
                        ["failure"] = request.CancelUrl
                    }
                };
                
                // Add token only if provided (optional field)
                if (!string.IsNullOrEmpty(request.Token))
                {
                    tabbyRequest["token"] = request.Token;
                }

                // Don't use PropertyNamingPolicy for Dictionary - keys are used as-is (already in snake_case)
                var options = new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                // Validate merchant code
                if (string.IsNullOrEmpty(_settings.MerchantCode))
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = "MerchantCode is required. Please configure it in appsettings.json"
                    };
                }
                
                // Validate items - Tabby requires at least one item with required fields
                if (request.Items == null || !request.Items.Any())
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = "At least one order item is required for Tabby checkout"
                    };
                }
                
                // Validate required item fields
                foreach (var item in request.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.Title))
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item title is required for all order items"
                        };
                    }
                    if (item.Quantity <= 0)
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item quantity must be greater than 0"
                        };
                    }
                    if (string.IsNullOrWhiteSpace(item.Category))
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = "Item category is required for all order items"
                        };
                    }
                }

                var json = JsonSerializer.Serialize(tabbyRequest, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/v2/checkout", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log error details for debugging
                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse error response
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        var errorRoot = errorDoc.RootElement;
                        var errorMessage = errorRoot.TryGetProperty("error", out var errorProp) 
                            ? errorProp.GetString() 
                            : responseContent;
                        var errorType = errorRoot.TryGetProperty("errorType", out var errorTypeProp) 
                            ? errorTypeProp.GetString() 
                            : "unknown";
                        
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = $"Payment creation failed ({errorType}): {errorMessage}. Request JSON: {json.Substring(0, Math.Min(500, json.Length))}..."
                        };
                    }
                    catch
                    {
                        return new TappyPaymentResponse
                        {
                            Success = false,
                            Message = $"Payment creation failed: {responseContent}. Status: {response.StatusCode}"
                        };
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    // Parse Tabby response according to API documentation
                    // Response structure: { id, configuration: { available_products: { installments: [{ web_url }] } }, payment: { id }, status }
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    // Get session ID
                    var sessionId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                    
                    // Get payment ID from payment object
                    var paymentId = root.TryGetProperty("payment", out var paymentProp) 
                        && paymentProp.TryGetProperty("id", out var paymentIdProp)
                        ? paymentIdProp.GetString() 
                        : null;
                    
                    // Get checkout URL from configuration.available_products.installments[0].web_url
                    string? checkoutUrl = null;
                    if (root.TryGetProperty("configuration", out var configProp))
                    {
                        if (configProp.TryGetProperty("available_products", out var availableProductsProp))
                        {
                            if (availableProductsProp.TryGetProperty("installments", out var installmentsProp))
                            {
                                if (installmentsProp.ValueKind == JsonValueKind.Array && installmentsProp.GetArrayLength() > 0)
                                {
                                    var firstInstallment = installmentsProp[0];
                                    if (firstInstallment.TryGetProperty("web_url", out var webUrlProp))
                                    {
                                        checkoutUrl = webUrlProp.GetString();
                                    }
                                }
                            }
                        }
                    }
                    
                    // Get status
                    var status = root.TryGetProperty("status", out var statusProp) 
                        ? statusProp.GetString() 
                        : "created";
                    
                    var paymentResponse = new TappyPaymentResponse
                    {
                        Success = true,
                        PaymentUrl = checkoutUrl,
                        TransactionId = paymentId ?? sessionId, // Prefer payment ID, fallback to session ID
                        Status = status ?? "created"
                    };

                    return paymentResponse;
                }
                else
                {
                    return new TappyPaymentResponse
                    {
                        Success = false,
                        Message = $"Payment creation failed: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new TappyPaymentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verify Tabby payment status
        /// </summary>
        public async Task<TappyPaymentStatusResponse> VerifyPaymentAsync(string transactionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v2/payments/{transactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var root = doc.RootElement;
                    
                    var statusResponse = new TappyPaymentStatusResponse
                    {
                        Success = true,
                        Status = root.TryGetProperty("status", out var status) ? status.GetString() : null,
                        TransactionId = transactionId,
                        IsPaid = root.TryGetProperty("status", out var statusProp) 
                            && statusProp.GetString()?.ToLower() == "authorized"
                    };

                    return statusResponse;
                }
                else
                {
                    return new TappyPaymentStatusResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                return new TappyPaymentStatusResponse { Success = false, Message = ex.Message };
            }
        }
    }

    // Request/Response Models
    public class TappyPaymentRequest
    {
        public string MerchantId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "AED";
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
        public string Description { get; set; }
        
        // Additional Tabby-specific fields
        public string? BuyerDateOfBirth { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ShippingPostalCode { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? ShippingAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Language { get; set; }
        public List<TabbyOrderItem>? Items { get; set; }
        public string? Token { get; set; } // Optional token for tokenized payments
    }

    public class TabbyOrderItem
    {
        public string ReferenceId { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductUrl { get; set; }
        public string? Category { get; set; }
    }

    public class TappyPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string PaymentUrl { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; }
    }

    public class TappyPaymentStatusResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string TransactionId { get; set; }
        public bool IsPaid { get; set; }
    }
}
