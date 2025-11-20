using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BulkyBook.Areas.Admin.Controllers
{
    public class DeleteImageRequest
    {
        public int imageId { get; set; }
    }

    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDBContext _dbContext;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment, ApplicationDBContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
        }

        // GET: Categries
        public async Task<IActionResult> Index()
        {
            List<Product> ObjProduct = _unitOfWork.product.GetAll(includeProperties: "categry").ToList();
            
                
                return View(ObjProduct);
         }

        // GET: Categries/Details/5
        public async Task<IActionResult> Details(int? id)
        {

            var Product = _unitOfWork.product.Get(m => m.Id == id);

            if (Product == null)
            {
                return NotFound();
            }

            return View(Product);
        }

        // GET: Categries/Create
        public IActionResult UpSert(int? id)
        {
            ProductVM productVM = new()
            {
                CategryList = _unitOfWork.categry.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                }),
                product = new Product()
                 
            };
            if(id==null || id == 0)
            {
                //create
                return View(productVM);

            }
            else
            {
                //update - load product with images
                productVM.product = _unitOfWork.product.Get(a => a.Id == id, includeProperties: "ProductImages"); 
                return View(productVM);  
            }


        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public IActionResult UpSert(ProductVM productVM, List<IFormFile>? files)
        {
            // Validate at least one image is required
            if (productVM.product.Id == 0 && (files == null || files.Count == 0))
            {
                ModelState.AddModelError("", "At least one image is required.");
            }

            if (ModelState.IsValid)
            {
                string WWWRootPath = _webHostEnvironment.WebRootPath;
                string ProductPath = Path.Combine(WWWRootPath, @"Images\Products");

                // Handle multiple image uploads
                if (files != null && files.Count > 0)
                {
                    // Get existing product if updating
                    Product existingProduct = null;
                    if (productVM.product.Id != 0)
                    {
                        existingProduct = _unitOfWork.product.Get(a => a.Id == productVM.product.Id, includeProperties: "ProductImages");
                    }

                    int displayOrder = 0;
                    if (existingProduct != null && existingProduct.ProductImages != null)
                    {
                        displayOrder = existingProduct.ProductImages.Any() 
                            ? existingProduct.ProductImages.Max(pi => pi.DisplayOrder) + 1 
                            : 0;
                    }

                    foreach (var file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            string FileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            
                            using (var fileStream = new FileStream(Path.Combine(ProductPath, FileName), FileMode.Create))
                            {
                                file.CopyTo(fileStream);
                            }

                            // Set first image as main ImageUrl for backward compatibility
                            if (string.IsNullOrEmpty(productVM.product.ImageUrl))
                            {
                                productVM.product.ImageUrl = @"\Images\Products\" + FileName;
                            }

                            // Add ProductImage
                            if (productVM.product.Id == 0)
                            {
                                // New product - add to collection
                                if (productVM.product.ProductImages == null)
                                {
                                    productVM.product.ProductImages = new List<ProductImage>();
                                }
                                productVM.product.ProductImages.Add(new ProductImage
                                {
                                    ImageUrl = @"\Images\Products\" + FileName,
                                    DisplayOrder = displayOrder++
                                });
                            }
                            else
                            {
                                // Existing product - add to DbContext
                                var productImage = new ProductImage
                                {
                                    ProductId = productVM.product.Id,
                                    ImageUrl = @"\Images\Products\" + FileName,
                                    DisplayOrder = displayOrder++
                                };
                                _dbContext.ProductImages.Add(productImage);
                            }
                        }
                    }
                }

                if (productVM.product.Id == 0)
                {
                    _unitOfWork.product.add(productVM.product);
                    _unitOfWork.save(); // Save first to get the ProductId
                    
                    // Update ProductImages with correct ProductId
                    if (productVM.product.ProductImages != null && productVM.product.ProductImages.Any())
                    {
                        foreach (var img in productVM.product.ProductImages)
                        {
                            img.ProductId = productVM.product.Id;
                            _dbContext.ProductImages.Add(img);
                        }
                        _dbContext.SaveChanges();
                    }
                    
                    TempData["success"] = "Product Created Successfully";
                }
                else
                {
                    _unitOfWork.product.update(productVM.product);
                    _unitOfWork.save();
                    TempData["success"] = "Product Updated Successfully";
                }
                
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategryList = _unitOfWork.categry.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                });
                
                // Reload product with images if updating
                if (productVM.product.Id != 0)
                {
                    productVM.product = _unitOfWork.product.Get(a => a.Id == productVM.product.Id, includeProperties: "ProductImages");
                }
                
                return View(productVM);
            }
        }

        // GET: Categries/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{


        //    var Product = _unitOfWork.product.Get(m => m.Id == id);
        //    if (Product == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(Product);
        //}

        //// POST: Categries/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id,Product Product)
        //{
        //    if (id != Product.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _unitOfWork.product.update(Product);
        //            _unitOfWork.save();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!ProductExists(Product.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        TempData["success"] = "product Modified Successfully";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(Product);
        //}

        // Delete Product Image
        [HttpPost]
        public IActionResult DeleteImage([FromBody] DeleteImageRequest request)
        {
            var productImage = _dbContext.ProductImages.FirstOrDefault(pi => pi.Id == request.imageId);
            if (productImage == null)
            {
                return Json(new { success = false, message = "Image not found" });
            }

            // Delete physical file
            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productImage.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            // Get product to check if this is the main image
            var product = _unitOfWork.product.Get(p => p.Id == productImage.ProductId);
            if (product != null && product.ImageUrl == productImage.ImageUrl)
            {
                // If this was the main image, set the first remaining image as main
                var remainingImages = _dbContext.ProductImages
                    .Where(pi => pi.ProductId == productImage.ProductId && pi.Id != request.imageId)
                    .OrderBy(pi => pi.DisplayOrder)
                    .FirstOrDefault();
                
                if (remainingImages != null)
                {
                    product.ImageUrl = remainingImages.ImageUrl;
                    _unitOfWork.product.update(product);
                }
                else
                {
                    product.ImageUrl = null;
                    _unitOfWork.product.update(product);
                }
            }

            // Delete from database
            _dbContext.ProductImages.Remove(productImage);
            _dbContext.SaveChanges();

            return Json(new { success = true, message = "Image deleted successfully" });
        }

        // GET: Categries/Delete/5
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
           var Product = _unitOfWork.product.Get(m => m.Id == id, includeProperties: "ProductImages");
            if (Product == null)
            {
                return Json(new { success = false, massage = "Error While Deleting" });
            }

            // Delete main image if exists
            if (!string.IsNullOrEmpty(Product.ImageUrl))
            {
                var OldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, Product.ImageUrl.Trim('\\'));
                if (System.IO.File.Exists(OldImagePath))
                {
                    System.IO.File.Delete(OldImagePath);
                }
            }

            // Delete all product images
            if (Product.ProductImages != null && Product.ProductImages.Any())
            {
                foreach (var productImage in Product.ProductImages)
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, productImage.ImageUrl.Trim('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _dbContext.ProductImages.RemoveRange(Product.ProductImages);
            }

            _unitOfWork.product.remove(Product);
            _unitOfWork.save();
            return Json(new { success = true, massage = "Success To Delete Product" });

        }

        // POST: Categries/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    Product? Product = _unitOfWork.product.Get(m => m.Id == id);

        //    if (Product != null)
        //    {
        //        _unitOfWork.product.remove(Product);
        //        _unitOfWork.save();
        //        TempData["success"] = "product Deleted Successfully";
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

        private bool ProductExists(int id)
        {
            return (_unitOfWork.product.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        #region API Call
        [HttpGet]
        public IActionResult GetAll() {
            List<Product> ObjProduct = _unitOfWork.product.GetAll(includeProperties: "categry").ToList();
            return Json(new { data = ObjProduct });
        }
        #endregion
    }
}
 