using System;
using System.Web;

namespace BulkyBook.Utility
{
    public static class WhatsAppHelper
    {
        /// <summary>
        /// Generates WhatsApp chat URL
        /// </summary>
        /// <param name="phoneNumber">Phone number with country code (e.g., 966500000000)</param>
        /// <param name="message">Pre-filled message</param>
        /// <returns>WhatsApp URL</returns>
        public static string GetWhatsAppUrl(string phoneNumber, string message = "")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "#";
            }

            // Clean phone number (remove spaces, dashes, etc.)
            phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Encode message for URL
            string encodedMessage = string.IsNullOrWhiteSpace(message) 
                ? "" 
                : Uri.EscapeDataString(message);

            // WhatsApp Web URL format
            return $"https://wa.me/{phoneNumber}?text={encodedMessage}";
        }

        /// <summary>
        /// Generates WhatsApp URL for product inquiry
        /// </summary>
        public static string GetProductInquiryUrl(string phoneNumber, string productName, string productUrl, string language = "en")
        {
            string message = language == "ar"
                ? $"مرحباً! أنا مهتم بهذا المنتج: {productName}\n{productUrl}"
                : $"Hello! I'm interested in this product: {productName}\n{productUrl}";

            return GetWhatsAppUrl(phoneNumber, message);
        }

        /// <summary>
        /// Generates WhatsApp URL for order inquiry
        /// </summary>
        public static string GetOrderInquiryUrl(string phoneNumber, int orderId, string language = "en")
        {
            string message = language == "ar"
                ? $"مرحباً! لدي استفسار حول الطلب رقم #{orderId}"
                : $"Hello! I have a question about order #{orderId}";

            return GetWhatsAppUrl(phoneNumber, message);
        }

        /// <summary>
        /// Generates WhatsApp URL for general support
        /// </summary>
        public static string GetSupportUrl(string phoneNumber, string language = "en")
        {
            string message = language == "ar"
                ? "مرحباً! أحتاج إلى مساعدة."
                : "Hello! I need some help.";

            return GetWhatsAppUrl(phoneNumber, message);
        }
    }
}

