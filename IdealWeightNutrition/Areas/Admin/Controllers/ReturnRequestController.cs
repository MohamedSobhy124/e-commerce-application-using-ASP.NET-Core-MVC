using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ReturnRequestController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;
        private readonly IdealWeightNutrition.Services.INotificationService _notificationService;
        private readonly IdealWeightNutrition.Services.IStockService _stockService;
        private readonly ILogger<ReturnRequestController> _logger;

        public ReturnRequestController(
            IUnitOfWork unitOfWork,
            IStringLocalizer<IdealWeightNutrition.SharedResources> localizer,
            IdealWeightNutrition.Services.INotificationService notificationService,
            IdealWeightNutrition.Services.IStockService stockService,
            ILogger<ReturnRequestController> logger)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
            _notificationService = notificationService;
            _stockService = stockService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            return View(returnRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNotes, string? returnTrackingNumber, string? returnCarrier)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            if (returnRequest.Status != SD.ReturnStatusPending)
            {
                TempData["error"] = _localizer["ReturnRequestCannotBeApproved"]?.Value ?? "Only pending return requests can be approved";
                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = SD.ReturnStatusApproved;
            returnRequest.ApprovedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            returnRequest.AdminNotes = adminNotes;
            
            if (!string.IsNullOrEmpty(returnTrackingNumber))
            {
                returnRequest.ReturnTrackingNumber = returnTrackingNumber;
                returnRequest.ReturnCarrier = returnCarrier;
                returnRequest.ReturnShippedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            }

            _unitOfWork.ReturnRequest.Update(returnRequest);
            _unitOfWork.save();

            // Update order status - use the already loaded OrderHeader from returnRequest or get a fresh instance
            if (returnRequest.OrderHeader != null && returnRequest.OrderHeader.OrderStatus != SD.StatusReturnApproved)
            {
                returnRequest.OrderHeader.OrderStatus = SD.StatusReturnApproved;
                _unitOfWork.OrderHeader.Update(returnRequest.OrderHeader);
                _unitOfWork.save();
            }
            else
            {
                // If OrderHeader wasn't loaded, use UpdateStatus method which handles tracking properly
                await _unitOfWork.OrderHeader.UpdateStatus(returnRequest.OrderHeaderId, SD.StatusReturnApproved);
            }

            // Send notification to customer
            await _notificationService.SendReturnRequestStatusUpdateToCustomer(returnRequest);

            TempData["success"] = _localizer["ReturnRequestApproved"]?.Value ?? "Return request approved successfully";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason, string? adminNotes)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            if (returnRequest.Status != SD.ReturnStatusPending)
            {
                TempData["error"] = _localizer["ReturnRequestCannotBeRejected"]?.Value ?? "Only pending return requests can be rejected";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["error"] = _localizer["RejectionReasonRequired"]?.Value ?? "Rejection reason is required";
                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = SD.ReturnStatusRejected;
            returnRequest.RejectedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            returnRequest.RejectionReason = rejectionReason;
            returnRequest.AdminNotes = adminNotes;

            _unitOfWork.ReturnRequest.Update(returnRequest);
            _unitOfWork.save();

            // Send notification to customer
            await _notificationService.SendReturnRequestStatusUpdateToCustomer(returnRequest);

            TempData["success"] = _localizer["ReturnRequestRejected"]?.Value ?? "Return request rejected";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsReceived(int id)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            if (returnRequest.Status != SD.ReturnStatusApproved && returnRequest.Status != SD.ReturnStatusProcessing)
            {
                TempData["error"] = _localizer["ReturnRequestCannotBeMarkedAsReceived"]?.Value ?? "Only approved or processing return requests can be marked as received";
                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = SD.ReturnStatusProcessing;
            returnRequest.ReturnReceivedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;

            _unitOfWork.ReturnRequest.Update(returnRequest);
            _unitOfWork.save();

            // Send notification to customer
            await _notificationService.SendReturnRequestStatusUpdateToCustomer(returnRequest);

            TempData["success"] = _localizer["ReturnMarkedAsReceived"]?.Value ?? "Return marked as received";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id, string? refundTransactionId)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            if (returnRequest.Status != SD.ReturnStatusProcessing)
            {
                TempData["error"] = _localizer["ReturnRequestCannotBeCompleted"]?.Value ?? "Only processing return requests can be completed";
                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = SD.ReturnStatusCompleted;
            returnRequest.CompletedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            returnRequest.RefundStatus = SD.RefundStatusProcessed;
            returnRequest.RefundProcessedDate = IdealWeightNutrition.Utility.DateTimeHelper.Now;
            
            if (!string.IsNullOrEmpty(refundTransactionId))
            {
                returnRequest.RefundTransactionId = refundTransactionId;
            }

            _unitOfWork.ReturnRequest.Update(returnRequest);
            _unitOfWork.save();

            // Restore stock for returned items
            await _stockService.ProcessReturnStockRestoration(returnRequest.Id);

            // Update order status - use the already loaded OrderHeader from returnRequest or use UpdateStatus
            if (returnRequest.OrderHeader != null)
            {
                returnRequest.OrderHeader.OrderStatus = SD.StatusReturned;
                if (returnRequest.OrderHeader.PaymentStatus != SD.PaymentStatusRefunded)
                {
                    returnRequest.OrderHeader.PaymentStatus = SD.PaymentStatusRefunded;
                }
                _unitOfWork.OrderHeader.Update(returnRequest.OrderHeader);
                _unitOfWork.save();
            }
            else
            {
                // If OrderHeader wasn't loaded, use UpdateStatus method which handles tracking properly
                await _unitOfWork.OrderHeader.UpdateStatus(returnRequest.OrderHeaderId, SD.StatusReturned, SD.PaymentStatusRefunded);
            }

            // Send notification to customer
            await _notificationService.SendReturnRequestStatusUpdateToCustomer(returnRequest);

            TempData["success"] = _localizer["ReturnRequestCompleted"]?.Value ?? "Return request completed and refund processed";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
        {
            var returnRequest = _unitOfWork.ReturnRequest.GetWithItems(id);
            
            if (returnRequest == null)
            {
                TempData["error"] = _localizer["ReturnRequestNotFound"]?.Value ?? "Return request not found";
                return RedirectToAction(nameof(Index));
            }

            if (returnRequest.Status == SD.ReturnStatusCompleted || returnRequest.Status == SD.ReturnStatusRejected)
            {
                TempData["error"] = _localizer["ReturnRequestCannotBeCancelled"]?.Value ?? "This return request cannot be cancelled";
                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = SD.ReturnStatusCancelled;

            _unitOfWork.ReturnRequest.Update(returnRequest);
            _unitOfWork.save();

            TempData["success"] = _localizer["ReturnRequestCancelled"]?.Value ?? "Return request cancelled";
            return RedirectToAction(nameof(Details), new { id });
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status = "all")
        {
            IEnumerable<ReturnRequest> returnRequests;

            if (status == "all")
            {
                returnRequests = _unitOfWork.ReturnRequest.GetAll(
                    includeProperties: "OrderHeader,ApplicationUser"
                ).OrderByDescending(r => r.RequestDate); // Order by latest requests first
            }
            else
            {
                returnRequests = _unitOfWork.ReturnRequest.GetByStatus(status).OrderByDescending(r => r.RequestDate); // Order by latest requests first
            }

            var returnRequestData = returnRequests.Select(r => new
            {
                id = r.Id,
                orderId = r.OrderHeaderId,
                customerName = r.OrderHeader?.Name ?? r.ApplicationUser?.Name ?? "Guest",
                customerEmail = r.Email ?? r.ApplicationUser?.Email ?? "N/A",
                status = r.Status,
                requestDate = r.RequestDate.ToString("MMM dd, yyyy HH:mm"),
                requestDateSort = r.RequestDate.ToString("yyyy-MM-dd HH:mm:ss"), // For proper sorting
                refundAmount = r.RefundAmount ?? 0,
                reason = r.Reason.Length > 50 ? r.Reason.Substring(0, 50) + "..." : r.Reason
            }).OrderByDescending(_ => _.requestDateSort); // Order by latest requests first

            return Json(new { data = returnRequestData });
        }

        #endregion
    }
}

