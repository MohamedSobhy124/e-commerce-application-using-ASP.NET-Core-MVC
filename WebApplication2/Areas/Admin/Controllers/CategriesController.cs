using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BulkyBook.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategriesController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategriesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Categries
        public  IActionResult Index()
        {
            return
                        View(_unitOfWork.categry.GetAll().ToList());

        }

        // GET: Categries/Details/5
        public IActionResult Details(int? id)
        {

            var categry = _unitOfWork.categry.Get(m => m.Id == id);

            if (categry == null)
            {
                return NotFound();
            }

            return View(categry);
        }

        // GET: Categries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Id,Name,Description")] Categry categry)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.categry.add(categry);
                _unitOfWork.save();
                TempData["success"] = "Categray Created Successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(categry);
        }

        // GET: Categries/Edit/5
        public IActionResult Edit(int? id)
        {


            var categry = _unitOfWork.categry.Get(m => m.Id == id);
            if (categry == null)
            {
                return NotFound();
            }
            return View(categry);
        }

        // POST: Categries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,Name,Description")] Categry categry)
        {
            if (id != categry.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.categry.update(categry);
                    _unitOfWork.save();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategryExists(categry.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["success"] = "Categray Modified Successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(categry);
        }

        // GET: Categries/Delete/5
        public IActionResult Delete(int? id)
        {
            Categry? categry = _unitOfWork.categry.Get(m => m.Id == id);
            if (categry == null)
            {
                return NotFound();
            }



            return View(categry);
        }

        // POST: Categries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            Categry? categry = _unitOfWork.categry.Get(m => m.Id == id);

            if (categry != null)
            {
                _unitOfWork.categry.remove(categry);
                _unitOfWork.save();
                TempData["success"] = "Categray Deleted Successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategryExists(int id)
        {
            return (_unitOfWork.categry.GetAll()?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
