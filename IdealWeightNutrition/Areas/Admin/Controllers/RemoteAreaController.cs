using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class RemoteAreaController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;

        public RemoteAreaController(IUnitOfWork unitOfWork, IStringLocalizer<IdealWeightNutrition.SharedResources> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        // GET: RemoteArea/Upsert
        public IActionResult Upsert(int? id, int? cityId)
        {
            RemoteArea remoteArea = new RemoteArea();
            
            if (id == null || id == 0)
            {
                // Create - require cityId
                if (cityId == null || cityId == 0)
                {
                    TempData["error"] = _localizer["CityIdRequiredForRemoteArea"].Value ?? "City ID is required to create a remote area";
                    return RedirectToAction("Index", "City");
                }
                
                var city = _unitOfWork.City.Get(c => c.Id == cityId);
                if (city == null)
                {
                    TempData["error"] = _localizer["CityNotFound"].Value ?? "City not found";
                    return RedirectToAction("Index", "City");
                }
                
                remoteArea.CityId = cityId.Value;
                ViewData["CityName"] = city.Name;
            }
            else
            {
                // Update
                remoteArea = _unitOfWork.RemoteArea.Get(ra => ra.Id == id, includeProperties: "City");
                if (remoteArea == null)
                {
                    return NotFound();
                }
                ViewData["CityName"] = remoteArea.City?.Name ?? "Unknown";
            }

            // Populate cities dropdown
            ViewData["CityId"] = new SelectList(
                _unitOfWork.City.GetAll().OrderBy(c => c.Name),
                "Id",
                "Name",
                remoteArea.CityId
            );

            return View(remoteArea);
        }

        // POST: RemoteArea/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(RemoteArea remoteArea)
        {
            ModelState.Remove("Id");
            ModelState.Remove("City");

            if (ModelState.IsValid)
            {
                if (remoteArea.Id == 0)
                {
                    // Create
                    _unitOfWork.RemoteArea.add(remoteArea);
                    _unitOfWork.save();
                    TempData["success"] = _localizer["RemoteAreaCreatedSuccessfully"].Value ?? "Remote area created successfully";
                }
                else
                {
                    // Update
                    _unitOfWork.RemoteArea.Update(remoteArea);
                    _unitOfWork.save();
                    TempData["success"] = _localizer["RemoteAreaUpdatedSuccessfully"].Value ?? "Remote area updated successfully";
                }
                return RedirectToAction("Upsert", "City", new { id = remoteArea.CityId });
            }

            // Repopulate dropdown on error
            ViewData["CityId"] = new SelectList(
                _unitOfWork.City.GetAll().OrderBy(c => c.Name),
                "Id",
                "Name",
                remoteArea.CityId
            );

            var city = _unitOfWork.City.Get(c => c.Id == remoteArea.CityId);
            ViewData["CityName"] = city?.Name ?? "Unknown";

            return View(remoteArea);
        }

        // GET: RemoteArea/Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            
            var remoteArea = _unitOfWork.RemoteArea.Get(ra => ra.Id == id, includeProperties: "City");
            if (remoteArea == null)
            {
                return NotFound();
            }
            
            return View(remoteArea);
        }

        // POST: RemoteArea/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int id)
        {
            var remoteArea = _unitOfWork.RemoteArea.Get(ra => ra.Id == id);
            if (remoteArea == null)
            {
                return NotFound();
            }

            var cityId = remoteArea.CityId;
            _unitOfWork.RemoteArea.remove(remoteArea);
            _unitOfWork.save();
            TempData["success"] = _localizer["RemoteAreaDeletedSuccessfully"].Value ?? "Remote area deleted successfully";
            return RedirectToAction("Upsert", "City", new { id = cityId });
        }
    }
}
