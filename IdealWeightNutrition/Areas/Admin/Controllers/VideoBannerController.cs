using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class VideoBannerController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<IdealWeightNutrition.SharedResources> _localizer;
        private readonly ILogger<VideoBannerController> _logger;

        public VideoBannerController(
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizer<IdealWeightNutrition.SharedResources> localizer,
            ILogger<VideoBannerController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
            _logger = logger;
        }

        // GET: Admin/VideoBanner
        public IActionResult Index()
        {
            var videoPath = Path.Combine(_webHostEnvironment.WebRootPath, "videos", "home-banner.mp4");
            var posterPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "video-banner-poster.jpg");
            
            ViewBag.HasVideo = System.IO.File.Exists(videoPath);
            ViewBag.HasPoster = System.IO.File.Exists(posterPath);
            
            if (ViewBag.HasVideo)
            {
                var fileInfo = new FileInfo(videoPath);
                ViewBag.VideoSize = fileInfo.Length;
                ViewBag.VideoSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                ViewBag.VideoLastModified = fileInfo.LastWriteTime;
            }
            
            if (ViewBag.HasPoster)
            {
                var posterInfo = new FileInfo(posterPath);
                ViewBag.PosterSize = posterInfo.Length;
                ViewBag.PosterSizeKB = Math.Round(posterInfo.Length / 1024.0, 2);
                ViewBag.PosterLastModified = posterInfo.LastWriteTime;
            }
            
            return View();
        }

        // POST: Admin/VideoBanner/UploadVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)] // 50MB limit
        public async Task<IActionResult> UploadVideo(IFormFile videoFile)
        {
            if (videoFile == null || videoFile.Length == 0)
            {
                TempData["error"] = _localizer["PleaseSelectVideoFile"].ToString();
                return RedirectToAction(nameof(Index));
            }

            // Validate file type
            var allowedExtensions = new[] { ".mp4", ".webm", ".mov" };
            var fileExtension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["error"] = _localizer["InvalidVideoFormat"].ToString();
                return RedirectToAction(nameof(Index));
            }

            // Validate file size (max 50MB)
            const long maxFileSize = 50 * 1024 * 1024; // 50MB
            if (videoFile.Length > maxFileSize)
            {
                TempData["error"] = _localizer["VideoFileTooLarge"].ToString();
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Ensure videos directory exists
                var videosPath = Path.Combine(_webHostEnvironment.WebRootPath, "videos");
                if (!Directory.Exists(videosPath))
                {
                    Directory.CreateDirectory(videosPath);
                }

                // Delete old video if exists
                var videoPath = Path.Combine(videosPath, "home-banner.mp4");
                if (System.IO.File.Exists(videoPath))
                {
                    try
                    {
                        System.IO.File.Delete(videoPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Could not delete old video file, will overwrite");
                    }
                }

                // Save new video with buffer for better performance
                using (var fileStream = new FileStream(videoPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                {
                    await videoFile.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }

                TempData["success"] = _localizer["VideoUploadedSuccessfully"].ToString();
                _logger.LogInformation("Video banner uploaded successfully by user: {UserId}, Size: {Size}MB", 
                    User.Identity?.Name, Math.Round(videoFile.Length / (1024.0 * 1024.0), 2));
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error uploading video banner - File may be locked or disk full");
                TempData["error"] = _localizer["ErrorUploadingVideo"].ToString() + " (IO Error)";
            }
            catch (UnauthorizedAccessException authEx)
            {
                _logger.LogError(authEx, "Permission denied uploading video banner");
                TempData["error"] = _localizer["ErrorUploadingVideo"].ToString() + " (Permission Denied)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video banner: {Message}", ex.Message);
                TempData["error"] = _localizer["ErrorUploadingVideo"].ToString();
            }

            // Redirect with cache-busting parameter to force page reload
            return RedirectToAction(nameof(Index), new { _t = DateTime.Now.Ticks });
        }

        // POST: Admin/VideoBanner/UploadPoster
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(5242880)] // 5MB limit for poster
        public async Task<IActionResult> UploadPoster(IFormFile posterFile)
        {
            if (posterFile == null || posterFile.Length == 0)
            {
                TempData["error"] = _localizer["PleaseSelectPosterImage"].ToString();
                return RedirectToAction(nameof(Index));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["error"] = _localizer["InvalidImageFormat"].ToString();
                return RedirectToAction(nameof(Index));
            }

            // Validate file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (posterFile.Length > maxFileSize)
            {
                TempData["error"] = _localizer["ImageFileTooLarge"].ToString();
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Ensure images directory exists
                var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Delete old poster if exists
                var posterPath = Path.Combine(imagesPath, "video-banner-poster.jpg");
                if (System.IO.File.Exists(posterPath))
                {
                    System.IO.File.Delete(posterPath);
                }

                // Save new poster
                using (var fileStream = new FileStream(posterPath, FileMode.Create))
                {
                    await posterFile.CopyToAsync(fileStream);
                }

                TempData["success"] = _localizer["PosterUploadedSuccessfully"].ToString();
                _logger.LogInformation("Video banner poster uploaded successfully by user: {UserId}", User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video banner poster");
                TempData["error"] = _localizer["ErrorUploadingPoster"].ToString();
            }

            // Redirect with cache-busting parameter to force page reload
            return RedirectToAction(nameof(Index), new { _t = DateTime.Now.Ticks });
        }

        // POST: Admin/VideoBanner/DeleteVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVideo()
        {
            try
            {
                var videoPath = Path.Combine(_webHostEnvironment.WebRootPath, "videos", "home-banner.mp4");
                
                if (System.IO.File.Exists(videoPath))
                {
                    System.IO.File.Delete(videoPath);
                    TempData["success"] = _localizer["VideoDeletedSuccessfully"].ToString();
                    _logger.LogInformation("Video banner deleted by user: {UserId}", User.Identity?.Name);
                }
                else
                {
                    TempData["error"] = _localizer["VideoNotFound"].ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting video banner");
                TempData["error"] = _localizer["ErrorDeletingVideo"].ToString();
            }

            // Redirect with cache-busting parameter to force page reload
            return RedirectToAction(nameof(Index), new { _t = DateTime.Now.Ticks });
        }

        // POST: Admin/VideoBanner/DeletePoster
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePoster()
        {
            try
            {
                var posterPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "video-banner-poster.jpg");
                
                if (System.IO.File.Exists(posterPath))
                {
                    System.IO.File.Delete(posterPath);
                    TempData["success"] = _localizer["PosterDeletedSuccessfully"].ToString();
                    _logger.LogInformation("Video banner poster deleted by user: {UserId}", User.Identity?.Name);
                }
                else
                {
                    TempData["error"] = _localizer["PosterNotFound"].ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting video banner poster");
                TempData["error"] = _localizer["ErrorDeletingPoster"].ToString();
            }

            // Redirect with cache-busting parameter to force page reload
            return RedirectToAction(nameof(Index), new { _t = DateTime.Now.Ticks });
        }
    }
}

