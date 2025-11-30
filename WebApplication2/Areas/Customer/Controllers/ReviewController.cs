using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace BulkyBook.Areas.Customer.Controllers
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

        public ReviewController(IUnitOfWork unitOfWork, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(int productId, int rating, string comment)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comment) || rating < 1 || rating > 5)
                {
                    TempData["error"] = "Please provide a valid rating and comment";
                    return RedirectToAction("Details", "Home", new { productId = productId });
                }

                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                // Check if user already reviewed this product
                var existingReview = _unitOfWork.review.Get(r => r.ProductId == productId && r.UserId == userId);
                
                if (existingReview != null)
                {
                    TempData["error"] = "You have already reviewed this product";
                    return RedirectToAction("Details", "Home", new { productId = productId });
                }

                // Check if user purchased the product (optional - can skip for testing)
                var hasPurchased = false;
                try
                {
                    hasPurchased = _unitOfWork.OrderDetail.GetAll(
                        od => od.ProductId == productId,
                        includeProperties: "OrderHeader"
                    ).Any(od => od.OrderHeader != null && 
                                od.OrderHeader.ApplicationUserId == userId && 
                                od.OrderHeader.OrderStatus == SD.StatusDelivered);
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
                    CreatedAt = BulkyBook.Utility.DateTimeHelper.Now,
                    HelpfulCount = 0
                };

                _unitOfWork.review.add(review);
                _unitOfWork.save();

                TempData["success"] = "Your review has been submitted successfully!";
                return RedirectToAction("Details", "Home", new { productId = productId });
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error submitting review: {ex.Message}";
                return RedirectToAction("Details", "Home", new { productId = productId });
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
            if (productIds == null || !productIds.Any())
            {
                return Json(new { ratings = new Dictionary<int, object>() });
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

            return Json(new { ratings = result });
        }
    }
}

