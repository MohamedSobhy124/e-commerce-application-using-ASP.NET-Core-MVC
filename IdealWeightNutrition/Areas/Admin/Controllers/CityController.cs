using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CityController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;

        public CityController(IUnitOfWork unitOfWork, IStringLocalizer<IdealWeightNutrition.SharedResources> localizer)
        {
            _unitOfWork = unitOfWork;
            _localizer = localizer;
        }

        // GET: City
        public IActionResult Index()
        {
            var cities = _unitOfWork.City.GetAll()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToList();
            
            // Get remote areas count for each city
            var cityRemoteAreasCount = new Dictionary<int, int>();
            foreach (var city in cities)
            {
                cityRemoteAreasCount[city.Id] = _unitOfWork.RemoteArea.GetAll(ra => ra.CityId == city.Id).Count();
            }
            ViewBag.RemoteAreasCount = cityRemoteAreasCount;
            
            return View(cities);
        }

        // GET: City/Upsert
        public IActionResult Upsert(int? id)
        {
            City city = new City();
            
            if (id == null || id == 0)
            {
                // Create
                return View(city);
            }
            else
            {
                // Update
                city = _unitOfWork.City.Get(u => u.Id == id);
                if (city == null)
                {
                    return NotFound();
                }
                
                // Load remote areas for this city
                var remoteAreas = _unitOfWork.RemoteArea.GetAll(ra => ra.CityId == city.Id)
                    .OrderBy(ra => ra.DisplayOrder)
                    .ThenBy(ra => ra.Name)
                    .ToList();
                ViewBag.RemoteAreas = remoteAreas;
                
                return View(city);
            }
        }

        // POST: City/Upsert
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(City city)
        {
            ModelState.Remove("Id");
            
            if (ModelState.IsValid)
            {
                if (city.Id == 0)
                {
                    // Create
                    _unitOfWork.City.add(city);
                    _unitOfWork.save();
                    TempData["success"] = _localizer["CityCreatedSuccessfully"].Value ?? "City created successfully";
                }
                else
                {
                    // Update
                    _unitOfWork.City.Update(city);
                    _unitOfWork.save();
                    TempData["success"] = _localizer["CityUpdatedSuccessfully"].Value ?? "City updated successfully";
                }
                return RedirectToAction(nameof(Index));
            }
            return View(city);
        }

        // GET: City/Delete
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            
            var city = _unitOfWork.City.Get(u => u.Id == id);
            if (city == null)
            {
                return NotFound();
            }
            
            // Get remote areas count
            var remoteAreasCount = _unitOfWork.RemoteArea.GetAll(ra => ra.CityId == city.Id).Count();
            ViewBag.RemoteAreasCount = new Dictionary<int, int> { { city.Id, remoteAreasCount } };
            
            return View(city);
        }

        // POST: City/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int id)
        {
            var city = _unitOfWork.City.Get(u => u.Id == id);
            if (city == null)
            {
                return NotFound();
            }

            // Check if city has remote areas
            var remoteAreas = _unitOfWork.RemoteArea.GetAll(ra => ra.CityId == id).ToList();
            if (remoteAreas.Any())
            {
                TempData["error"] = _localizer["CannotDeleteCityHasRemoteAreas"].Value ?? "Cannot delete city. It has remote areas associated with it. Please delete remote areas first.";
                return RedirectToAction(nameof(Index));
            }

            _unitOfWork.City.remove(city);
            _unitOfWork.save();
            TempData["success"] = _localizer["CityDeletedSuccessfully"].Value ?? "City deleted successfully";
            return RedirectToAction(nameof(Index));
        }

        // API: Get Remote Areas for a City
        [HttpGet]
        public IActionResult GetRemoteAreas(int cityId)
        {
            var remoteAreas = _unitOfWork.RemoteArea.GetAll(ra => ra.CityId == cityId, includeProperties: "City")
                .OrderBy(ra => ra.DisplayOrder)
                .ThenBy(ra => ra.Name)
                .Select(ra => new
                {
                    id = ra.Id,
                    name = ra.Name,
                    nameAr = ra.NameAr ?? "",
                    deliveryCharge = ra.DeliveryCharge,
                    isActive = ra.IsActive,
                    displayOrder = ra.DisplayOrder
                })
                .ToList();

            return Json(new { success = true, data = remoteAreas });
        }

        // API: Toggle City Active Status
        [HttpPost]
        public IActionResult ToggleActive(int id)
        {
            var city = _unitOfWork.City.Get(u => u.Id == id);
            if (city == null)
            {
                return Json(new { success = false, message = _localizer["CityNotFound"].Value ?? "City not found" });
            }

            city.IsActive = !city.IsActive;
            _unitOfWork.City.Update(city);
            _unitOfWork.save();

            return Json(new { success = true, isActive = city.IsActive });
        }
    }
}
