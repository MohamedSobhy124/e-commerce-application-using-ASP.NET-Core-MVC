using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            // Get order header without ApplicationUser first
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            // Load ApplicationUser only if it's not a guest order
            if (!orderHeader.IsGuestOrder && !string.IsNullOrEmpty(orderHeader.ApplicationUserId))
            {
                orderHeader.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == orderHeader.ApplicationUserId);
            }

            OrderVM = new OrderVM
            {
                OrderHeader = orderHeader,
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == id, includeProperties: "Product")
            };

            return View(OrderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.save();

            TempData["success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { id = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult StartProcessing(int id)
        {
            _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusInProcess);
            _unitOfWork.save();
            TempData["success"] = "Order Status Updated Successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult ShipOrder(int id, string carrier, string trackingNumber)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }
            
            orderHeader.TrackingNumber = trackingNumber;
            orderHeader.Carrier = carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }

            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.save();
            TempData["success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult MarkAsDelivered(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            if (orderHeader.OrderStatus != SD.StatusShipped)
            {
                TempData["error"] = "Only shipped orders can be marked as delivered";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusDelivered);
            _unitOfWork.save();
            TempData["success"] = "Order Marked as Delivered Successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]
        public IActionResult CancelOrder(int id)
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id);
            
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found";
                return RedirectToAction(nameof(Index));
            }

            if (orderHeader.PaymentStatus == SD.PaymentStatusPaid)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.save();
            TempData["success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            // Get all order headers without including ApplicationUser
            IEnumerable<OrderHeader> objOrderHeaders = _unitOfWork.OrderHeader.GetAll().ToList();

            // Load ApplicationUser only for non-guest orders
            foreach (var order in objOrderHeaders)
            {
                if (!order.IsGuestOrder && !string.IsNullOrEmpty(order.ApplicationUserId))
                {
                    order.ApplicationUser = _unitOfWork.applicationUser.Get(u => u.Id == order.ApplicationUserId);
                }
            }

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == status);
            }

            return Json(new { data = objOrderHeaders });
        }

        #endregion
    }
}

