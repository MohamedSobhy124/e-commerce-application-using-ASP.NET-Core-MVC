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
            // Set audit fields
            if (obj is BaseEntity baseEntity)
            {
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
            }
            _db.FlashSaleItems.Update(obj);
        }   
        public void Add(FlashSaleItem obj)
        {
            _db.FlashSaleItems.Update(obj);
        }  
        public void Remove(FlashSaleItem obj)
        {
            // Soft delete for BaseEntity types
            if (obj is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.ModifiedDate = BulkyBook.Utility.DateTimeHelper.Now;
                _db.FlashSaleItems.Update(obj);
            }
            else
            {
                _db.FlashSaleItems.Remove(obj);
            }
        }
    }
}
