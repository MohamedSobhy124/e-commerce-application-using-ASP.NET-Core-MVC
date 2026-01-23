// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDBContext _db;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDBContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required]
            [Display(Name = "Full Name")]
            [StringLength(100)]
            public string Name { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Street Address")]
            [StringLength(200)]
            public string StreetAddress { get; set; }

            [Display(Name = "City")]
            [StringLength(100)]
            public string City { get; set; }

            [Display(Name = "State")]
            [StringLength(100)]
            public string State { get; set; }

            [Display(Name = "Postal Code")]
            [StringLength(20)]
            public string PostalCode { get; set; }
        }

        private async Task LoadAsync(IdentityUser user)
        {
            var userName = await _userManager.GetUserNameAsync((ApplicationUser)user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync((ApplicationUser)user);

            Username = userName;

            // Try to load ApplicationUser data (for custom fields)
            var appUser = await _db.applicationUsers.FindAsync(user.Id);

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Name = appUser?.Name ?? "",
                StreetAddress = appUser?.StreetAddress ?? "",
                City = appUser?.City ?? "",
                State = appUser?.State ?? "",
                PostalCode = appUser?.PostalCode ?? ""
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // Update phone number via UserManager
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update ApplicationUser fields (Name and Address)
            // Use raw SQL to update custom columns directly in AspNetUsers table
            // This works for both IdentityUser (Google) and ApplicationUser records
            await _db.Database.ExecuteSqlRawAsync(
                @"UPDATE AspNetUsers 
                  SET Name = {0}, 
                      StreetAddress = {1}, 
                      City = {2}, 
                      State = {3}, 
                      PostalCode = {4},
                      Discriminator = 'ApplicationUser'
                  WHERE Id = {5}",
                Input.Name ?? "",
                Input.StreetAddress ?? "",
                Input.City ?? "",
                Input.State ?? "",
                Input.PostalCode ?? "",
                user.Id
            );
            await _signInManager.RefreshSignInAsync(user);
            
            StatusMessage = "Your profile has been updated successfully";
            return RedirectToPage();
        }
    }
}
