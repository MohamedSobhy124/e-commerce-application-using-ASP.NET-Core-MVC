// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using IdealWeightNutrition.Areas.Customer.Controllers;

namespace IdealWeightNutrition.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
            public string? Role { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem>RoleList { get; set; }
            [Required]
            public string Name { get; set; }
            public string? StreetAddress { get; set; }
            public string? City { get; set; }
            public string? State { get; set; }
            public string? PostalCode { get; set; }
            public string? PhoneNumber { get; set; }
            public int? CompanyId { get; set; }
            public IEnumerable<SelectListItem> CompanyList { get; set; }


        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
            }

            Input = new()
            {
                RoleList = _roleManager.Roles.Select(a => a.Name).Select(i => new SelectListItem
                {
                    Text = i,
                    Value = i
                }),
                CompanyList = _unitOfWork.company.GetAll().Select(s => new SelectListItem
                {
                    Text = s.Name,
                    Value = s.Id.ToString()
                })
            };

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        // OTP Handler Methods
        public async Task<IActionResult> OnPostSendOtpAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { success = false, message = "Email is required" });
            }

            email = email.Trim().ToLowerInvariant();

            // Validate email format
            var emailRegex = new System.Text.RegularExpressions.Regex(
                @"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (!emailRegex.IsMatch(email))
            {
                return new JsonResult(new { success = false, message = "Please enter a valid email address" });
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                return new JsonResult(new { success = false, message = "This email is already registered. Please use a different email or sign in." });
            }

            try
            {
                var otpHelper = new OtpHelper(_memoryCache);
                var otp = otpHelper.GenerateOtp();
                otpHelper.StoreOtp(email, otp);

                // Send OTP email
                var emailSubject = "Email Verification Code - Ideal Weight";
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9fafb;'>
                        <div style='background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <h2 style='color: #059669; margin-top: 0;'>Email Verification</h2>
                            <p style='color: #374151; font-size: 16px; line-height: 1.6;'>
                                Thank you for registering with Ideal Weight Nutrition. To complete your registration, please verify your email address using the code below:
                            </p>
                            <div style='background: linear-gradient(135deg, #059669 0%, #047857 100%); color: white; padding: 20px; border-radius: 8px; text-align: center; margin: 30px 0;'>
                                <div style='font-size: 36px; font-weight: bold; letter-spacing: 8px; font-family: monospace;'>{otp}</div>
                            </div>
                            <p style='color: #6b7280; font-size: 14px; margin-top: 20px;'>
                                <strong>Important:</strong> This code will expire in 10 minutes. If you didn't request this code, please ignore this email.
                            </p>
                            <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;' />
                            <p style='color: #9ca3af; font-size: 12px; text-align: center; margin: 0;'>
                                © {DateTime.Now.Year} Ideal Weight Nutrition. All rights reserved.
                            </p>
                        </div>
                    </div>";

                await _emailSender.SendEmailAsync(email, emailSubject, emailBody);

                return new JsonResult(new 
                { 
                    success = true, 
                    message = "Verification code sent to your email. Please check your inbox." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {Email}", email);
                return new JsonResult(new { success = false, message = "Error sending verification code. Please try again." });
            }
        }

        public IActionResult OnPostVerifyOtpAsync(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return new JsonResult(new { success = false, message = "Email and OTP are required" });
            }

            email = email.Trim().ToLowerInvariant();
            otp = otp.Trim();

            try
            {
                var otpHelper = new OtpHelper(_memoryCache);
                var result = otpHelper.VerifyOtp(email, otp);

                if (result.IsValid)
                {
                    return new JsonResult(new { success = true, message = "Email verified successfully!" });
                }
                else
                {
                    return new JsonResult(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", email);
                return new JsonResult(new { success = false, message = "Error verifying code. Please try again." });
            }
        }

        public IActionResult OnPostCheckEmailVerifiedAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { verified = false });
            }

            email = email.Trim().ToLowerInvariant();
            var otpHelper = new OtpHelper(_memoryCache);
            var isVerified = otpHelper.IsEmailVerified(email);

            return new JsonResult(new { verified = isVerified });
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                // Check if email is verified via OTP
                if (!string.IsNullOrWhiteSpace(Input.Email))
                {
                    var email = Input.Email.Trim().ToLowerInvariant();
                    var otpHelper = new OtpHelper(_memoryCache);
                    if (!otpHelper.IsEmailVerified(email))
                    {
                        ModelState.AddModelError(string.Empty, "Please verify your email address before registering.");
                        return Page();
                    }
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                user.City=Input.City;
                user.Name = Input.Name;
                user.StreetAddress=Input.StreetAddress;
                user.PostalCode = Input.PostalCode;
                user.State = Input.State;
                user.PhoneNumber = Input.PhoneNumber;
                if(Input.Role== SD.Role_Company)
                {
                    user.CompanyId= Input.CompanyId;        
                }
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    if(! string.IsNullOrEmpty(Input.Role))
                    {
                            await _userManager.AddToRoleAsync(user, Input.Role);    
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user,SD.Role_Customer);

                    }

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        
                        // Merge guest cart into user cart after registration
                        var mergedCount = CartController.MergeGuestCartToUserCart(_unitOfWork, user.Id, HttpContext.Session);
                        if (mergedCount > 0)
                        {
                            _logger.LogInformation($"Merged {mergedCount} items from guest cart to user cart for new user {user.Id}");
                        }
                        
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
