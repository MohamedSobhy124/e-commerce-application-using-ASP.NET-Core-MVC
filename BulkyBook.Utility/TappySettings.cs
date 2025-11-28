namespace BulkyBook.Utility
{
	public class TappySettings
	{
		public string ApiKey { get; set; } = string.Empty;
		public string MerchantId { get; set; } = string.Empty;
		public string MerchantCode { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = "https://api.tabby.ai";
		public bool Enabled { get; set; } = true;
	}
}


