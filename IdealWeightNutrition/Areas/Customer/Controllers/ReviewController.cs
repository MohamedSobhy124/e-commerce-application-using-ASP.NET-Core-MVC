using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace IdealWeightNutrition.Areas.Customer.Controllers
{
    // Helper class for raw SQL query results
    public class ApplicationUserRaw
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
    }

    [Area("Customer")]
    public class ReviewController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;

        public ReviewController(IUnitOfWork unitOfWork, ApplicationDBContext dbContext, IConfiguration configuration, IStringLocalizer<IdealWeightNutrition.SharedResources> localizer)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _configuration = configuration;
            _localizer = localizer;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(int productId, int rating, string comment)
        {
            try
            {
                // Get product to retrieve slug for redirect
                var product = _unitOfWork.product.Get(p => p.Id == productId && !p.IsDeleted);
                if (product == null)
                {
                    TempData["error"] = _localizer["ProductNotFound"].Value;
                    return RedirectToAction("Index", "Home");
                }

                // Get current culture to determine which slug to use
                var requestCulture = Request.HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
                var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
                var slug = ( product.SlugEn ?? product.Id.ToString()) ;

                if (string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
                {
                    TempData["error"] = _localizer["PleaseProvideValidRatingAndComment"].Value;
                    return RedirectToAction("Details", "Home", new { slug = slug });
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Check if user already reviewed this product
                var existingReview = _unitOfWork.review.Get(r => r.ProductId == productId && r.UserId == userId);
                
                if (existingReview != null)
                {
                    TempData["error"] = _localizer["YouHaveAlreadyReviewedThisProduct"].Value;
                    return RedirectToAction("Details", "Home", new { slug = slug });
                }

                // Check if user purchased the product using the configuration setting
                var hasPurchased = false;
                try
                {
                    var enableReviewWithoutOrder = bool.Parse(_configuration["SiteSettings:EnableReviewWithoutOrder"] ?? "false");
                    
                    if (enableReviewWithoutOrder)
                    {
                        // If reviews are enabled without order, consider all users as having purchased
                        hasPurchased = true;
                    }
                    else
                    {
                        // Check if user actually purchased the product
                        hasPurchased = _unitOfWork.OrderDetail.GetAll(
                            od => od.ProductId == productId,
                            includeProperties: "OrderHeader"
                        ).Any(od => od.OrderHeader != null && 
                                    od.OrderHeader.ApplicationUserId == userId && 
                                    od.OrderHeader.OrderStatus == SD.StatusDelivered);
                    }
                }
                catch
                {
                    // If error checking purchase, just set to false
                    hasPurchased = false;
                }

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    IsVerifiedPurchase = hasPurchased,
                    IsApproved = true, // Auto-approve for now (change to false for moderation)
                    CreatedAt = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                    HelpfulCount = 0
                };

                _unitOfWork.review.add(review);
                _unitOfWork.save();

                TempData["success"] = _localizer["ReviewSubmittedSuccessfully"].Value;
                return RedirectToAction("Details", "Home", new { slug = slug });
            }
            catch (Exception ex)
            {
                TempData["error"] = _localizer["ErrorSubmittingReview"].Value + ": " + ex.Message;
                // Try to get product slug for redirect, fallback to Index if not possible
                try
                {
                    var product = _unitOfWork.product.Get(p => p.Id == productId && !p.IsDeleted);
                    if (product != null)
                    {
                        var requestCulture = Request.HttpContext.Features.Get<Microsoft.AspNetCore.Localization.IRequestCultureFeature>();
                        var currentCulture = requestCulture?.RequestCulture.Culture.Name ?? "en";
                        var slug =   (product.SlugEn ?? product.Id.ToString());
                        return RedirectToAction("Details", "Home", new { slug = slug });
                    }
                }
                catch { }
                
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReviews(int productId)
        {
            try
            {
                // Get reviews first without including User to avoid Discriminator filter
                var reviews = _unitOfWork.review.GetAll(
                    r => r.ProductId == productId && r.IsApproved
                ).OrderByDescending(r => r.CreatedAt).ToList();

                if (!reviews.Any())
                {
                    return Json(new { success = true, reviews = new List<object>() });
                }

                // Get all user IDs from reviews
                var userIds = reviews.Select(r => r.UserId).Distinct().Where(id => !string.IsNullOrEmpty(id)).ToList();

                if (!userIds.Any())
                {
                    // If no user IDs, return reviews without user names
                    var reviewDataWithoutUsers = reviews.Select(r => new
                    {
                        id = r.Id,
                        userName = "Anonymous",
                        rating = r.Rating,
                        comment = r.Comment,
                        date = r.CreatedAt.ToString("MMM dd, yyyy"),
                        isVerifiedPurchase = r.IsVerifiedPurchase,
                        helpfulCount = r.HelpfulCount
                    }).ToList();

                    return Json(new { success = true, reviews = reviewDataWithoutUsers });
                }

                // Load users using applicationUser repository directly (this should work without Discriminator filter)
                // If that doesn't work, query AspNetUsers using raw SQL
                Dictionary<string, ApplicationUser> userDict;
                
                try
                {
                    // If repository doesn't work or filters by Discriminator, use raw SQL
                    // Build SQL query with parameters to get users directly from AspNetUsers without Discriminator filter
                    var sqlQuery = $"SELECT Id, UserName, Name FROM AspNetUsers WHERE Id IN ({string.Join(",", userIds.Select((id, i) => $"@p{i}"))})";

                    // Create SQL parameters for security (prevents SQL injection)
                    var parameters = userIds.Select((id, i) => new SqlParameter($"@p{i}", id)).ToArray();

                    // Execute raw SQL using Database connection
                    var connection = _dbContext.Database.GetDbConnection();
                    var wasOpen = connection.State == System.Data.ConnectionState.Open;
                    if (!wasOpen) await connection.OpenAsync();

                    try
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = sqlQuery;
                        command.Parameters.AddRange(parameters);

                        var userResults = new List<ApplicationUserRaw>();
                        using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            userResults.Add(new ApplicationUserRaw
                            {
                                Id = reader.GetString(0),
                                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }

                        // Convert to dictionary
                        userDict = userResults.ToDictionary(u => u.Id, u => new ApplicationUser
                        {
                            Id = u.Id,
                            UserName = u.UserName ?? "",
                            Name = u.Name
                        });
                    }
                    finally
                    {
                        if (!wasOpen) await connection.CloseAsync();
                    }
                }
                catch
                {
                    // If repository doesn't work or filters by Discriminator, use raw SQL
                    // Build SQL query with parameters to get users directly from AspNetUsers without Discriminator filter
                    var sqlQuery = $"SELECT Id, UserName, Name FROM AspNetUsers WHERE Id IN ({string.Join(",", userIds.Select((id, i) => $"@p{i}"))})";
                    
                    // Create SQL parameters for security (prevents SQL injection)
                    var parameters = userIds.Select((id, i) => new SqlParameter($"@p{i}", id)).ToArray();
                    
                    // Execute raw SQL using Database connection
                    var connection = _dbContext.Database.GetDbConnection();
                    var wasOpen = connection.State == System.Data.ConnectionState.Open;
                    if (!wasOpen) await connection.OpenAsync();
                    
                    try
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = sqlQuery;
                        command.Parameters.AddRange(parameters);
                        
                        var userResults = new List<ApplicationUserRaw>();
                        using var reader = await command.ExecuteReaderAsync();
                        
                        while (await reader.ReadAsync())
                        {
                            userResults.Add(new ApplicationUserRaw
                            {
                                Id = reader.GetString(0),
                                UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Name = reader.IsDBNull(2) ? null : reader.GetString(2)
                            });
                        }
                        
                        // Convert to dictionary
                        userDict = userResults.ToDictionary(u => u.Id, u => new ApplicationUser 
                        { 
                            Id = u.Id, 
                            UserName = u.UserName ?? "", 
                            Name = u.Name 
                        });
                    }
                    finally
                    {
                        if (!wasOpen) await connection.CloseAsync();
                    }
                }


                // Map reviews with user information
                var reviewData = reviews.Select(r => new
                {
                    id = r.Id,
                    userName = userDict.ContainsKey(r.UserId) 
                        ? (userDict[r.UserId].Name ?? userDict[r.UserId].UserName ?? "Anonymous") 
                        : "Anonymous",
                    rating = r.Rating,
                    comment = r.Comment ?? "",
                    date = r.CreatedAt.ToString("MMM dd, yyyy"),
                    isVerifiedPurchase = r.IsVerifiedPurchase,
                    helpfulCount = r.HelpfulCount
                }).ToList();

                return Json(new { success = true, reviews = reviewData });
            }
            catch (Exception ex)
            {
                // Log error and return empty reviews
                return Json(new { success = false, reviews = new List<object>(), error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetProductRating(int productId)
        {
            var averageRating = _unitOfWork.review.GetAverageRating(productId);
            var reviewCount = _unitOfWork.review.GetReviewCount(productId);

            return Json(new
            {
                averageRating = averageRating,
                reviewCount = reviewCount
            });
        }

        [HttpPost]
        public IActionResult GetBatchProductRatings([FromBody] List<int> productIds)
        {
            try
            {
                if (productIds == null || !productIds.Any())
                {
                    return Json(new { ratings = new Dictionary<int, object>(), debug = "No product IDs provided" });
                }

                var ratings = _unitOfWork.review.GetBatchProductRatings(productIds);
                
                // Convert to a format that's easier to work with in JavaScript
                var result = ratings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        averageRating = kvp.Value.averageRating,
                        reviewCount = kvp.Value.reviewCount
                    }
                );

                // Add debug info
                var totalReviews = _dbContext.Reviews.Count();
                var approvedReviews = _dbContext.Reviews.Count(r => r.IsApproved);

                return Json(new { 
                    ratings = result,
                    debug = new {
                        productIdsRequested = productIds.Count,
                        totalReviewsInDb = totalReviews,
                        approvedReviewsInDb = approvedReviews,
                        ratingsReturned = result.Count
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    ratings = new Dictionary<int, object>(),
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Test endpoint to check reviews in database
        [HttpGet]
        public IActionResult TestReviews()
        {
            try
            {
                var allReviews = _dbContext.Reviews.ToList();
                var approvedReviews = allReviews.Where(r => r.IsApproved).ToList();
                
                return Json(new {
                    success = true,
                    totalReviews = allReviews.Count,
                    approvedReviews = approvedReviews.Count,
                    reviews = allReviews.Select(r => new {
                        r.Id,
                        r.ProductId,
                        r.ServiceSubscriptionId,
                        r.UserId,
                        r.Rating,
                        r.Comment,
                        r.IsApproved,
                        r.IsVerifiedPurchase,
                        r.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        #region Service Review Methods

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitServiceReview(int serviceId, int rating, string comment)
        {
            try
            {
                // Get service to validate it exists
                var service = _unitOfWork.ServiceSubscriptions.Get(s => s.Id == serviceId && s.IsActive);
                if (service == null)
                {
                    TempData["error"] = _localizer["ServiceNotFound"].Value ?? "Service not found";
                    return RedirectToAction("Index", "ServiceSubscription");
                }

                if (string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
                {
                    TempData["error"] = _localizer["PleaseProvideValidRatingAndComment"].Value ?? "Please provide a valid rating and comment";
                    return RedirectToAction("Details", "ServiceSubscription", new { id = serviceId });
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Check if user already reviewed this service
                var existingReview = _unitOfWork.review.Get(r => r.ServiceSubscriptionId == serviceId && r.UserId == userId);
                
                if (existingReview != null)
                {
                    TempData["error"] = _localizer["YouHaveAlreadyReviewedThisService"].Value ?? "You have already reviewed this service";
                    return RedirectToAction("Details", "ServiceSubscription", new { id = serviceId });
                }

                // Check if user purchased the service
                var hasPurchased = false;
                try
                {
                    var enableReviewWithoutOrder = bool.Parse(_configuration["SiteSettings:EnableReviewWithoutOrder"] ?? "false");
                    
                    if (enableReviewWithoutOrder)
                    {
                        hasPurchased = true;
                    }
                    else
                    {
                        // Check if user actually purchased the service
                        hasPurchased = _dbContext.ServicePurchases.Any(
                            sp => sp.ServiceSubscriptionId == serviceId && 
                                  sp.ApplicationUserId == userId && 
                                  sp.PaymentStatus == "Paid");
                    }
                }
                catch
                {
                    hasPurchased = false;
                }

                var review = new Review
                {
                    ServiceSubscriptionId = serviceId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment,
                    IsVerifiedPurchase = hasPurchased,
                    IsApproved = true, // Auto-approve for now
                    CreatedAt = IdealWeightNutrition.Utility.DateTimeHelper.Now,
                    HelpfulCount = 0
                };

                _unitOfWork.review.add(review);
                _unitOfWork.save();

                TempData["success"] = _localizer["ReviewSubmittedSuccessfully"].Value ?? "Review submitted successfully";
                return RedirectToAction("Details", "ServiceSubscription", new { id = serviceId });
            }
            catch (Exception ex)
            {
                TempData["error"] = (_localizer["ErrorSubmittingReview"].Value ?? "Error submitting review") + ": " + ex.Message;
                return RedirectToAction("Details", "ServiceSubscription", new { id = serviceId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceReviews(int serviceId)
        {
            try
            {
                var reviews = _unitOfWork.review.GetApprovedReviewsByService(serviceId).ToList();

                if (!reviews.Any())
                {
                    return Json(new { reviews = new List<object>() });
                }

                // Get all user IDs from reviews
                var userIds = reviews.Select(r => r.UserId).Distinct().Where(id => !string.IsNullOrEmpty(id)).ToList();

                if (!userIds.Any())
                {
                    // If no user IDs, return reviews without user names
                    var reviewDataWithoutUsers = reviews.Select(r => new
                    {
                        id = r.Id,
                        userName = "Anonymous",
                        rating = r.Rating,
                        comment = r.Comment,
                        date = r.CreatedAt.ToString("MMM dd, yyyy"),
                        isVerifiedPurchase = r.IsVerifiedPurchase,
                        helpfulCount = r.HelpfulCount
                    }).ToList();

                    return Json(new { reviews = reviewDataWithoutUsers });
                }

                // Build SQL query with parameters
                var sqlQuery = $"SELECT Id, UserName, Name FROM AspNetUsers WHERE Id IN ({string.Join(",", userIds.Select((id, i) => $"@p{i}"))})";
                var parameters = userIds.Select((id, i) => new SqlParameter($"@p{i}", id)).ToArray();

                // Execute raw SQL using Database connection
                var connection = _dbContext.Database.GetDbConnection();
                var wasOpen = connection.State == System.Data.ConnectionState.Open;
                if (!wasOpen) await connection.OpenAsync();

                Dictionary<string, ApplicationUserRaw> userDict;
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = sqlQuery;
                    command.Parameters.AddRange(parameters);

                    var userResults = new List<ApplicationUserRaw>();
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        userResults.Add(new ApplicationUserRaw
                        {
                            Id = reader.GetString(0),
                            UserName = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Name = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }

                    userDict = userResults.ToDictionary(u => u.Id);
                }
                finally
                {
                    if (!wasOpen) await connection.CloseAsync();
                }

                var reviewData = reviews.Select(r => new
                {
                    id = r.Id,
                    userName = userDict.ContainsKey(r.UserId) ? (userDict[r.UserId].Name ?? userDict[r.UserId].UserName ?? "Anonymous") : "Anonymous",
                    rating = r.Rating,
                    comment = r.Comment,
                    date = r.CreatedAt.ToString("MMM dd, yyyy"),
                    isVerifiedPurchase = r.IsVerifiedPurchase,
                    helpfulCount = r.HelpfulCount
                }).ToList();

                return Json(new { reviews = reviewData });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                Console.WriteLine($"Error loading service reviews: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public IActionResult GetServiceRating(int serviceId)
        {
            try
            {
                var averageRating = _unitOfWork.review.GetAverageServiceRating(serviceId);
                var reviewCount = _unitOfWork.review.GetServiceReviewCount(serviceId);

                return Json(new
                {
                    averageRating = averageRating,
                    reviewCount = reviewCount,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    averageRating = 0,
                    reviewCount = 0,
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult GetBatchServiceRatings([FromBody] List<int> serviceIds)
        {
            try
            {
                if (serviceIds == null || !serviceIds.Any())
                {
                    return Json(new { ratings = new Dictionary<int, object>() });
                }

                var ratings = _unitOfWork.review.GetBatchServiceRatings(serviceIds);

                var result = ratings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        averageRating = kvp.Value.averageRating,
                        reviewCount = kvp.Value.reviewCount
                    }
                );

                return Json(new
                {
                    ratings = result,
                    success = true
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    ratings = new Dictionary<int, object>(),
                    error = ex.Message
                });
            }
        }

        #endregion
    }
}

