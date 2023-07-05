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

    public class CompaniesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public CompaniesController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;   
        }

        // GET: Categries
        public async Task<IActionResult> Index()
        {
            List<Company> ObjCompany = _unitOfWork.company.GetAll().ToList();
            
                
                return View(ObjCompany);
         }

        // GET: Categries/Details/5
        public async Task<IActionResult> Details(int? id)
        {

            var Company = _unitOfWork.company.Get(m => m.Id == id);

            if (Company == null)
            {
                return NotFound();
            }

            return View(Company);
        }

        // GET: Categries/Create
        public IActionResult UpSert(int? id)
        {

            Company company = new Company();
                
            if(id==null || id == 0)
            {
                //create
                return View(company);

            }
            else
            {
                //update    
                company=_unitOfWork.company.Get(a=>a.Id==id); 
                return View(company);  
            }


        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public  IActionResult UpSert( Company company)
        {
            if (ModelState.IsValid)
            {
              
                if (company.Id == 0) { 
                _unitOfWork.company.add(company);
                    TempData["success"] = "company Created Successfully";
                }
                else
                {
                    _unitOfWork.company.Update(company);
                    TempData["success"] = "company Updated Successfully";

                }
                _unitOfWork.save();
                
                return RedirectToAction("Index");
            }
            else
            {

               
                return View(company);

            }
        }

        // GET: Categries/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{


        //    var Company = _unitOfWork.company.Get(m => m.Id == id);
        //    if (Company == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(Company);
        //}

        //// POST: Categries/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id,Company Company)
        //{
        //    if (id != Company.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _unitOfWork.company.update(Company);
        //            _unitOfWork.save();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!CompanyExists(Company.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        TempData["success"] = "company Modified Successfully";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(Company);
        //}

        // GET: Categries/Delete/5
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
           var Company = _unitOfWork.company.Get(m => m.Id == id);
            if (Company == null)
            {
                return Json(new { success = false, massage = "Error While Deleting" });
            }

            

            _unitOfWork.company.remove(Company);
            _unitOfWork.save();
            return Json(new { success = true, massage = "Success To Delete Company" });

        }

        // POST: Categries/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    Company? Company = _unitOfWork.company.Get(m => m.Id == id);

        //    if (Company != null)
        //    {
        //        _unitOfWork.company.remove(Company);
        //        _unitOfWork.save();
        //        TempData["success"] = "company Deleted Successfully";
        //    }

        //    return RedirectToAction(nameof(Index));
        //}

        private bool CompanyExists(int id)
        {
            return (_unitOfWork.company.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        #region API Call
        [HttpGet]
        public IActionResult GetAll() {
            List<Company> ObjCompany = _unitOfWork.company.GetAll().ToList();
            return Json(new { data = ObjCompany });
        }
        #endregion
    }
}
 