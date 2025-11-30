using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ServiceOfferController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ServiceOfferController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResources> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        // GET: ServiceOffer/Index
        public IActionResult Index()
        {
            var offers = _unitOfWork.ServiceOffers.GetAll(includeProperties: "ServiceSubscription,PromoCode")
                .OrderByDescending(o => o.CreatedDate);
            return View(offers);
        }

        // GET: ServiceOffer/Create
        public IActionResult Create(int? serviceId = null)
        {
            var offer = new ServiceOffer
            {
                StartDate = BulkyBook.Utility.DateTimeHelper.Now,
                EndDate = BulkyBook.Utility.DateTimeHelper.Now.AddDays(30),
                IsActive = true,
                DiscountType = DiscountType.Percentage
            };

            if (serviceId.HasValue)
            {
                offer.ServiceSubscriptionId = serviceId.Value;
            }

            ViewBag.ServiceSubscriptionList = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Text = s.Title,
                    Value = s.Id.ToString()
                }).ToList();

            ViewBag.PromoCodeList = _unitOfWork.PromoCode.GetAll(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Text = $"{p.Code} - {p.Description}",
                    Value = p.Id.ToString()
                }).ToList();

            return View(offer);
        }

        // POST: ServiceOffer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ServiceOffer offer)
        {
            if (offer.EndDate <= offer.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (offer.DiscountType == DiscountType.Percentage && offer.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%");
            }

            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                offer.CreatedBy = userId;
                offer.CreatedDate = BulkyBook.Utility.DateTimeHelper.Now;

                _unitOfWork.ServiceOffers.Add(offer);
                _unitOfWork.save();

                TempData["success"] = "Service offer created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ServiceSubscriptionList = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Text = s.Title,
                    Value = s.Id.ToString(),
                    Selected = s.Id == offer.ServiceSubscriptionId
                }).ToList();

            ViewBag.PromoCodeList = _unitOfWork.PromoCode.GetAll(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Text = $"{p.Code} - {p.Description}",
                    Value = p.Id.ToString(),
                    Selected = p.Id == offer.PromoCodeId
                }).ToList();

            return View(offer);
        }

        // GET: ServiceOffer/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var offer = _unitOfWork.ServiceOffers.Get(o => o.Id == id);
            
            if (offer == null)
            {
                return NotFound();
            }

            ViewBag.ServiceSubscriptionList = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Text = s.Title,
                    Value = s.Id.ToString(),
                    Selected = s.Id == offer.ServiceSubscriptionId
                }).ToList();

            ViewBag.PromoCodeList = _unitOfWork.PromoCode.GetAll(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Text = $"{p.Code} - {p.Description}",
                    Value = p.Id.ToString(),
                    Selected = p.Id == offer.PromoCodeId
                }).ToList();

            return View(offer);
        }

        // POST: ServiceOffer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ServiceOffer offer)
        {
            if (offer.EndDate <= offer.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (offer.DiscountType == DiscountType.Percentage && offer.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%");
            }

            if (ModelState.IsValid)
            {
                _unitOfWork.ServiceOffers.Update(offer);
                _unitOfWork.save();

                TempData["success"] = "Service offer updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ServiceSubscriptionList = _unitOfWork.ServiceSubscriptions.GetAll(s => s.IsActive)
                .Select(s => new SelectListItem
                {
                    Text = s.Title,
                    Value = s.Id.ToString(),
                    Selected = s.Id == offer.ServiceSubscriptionId
                }).ToList();

            ViewBag.PromoCodeList = _unitOfWork.PromoCode.GetAll(p => p.IsActive)
                .Select(p => new SelectListItem
                {
                    Text = $"{p.Code} - {p.Description}",
                    Value = p.Id.ToString(),
                    Selected = p.Id == offer.PromoCodeId
                }).ToList();

            return View(offer);
        }

        // GET: ServiceOffer/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var offer = _unitOfWork.ServiceOffers.Get(o => o.Id == id, includeProperties: "ServiceSubscription");
            
            if (offer == null)
            {
                return NotFound();
            }

            return View(offer);
        }

        // POST: ServiceOffer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var offer = _unitOfWork.ServiceOffers.Get(o => o.Id == id);
            
            if (offer == null)
            {
                return NotFound();
            }

            _unitOfWork.ServiceOffers.Remove(offer);
            _unitOfWork.save();

            TempData["success"] = "Service offer deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

