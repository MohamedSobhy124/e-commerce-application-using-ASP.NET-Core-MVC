using BulkyBook.DataAccess.Repository.IRepository;
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
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;   
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
                //update    
                productVM.product=_unitOfWork.product.Get(a=>a.Id==id); 
                return View(productVM);  
            }


        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public  IActionResult UpSert( ProductVM productVM,IFormFile? file )
        {
            if (ModelState.IsValid)
            {
                string WWWRootPath = _webHostEnvironment.WebRootPath;
                if (file !=null)
                {
                   string FileName=Guid.NewGuid().ToString()+Path.GetExtension(file.FileName);   
                    string ProductPath=Path.Combine(WWWRootPath,@"Images\Products");

                    if (!string.IsNullOrEmpty(productVM.product.ImageUrl))
                    {
                        //delete old img
                        var OldImagePath=
                            Path.Combine(WWWRootPath,productVM.product.ImageUrl.Trim('\\'));  
                        if(System.IO.File.Exists(OldImagePath))
                        {
                            System.IO.File.Delete(OldImagePath);    
                        }
                    }

                    using(var fileSteam =new FileStream(Path.Combine(ProductPath, FileName), FileMode.Create))
                    {
                        file.CopyTo(fileSteam); 
                    }
                    productVM.product.ImageUrl = @"\Images\Products\" + FileName;   
                }
                if (productVM.product.Id == 0) { 
                _unitOfWork.product.add(productVM.product);
                    TempData["success"] = "product Created Successfully";
                }
                else
                {
                    _unitOfWork.product.update(productVM.product);
                    TempData["success"] = "product Updated Successfully";

                }
                _unitOfWork.save();
                
                return RedirectToAction("Index");
            }
            else
            {

                productVM.CategryList = _unitOfWork.categry.GetAll().Select(a => new SelectListItem
                {
                    Text = a.Name,
                    Value = a.Id.ToString()
                });
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

        // GET: Categries/Delete/5
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
           var Product = _unitOfWork.product.Get(m => m.Id == id);
            if (Product == null)
            {
                return Json(new { success = false, massage = "Error While Deleting" });
            }

            var OldImagePath =
                           Path.Combine(_webHostEnvironment.WebRootPath, Product.ImageUrl.Trim('\\'));
            if (System.IO.File.Exists(OldImagePath))
            {
                System.IO.File.Delete(OldImagePath);
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
 