using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// Create a Tappy payment session
        /// </summary>
        public async Task<TappyPaymentResponse> CreatePaymentAsync(TappyPaymentRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                //var response = await _httpClient.PostAsync("/v1/payments", content);
                var response = await _httpClient.PostAsync("https://sandbox-api.tappy.tech", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonSerializer.Deserialize<TappyPaymentResponse>(responseContent);
                    return paymentResponse ?? new TappyPaymentResponse { Success = false, Message = "Invalid response from Tappy" };
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
        /// Verify Tappy payment status
        /// </summary>
        public async Task<TappyPaymentStatusResponse> VerifyPaymentAsync(string transactionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/v1/payments/{transactionId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var statusResponse = JsonSerializer.Deserialize<TappyPaymentStatusResponse>(responseContent);
                    return statusResponse ?? new TappyPaymentStatusResponse { Success = false };
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

