namespace IdealWeightNutrition.Models.ViewModels
{
    public class ServiceSummaryVM
    {
        public ServiceSubscription Service { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public ServiceOffer? AppliedOffer { get; set; }
        public PromoCode? AppliedPromoCode { get; set; }
        public decimal MinAmountRequired { get; set; }
        public bool IsGuest { get; set; }
        public ApplicationUser? User { get; set; }
        
        // Form fields
        public int ServiceId { get; set; }
        public int? OfferId { get; set; }
        public string? PromoCode { get; set; }
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public decimal? CustomAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public bool CreateAccountForGuest { get; set; }
    }
}
