namespace BulkyBook.Utility
{
	public class TamaraSettings
	{
		public string ApiToken { get; set; } = string.Empty;
		public string MerchantId { get; set; } = string.Empty;
		public string BaseUrl { get; set; } = "https://api.tamara.co";
		public string NotificationToken { get; set; } = string.Empty;
		public bool Enabled { get; set; } = true;
		public bool UseSandbox { get; set; } = true;
	}
}

