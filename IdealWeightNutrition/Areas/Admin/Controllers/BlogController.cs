using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BlogController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDBContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BlogController(IUnitOfWork unitOfWork, ApplicationDBContext db, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _db = db;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Blog - Show all blogs including hidden
        public IActionResult Index()
        {
            var blogs = _db.BlogPosts
                .OrderByDescending(b => b.PublishedDate)
                .ToList();
            return View(blogs);
        }

        private void PopulateCategoryLists()
        {
            var categoriesEn = _db.BlogPosts
                .Where(b => !string.IsNullOrEmpty(b.Category))
                .Select(b => b.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            var categoriesAr = _db.BlogPosts
                .Where(b => !string.IsNullOrEmpty(b.CategoryAr))
                .Select(b => b.CategoryAr)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            ViewBag.CategoriesEn = categoriesEn;
            ViewBag.CategoriesAr = categoriesAr;
        }

        // GET: Blog/Create
        public IActionResult Create()
        {
            PopulateCategoryLists();
            return View(new BlogPost { PublishedDate = DateTime.Now });
        }

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BlogPost blog)
        {
            ModelState.Remove("Id");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("ModifiedDate");
            ModelState.Remove("CreatedBy");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("IsDeleted");

            // Auto-generate slug from title (always use title for consistency)
            if (!string.IsNullOrWhiteSpace(blog.Title))
            {
                blog.Slug = GenerateSlug(blog.Title);
                ModelState.Remove("Slug");
            }

            if (ModelState.IsValid)
            {
                // Check slug uniqueness
                if (_db.BlogPosts.Any(b => b.Slug == blog.Slug))
                {
                    ModelState.AddModelError("Slug", "A blog with this slug already exists.");
                    return View(blog);
                }

                try
                {
                    HandleImageUpload(blog);
                    AuditHelper.SetCreatedAudit(blog, User);
                    _unitOfWork.BlogPost.add(blog);
                    _unitOfWork.save();
                    TempData["success"] = "Blog created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            PopulateCategoryLists();
            return View(blog);
        }

        // GET: Blog/Edit/5
        public IActionResult Edit(int? id)
        {
            var blog = _db.BlogPosts.Find(id);
            if (blog == null) return NotFound();
            PopulateCategoryLists();
            return View(blog);
        }

        // POST: Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, BlogPost blog)
        {
            if (id != blog.Id) return NotFound();

            var existingBlog = _db.BlogPosts.AsNoTracking().FirstOrDefault(b => b.Id == id);
            if (existingBlog == null) return NotFound();

            ModelState.Remove("CreatedDate");
            ModelState.Remove("CreatedBy");

            // Check slug uniqueness (exclude current blog)
            if (_db.BlogPosts.Any(b => b.Slug == blog.Slug && b.Id != id))
            {
                ModelState.AddModelError("Slug", "A blog with this slug already exists.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve audit fields
                    blog.CreatedDate = existingBlog.CreatedDate;
                    blog.CreatedBy = existingBlog.CreatedBy;
                    blog.IsDeleted = existingBlog.IsDeleted;

                    // Preserve existing image if no new upload
                    if (blog.ImageFile == null || blog.ImageFile.Length == 0)
                    {
                        blog.ImageUrl = existingBlog.ImageUrl;
                    }
                    else
                    {
                        HandleImageUpload(blog);
                        // Delete old image if exists (only for uploaded files, not external URLs)
                        if (!string.IsNullOrEmpty(existingBlog.ImageUrl) && !existingBlog.ImageUrl.StartsWith("http"))
                        {
                            var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, existingBlog.ImageUrl.TrimStart('\\', '/'));
                            if (System.IO.File.Exists(oldPath))
                                System.IO.File.Delete(oldPath);
                        }
                    }

                    AuditHelper.SetModifiedAudit(blog, User);
                    _unitOfWork.BlogPost.update(blog);
                    _unitOfWork.save();
                    TempData["success"] = "Blog updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                }
            }
            PopulateCategoryLists();
            return View(blog);
        }

        // POST: Blog/Hide/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Hide(int id)
        {
            var blog = _db.BlogPosts.Find(id);
            if (blog == null) return NotFound();

            AuditHelper.SetDeletedAudit(blog, User);
            blog.IsDeleted = true;
            _db.BlogPosts.Update(blog);
            _db.SaveChanges();
            TempData["success"] = "Blog hidden successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Blog/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(int id)
        {
            var blog = _db.BlogPosts.Find(id);
            if (blog == null) return NotFound();

            blog.IsDeleted = false;
            AuditHelper.SetModifiedAudit(blog, User);
            _db.BlogPosts.Update(blog);
            _db.SaveChanges();
            TempData["success"] = "Blog restored successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void HandleImageUpload(BlogPost blog)
        {
            if (blog.ImageFile != null && blog.ImageFile.Length > 0)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string blogPath = Path.Combine(wwwRootPath, "images", "blogs");
                if (!Directory.Exists(blogPath))
                    Directory.CreateDirectory(blogPath);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(blog.ImageFile.FileName);
                using (var fileStream = new FileStream(Path.Combine(blogPath, fileName), FileMode.Create))
                {
                    blog.ImageFile.CopyTo(fileStream);
                }
                blog.ImageUrl = $"/images/blogs/{fileName}";
            }
        }

        private static string GenerateSlug(string title)
        {
            var slug = title.ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", " ").Trim();
            slug = Regex.Replace(slug, @"\s", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            return slug;
        }
    }
}
