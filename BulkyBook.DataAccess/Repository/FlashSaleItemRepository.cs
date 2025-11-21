using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class FlashSaleItemRepository : Repository<FlashSaleItem>, IFlashSaleItemRepository
    {
        private ApplicationDBContext _db;
        public FlashSaleItemRepository(ApplicationDBContext db) : base(db)
        {
            _db = db;
        }

        public void Update(FlashSaleItem obj)
        {
            _db.FlashSaleItems.Update(obj);
        }   
        public void Add(FlashSaleItem obj)
        {
            _db.FlashSaleItems.Update(obj);
        }  
        public void Remove(FlashSaleItem obj)
        {
            _db.FlashSaleItems.Remove(obj);
        }
    }
}
