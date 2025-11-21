using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IFlashSaleItemRepository : IRepository<FlashSaleItem>
    {
        void Update(FlashSaleItem obj);
        void Add(FlashSaleItem obj);
        void Remove(FlashSaleItem obj);
    }
}
