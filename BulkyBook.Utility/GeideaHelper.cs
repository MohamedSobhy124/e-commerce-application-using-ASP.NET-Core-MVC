using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Globalization;

namespace BulkyBook.Utility
{
	public class GeideaHelper
	{
		private readonly GeideaSettings _settings;
		private readonly HttpClient _httpClient;

		public GeideaHelper(GeideaSettings settings)
		{
			_settings = settings;
			_httpClient = new HttpClient();
		}

		private string GetBaseUrl()
		{
			if (!string.IsNullOrEmpty(_settings.BaseUrl))
			{
				return _settings.BaseUrl;
			}
			
			return "https://api.geidea.ae";
		}

		private string GetEndpoint()
		{
			return "/payment-intent/api/v2/direct/session";
		}

        /// <summary>
        /// Generate Geidea signature using HMAC SHA256
        /// According to working example: merchantPublicKey + amount + currency + merchantReferenceId + timestamp
        /// Amount is in smallest currency unit (cents for AED, e.g., 1850 for 18.50 AED)
        /// </summary>
        static string GenerateSignature(string merchantPublicKey, decimal orderAmount, string orderCurrency, string? orderMerchantReferenceId, string apiPassword, string timeStamp)
        {
            var amountStr = orderAmount.ToString("F2", CultureInfo.InvariantCulture);
            var data = $"{merchantPublicKey}{amountStr}{orderCurrency}{orderMerchantReferenceId}{timeStamp}";
            using var hmacSha256 = new HMACSHA256(Encoding.UTF8.GetBytes(apiPassword));
            var hash = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);

        }

        /// <summary>
        /// Create a Geidea payment session/order
        /// </summary>
        public async Task<GeideaPaymentResponse> CreatePaymentAsync(GeideaPaymentRequest request)
		{
			try
			{
				if (string.IsNullOrEmpty(_settings.MerchantPublicKey) || string.IsNullOrEmpty(_settings.MerchantApiPassword))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Geidea credentials are not configured. Please check MerchantPublicKey and MerchantApiPassword in appsettings.json"
					};
				}

				decimal amountInSmallestUnit = (request.Amount);
				string currency = request.Currency ?? "AED";
				
