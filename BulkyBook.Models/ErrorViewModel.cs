namespace BulkyBook.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        
        public int? StatusCode { get; set; }
        
        public string GetErrorMessage()
        {
            return StatusCode switch
            {
                404 => "Page Not Found",
                500 => "Internal Server Error",
                403 => "Access Forbidden",
                401 => "Unauthorized",
                _ => "An Error Occurred"
            };
        }
    }
}