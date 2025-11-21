using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.Utility
{
    public class SD
    {
        public const string Role_Customer = "Customer";
        public const string Role_Company = "Company";
        public const string Role_Admin = "Admin";
        public const string Role_Employee = "Employee";

		public const string StatusPending = "Pending";
		public const string StatusApproved = "Approved";
		public const string StatusPaid = "Paid";
		public const string StatusInProcess = "Processing";
		public const string StatusInPreparingShiping = "Preparing Shiping";
		public const string StatusShipped = "Shipped";
		public const string StatusOutForDelivery = "OutForDelivery";
		public const string StatusDelivered = "Delivered";
		public const string StatusCancelled = "Cancelled";
		public const string StatusReturned = "Returned";
		public const string StatusRefunded = "Refunded";

		public const string PaymentStatusPending = "Pending";
		public const string PaymentStatusPaid = "Paid";
		public const string PaymentStatusDelayedPayment = "ApprovedForDelayedPayment";
		public const string PaymentStatusRejected = "Rejected";

		// Payment Methods
		public const string PaymentMethodStripe = "Stripe";
		public const string PaymentMethodTappy = "Tappy";
		public const string PaymentMethodTamara = "Tamara";


		public const string SessionCart = "SessionShoppingCart";

	}
}
