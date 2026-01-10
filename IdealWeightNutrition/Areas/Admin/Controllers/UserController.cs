using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public IActionResult CreateAdminUser()
        {
         
            ViewBag.Companies = _unitOfWork.company.GetAll().Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdminUser(string email, string name, string password, 
            string phoneNumber, string role=SD.Role_Admin, int? companyId=null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || 
                string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(role))
            {
                TempData["error"] = "Please fill all required fields";
                return RedirectToAction(nameof(CreateAdminUser));
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = name,
                PhoneNumber = phoneNumber,
                Role = role
            };

            // Add company if role is Company
            if (role == SD.Role_Company && companyId.HasValue)
            {
                user.CompanyId = companyId.Value;
            }

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assign role
                await _userManager.AddToRoleAsync(user, role);

                TempData["success"] = $"{role} user created successfully!";
                return RedirectToAction(nameof(CreateAdminUser));
            }

            foreach (var error in result.Errors)
            {
                TempData["error"] = error.Description;
                break;
            }

            return RedirectToAction(nameof(CreateAdminUser));
        }
    }
}

