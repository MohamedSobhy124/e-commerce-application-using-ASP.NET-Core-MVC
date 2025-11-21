using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class TamaraHelper
    {
        private readonly TamaraSettings _settings;
        private readonly HttpClient _httpClient;

        public TamaraHelper(TamaraSettings settings)
        {
            _settings = settings;
            _httpClient = new HttpClient();
            
            // Use sandbox or production URL
            var baseUrl = _settings.UseSandbox 
                ? "https://api-sandbox.tamara.co" 
                : "https://api.tamara.co";
            
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiToken}");
        }

        /// <summary>
        /// Create a Tamara checkout session
        /// </summary>
        public async Task<TamaraPaymentResponse> CreateCheckoutAsync(TamaraPaymentRequest request)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(request, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/checkout", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonSerializer.Deserialize<TamaraPaymentResponse>(responseContent, options);
                    return paymentResponse ?? new TamaraPaymentResponse { Success = false, Message = "Invalid response from Tamara" };
                }
                else
                {
                    return new TamaraPaymentResponse
                    {
                        Success = false,
                        Message = $"Checkout creation failed: {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new TamaraPaymentResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Authorize Tamara order
        /// </summary>
        public async Task<TamaraAuthorizationResponse> AuthorizeOrderAsync(string orderId)
        {
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"/orders/{orderId}/authorise", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                if (response.IsSuccessStatusCode)
                {
                    var authResponse = JsonSerializer.Deserialize<TamaraAuthorizationResponse>(responseContent, options);
                    return authResponse ?? new TamaraAuthorizationResponse { Success = false };
                }
                else
                {
                    return new TamaraAuthorizationResponse { Success = false, Message = responseContent };
                }
            }
            catch (Exception ex)
            {
                return new TamaraAuthorizationResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get order details from Tamara
        /// </summary>
        public async Task<TamaraOrderDetailsResponse> GetOrderDetailsAsync(string orderId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/orders/{orderId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                if (response.IsSuccessStatusCode)
                {
                    var orderDetails = JsonSerializer.Deserialize<TamaraOrderDetailsResponse>(responseContent, options);
                    return orderDetails ?? new TamaraOrderDetailsResponse { Success = false };
                }
                else
                {
                    return new TamaraOrderDetailsResponse { Success = false };
                }
            }
            catch (Exception ex)
            {
                return new TamaraOrderDetailsResponse { Success = false, Message = ex.Message };
            }
        }
    }

    // Request/Response Models for Tamara
    public class TamaraPaymentRequest
    {
        [JsonPropertyName("order_reference_id")]
        public string OrderReferenceId { get; set; }

        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = "AE";

        [JsonPropertyName("payment_type")]
        public string PaymentType { get; set; } = "PAY_BY_INSTALMENTS";

        [JsonPropertyName("locale")]
        public string Locale { get; set; } = "en_US";

        [JsonPropertyName("merchant_url")]
        public TamaraMerchantUrl MerchantUrl { get; set; }

        [JsonPropertyName("consumer")]
        public TamaraConsumer Consumer { get; set; }

        [JsonPropertyName("items")]
        public List<TamaraItem> Items { get; set; }
    }

    public class TamaraAmount
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "AED";
    }

    public class TamaraMerchantUrl
    {
        [JsonPropertyName("success")]
        public string Success { get; set; }

        [JsonPropertyName("failure")]
        public string Failure { get; set; }

        [JsonPropertyName("cancel")]
        public string Cancel { get; set; }

        [JsonPropertyName("notification")]
        public string Notification { get; set; }
    }

    public class TamaraConsumer
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }

    public class TamaraItem
    {
        [JsonPropertyName("reference_id")]
        public string ReferenceId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("total_amount")]
        public TamaraAmount TotalAmount { get; set; }
    }

    public class TamaraPaymentResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("checkout_url")]
        public string CheckoutUrl { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("checkout_id")]
        public string CheckoutId { get; set; }
    }

    public class TamaraAuthorizationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class TamaraOrderDetailsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        [JsonPropertyName("order_id")]
        public string OrderId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("payment_status")]
        public string PaymentStatus { get; set; }
    }
}

