using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
 

namespace BulkyBook.DataAccess.Repository
{
    public class ProductReprository :Repository<Product>, IProductReprository
    {
        private ApplicationDBContext _db;

        public ProductReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }
        

        public void update(Product obj)
        {
           var obgFromDB=_db.Products.FirstOrDefault(a=>a.Id == obj.Id);
            if (obgFromDB != null) { 
            obgFromDB.Title = obj.Title;    
                obgFromDB.Price = obj.Price;    
                obgFromDB.ListPrice = obj.ListPrice;
                obgFromDB.CategryId = obj.CategryId;
                obgFromDB.Description   = obj.Description;
                obgFromDB.StockQuantity   = obj.StockQuantity;
                obgFromDB.MinimumStockAlert   = obj.MinimumStockAlert;
                obgFromDB.ProductType = obj.ProductType;
                if(obj.ImageUrl != null)
                {
                    obgFromDB.ImageUrl = obj.ImageUrl;

                }
                // Set audit fields
                if (obgFromDB is BaseEntity baseEntity)
                {
                    baseEntity.ModifiedDate = DateTime.Now;
                }
            }
        }
    }
}