				DateTime timestampDate = DateTimeHelper.Now;
				string timestamp = timestampDate.ToString("M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
				
				string signature = GenerateSignature(_settings.MerchantPublicKey, amountInSmallestUnit, currency, request.OrderId, _settings.MerchantApiPassword, timestamp);
				
				var callbackUrl = request.ReturnUrl ?? request.CancelUrl ?? "";
				
				if (string.IsNullOrEmpty(callbackUrl))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Callback URL is required for Geidea payment"
					};
				}
				
				
				var geideaRequest = new Dictionary<string, object>
				{
					["amount"] = amountInSmallestUnit,
					["currency"] = currency,
					["timestamp"] = timestamp,
					["merchantReferenceId"] = request.OrderId,
					["signature"] = signature,
					["paymentOperation"] = "Pay",
					["callbackUrl"] = callbackUrl
				};
				
				if (!string.IsNullOrEmpty(request.Language))
				{
					geideaRequest["language"] = request.Language;
				}
				else
				{
					geideaRequest["language"] = "en";  
				}
				
				geideaRequest["appearance"] = new Dictionary<string, object>
				{
					["uiMode"] = "modal"
				};
				
				if (!string.IsNullOrEmpty(request.ReturnUrl))
				{
					geideaRequest["returnUrl"] = request.ReturnUrl;
				}
				
				if (!string.IsNullOrEmpty(request.CustomerEmail) || !string.IsNullOrEmpty(request.CustomerPhone))
				{
					var customer = new Dictionary<string, object>();
					if (!string.IsNullOrEmpty(request.CustomerEmail))
						customer["email"] = request.CustomerEmail;
					if (!string.IsNullOrEmpty(request.CustomerPhone))
					{
						customer["phoneNumber"] = request.CustomerPhone;
						if (request.CustomerPhone.StartsWith("+"))
						{
							var phoneParts = request.CustomerPhone.Split(' ');
							if (phoneParts.Length > 0)
							{
								customer["phoneCountryCode"] = phoneParts[0];
							}
						}
					}
					if (customer.Count > 0)
						geideaRequest["customer"] = customer;
				}
				
				geideaRequest["initiatedBy"] = "Internet";

				var options = new JsonSerializerOptions
				{
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = false
				};

				var json = JsonSerializer.Serialize(geideaRequest, options);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				var publicKey = (_settings.MerchantPublicKey ?? "").Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
				var apiPassword = (_settings.MerchantApiPassword ?? "").Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
				
				if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(apiPassword))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = "Geidea credentials are empty. Please check MerchantPublicKey and MerchantApiPassword in appsettings.json"
					};
				}
				
				if (publicKey.Length != 36 || !publicKey.Contains("-"))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Geidea MerchantPublicKey format appears invalid. Expected UUID format (36 characters with hyphens). Got: {publicKey.Substring(0, Math.Min(20, publicKey.Length))}..."
					};
				}
				
				var credentials = $"{publicKey}:{apiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				string endpoint = GetEndpoint();
				string fullUrl = $"{baseUrl}{endpoint}";
				var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl)
				{
					Content = content
				};
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (!response.IsSuccessStatusCode)
				{
					if (responseContent.Contains("<html>") || responseContent.Contains("Request Rejected"))
					{
						string callbackUrlNote = callbackUrl.Contains("localhost") 
							? " NOTE: Geidea does NOT allow localhost callback URLs. Use ngrok or a public URL for testing."
							: "";
						
						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: Request was rejected by server (Status: {response.StatusCode}).{callbackUrlNote} " +
									  $"Request URL: {fullUrl}. " +
									  $"Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}"
						};
					}

					if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
					{
						try
						{
							using var errorDoc = JsonDocument.Parse(responseContent);
							var errorRoot = errorDoc.RootElement;
							var errorMessage = errorRoot.TryGetProperty("detailedResponseMessage", out var msgProp)
								? msgProp.GetString()
								: errorRoot.TryGetProperty("responseMessage", out var respMsgProp)
								? respMsgProp.GetString()
								: errorRoot.TryGetProperty("message", out var msgProp2)
								? msgProp2.GetString()
								: responseContent;

							return new GeideaPaymentResponse
							{
								Success = false,
								Message = $"Geidea Authentication Failed (401 Unauthorized): {errorMessage}. "
							};
						}
						catch
						{
							return new GeideaPaymentResponse
							{
								Success = false,
								Message = $"Geidea Authentication Failed (401 Unauthorized): {responseContent}. " +
										  $"Please verify your MerchantPublicKey and MerchantApiPassword are correct."
							};
						}
					}

					try
					{
						using var errorDoc = JsonDocument.Parse(responseContent);
						var errorRoot = errorDoc.RootElement;
						var errorMessage = errorRoot.TryGetProperty("detailedResponseMessage", out var msgProp)
							? msgProp.GetString()
							: errorRoot.TryGetProperty("responseMessage", out var respMsgProp)
							? respMsgProp.GetString()
							: errorRoot.TryGetProperty("message", out var msgProp2)
							? msgProp2.GetString()
							: responseContent;

						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: {errorMessage}. Status: {response.StatusCode}"
						};
					}
					catch
					{
						return new GeideaPaymentResponse
						{
							Success = false,
							Message = $"Payment creation failed: {responseContent}. Status: {response.StatusCode}"
						};
					}
				}

				using var doc = JsonDocument.Parse(responseContent);
				var root = doc.RootElement;

				var responseCode = root.TryGetProperty("responseCode", out var codeProp) ? codeProp.GetString() : null;
				var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var detailCodeProp) ? detailCodeProp.GetString() : null;
				
				if (responseCode != "000" || detailedResponseCode != "000")
				{
					var errorMessage = root.TryGetProperty("detailedResponseMessage", out var msgProp)
						? msgProp.GetString()
						: root.TryGetProperty("responseMessage", out var respMsgProp)
						? respMsgProp.GetString()
						: "Unknown error";
					
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Geidea API Error: {errorMessage} (Code: {responseCode}, Detail: {detailedResponseCode})"
					};
				}

				string redirectUrl = null;
				string sessionId = null;
				string orderId = null;

				if (root.TryGetProperty("redirectUrl", out var redirectUrlProp))
				{
					redirectUrl = redirectUrlProp.GetString();
				}
				else if (root.TryGetProperty("checkoutUrl", out var checkoutUrlProp))
				{
					redirectUrl = checkoutUrlProp.GetString();
				}
				else if (root.TryGetProperty("paymentUrl", out var paymentUrlProp))
				{
					redirectUrl = paymentUrlProp.GetString();
				}
				else if (root.TryGetProperty("url", out var urlProp))
				{
					redirectUrl = urlProp.GetString();
				}

				if (root.TryGetProperty("session", out var sessionProp))
				{
					if (sessionProp.TryGetProperty("id", out var sessionIdProp))
					{
						sessionId = sessionIdProp.GetString();
					}
				}
				
				if (string.IsNullOrEmpty(sessionId) && root.TryGetProperty("sessionId", out var sessionIdDirectProp))
				{
					sessionId = sessionIdDirectProp.GetString();
				}

				if (root.TryGetProperty("orderId", out var orderIdProp))
				{
					orderId = orderIdProp.GetString();
				}

				if (string.IsNullOrEmpty(redirectUrl) && !string.IsNullOrEmpty(sessionId))
				{
					string apiBaseUrl = GetBaseUrl();
					string checkoutBaseUrl;
					
					if (apiBaseUrl.Contains("ksamerchant"))
					{
						checkoutBaseUrl = "https://www.ksamerchant.geidea.net/hpp/checkout";
					}
					else
					{
						checkoutBaseUrl = "https://www.merchant.geidea.net/hpp/checkout";
					}
					
					redirectUrl = $"{checkoutBaseUrl}/?sessionId={sessionId}";
				}

				if (string.IsNullOrEmpty(redirectUrl) && string.IsNullOrEmpty(sessionId))
				{
					return new GeideaPaymentResponse
					{
						Success = false,
						Message = $"Payment session created but neither redirectUrl nor session ID found in response. Response: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}"
					};
				}

				if (string.IsNullOrEmpty(redirectUrl) && !string.IsNullOrEmpty(sessionId))
				{
					redirectUrl = $"https://www.merchant.geidea.net/hpp/checkout/?sessionId={sessionId}";
				}

				return new GeideaPaymentResponse
				{
					Success = true,
					PaymentUrl = redirectUrl ?? "", 
					TransactionId = sessionId ?? orderId ?? "", 
					Status = "Created"
				};
			}
			catch (Exception ex)
			{
				return new GeideaPaymentResponse
				{
					Success = false,
					Message = $"Error creating payment: {ex.Message}"
				};
			}
		}
 

		/// <summary>
		/// Verify Geidea payment status using merchant reference ID (order ID)
		/// Uses the correct Geidea API endpoint: /pgw/api/v1/direct/order?MerchantReferenceId={id}
		/// </summary>
		public async Task<GeideaPaymentStatusResponse> VerifyPaymentAsync(string merchantReferenceId)
		{
			try
			{
				if (string.IsNullOrEmpty(merchantReferenceId))
				{
					return new GeideaPaymentStatusResponse
					{
						Success = false,
						Message = "Merchant reference ID (Order ID) is required for verification"
					};
				}

				var credentials = $"{_settings.MerchantPublicKey}:{_settings.MerchantApiPassword}";
				var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
				
				string baseUrl = GetBaseUrl();
				
				string fullUrl = $"{baseUrl}/pgw/api/v1/direct/order?MerchantReferenceId={Uri.EscapeDataString(merchantReferenceId)}";
				
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, fullUrl);
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
				requestMessage.Headers.Accept.Clear();
				requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var response = await _httpClient.SendAsync(requestMessage);
				var responseContent = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					using var doc = JsonDocument.Parse(responseContent);
					var root = doc.RootElement;

					var responseCode = root.TryGetProperty("responseCode", out var rcProp) ? rcProp.GetString() : null;
					var detailedResponseCode = root.TryGetProperty("detailedResponseCode", out var drcProp) ? drcProp.GetString() : null;
					
					if (responseCode != "000" || detailedResponseCode != "000")
					{
						return new GeideaPaymentStatusResponse
						{
							Success = false,
							Message = $"API returned error code: {responseCode}/{detailedResponseCode}"
						};
					}

					if (!root.TryGetProperty("orders", out var ordersProp) || ordersProp.ValueKind != JsonValueKind.Array)
					{
						return new GeideaPaymentStatusResponse
						{
							Success = false,
							Message = "No orders found in response"
						};
					}

					bool isPaid = false;
					string orderStatus = null;
					string orderId = null;
					
					foreach (var order in ordersProp.EnumerateArray())
					{
						var status = order.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
						var detailedStatus = order.TryGetProperty("detailedStatus", out var detailedStatusProp) ? detailedStatusProp.GetString() : null;
						var currentOrderId = order.TryGetProperty("orderId", out var oidProp) ? oidProp.GetString() : null;
						
						if (status?.Equals("Success", StringComparison.OrdinalIgnoreCase) == true ||
							detailedStatus?.Equals("Paid", StringComparison.OrdinalIgnoreCase) == true)
						{
							isPaid = true;
							orderStatus = status;
							orderId = currentOrderId;
							break; // Use the first (most recent) successful order
						}
						
						if (string.IsNullOrEmpty(orderStatus))
						{
							orderStatus = status;
							orderId = currentOrderId;
						}
					}

					return new GeideaPaymentStatusResponse
					{
						Success = true,
						Status = orderStatus,
						TransactionId = orderId ?? merchantReferenceId,
						IsPaid = isPaid,
						Message = isPaid ? "Payment verified successfully" : $"Payment status: {orderStatus}"
					};
				}
				else
				{
					return new GeideaPaymentStatusResponse
					{
						Success = false,
						Message = $"Verification failed: {response.StatusCode}. Response: {responseContent}"
					};
				}
			}
			catch (Exception ex)
			{
				return new GeideaPaymentStatusResponse
				{
					Success = false,
					Message = $"Error verifying payment: {ex.Message}"
				};
			}
		}
	}

	// Request/Response Models
	public class GeideaPaymentRequest
	{
		public decimal Amount { get; set; }
		public string Currency { get; set; } = "AED";
		public string OrderId { get; set; }
		public string CustomerName { get; set; }
		public string CustomerEmail { get; set; }
		public string CustomerPhone { get; set; }
		public string ReturnUrl { get; set; }
		public string CancelUrl { get; set; }
		public string? BillingAddress { get; set; }
		public string? BillingCity { get; set; }
		public string? BillingState { get; set; }
		public string? BillingPostalCode { get; set; }
		public string? BillingCountryCode { get; set; }
		public string? Language { get; set; }
		public string? PaymentOperation { get; set; }
		public List<GeideaOrderItem>? Items { get; set; }
	}

	public class GeideaOrderItem
	{
		public string Name { get; set; }
		public string? Description { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
		public string? Sku { get; set; }
	}

	public class GeideaPaymentResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string PaymentUrl { get; set; }
		public string TransactionId { get; set; }
		public string Status { get; set; }
	}

	public class GeideaPaymentStatusResponse
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public string Status { get; set; }
		public string TransactionId { get; set; }
		public bool IsPaid { get; set; }
	}
}
