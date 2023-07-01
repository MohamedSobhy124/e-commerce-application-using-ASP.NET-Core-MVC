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
                obgFromDB.ISBN = obj.ISBN;  
                obgFromDB.Author = obj.Author;
                obgFromDB.Price = obj.Price;    
                obgFromDB.ListPrice = obj.ListPrice;
                obgFromDB.Price50 = obj.Price50;
                obgFromDB.Price100  = obj.Price100;
                obgFromDB.CategryId = obj.CategryId;
                obgFromDB.Description   = obj.Description;
                if(obj.ImageUrl != null)
                {
                    obgFromDB.ImageUrl = obj.ImageUrl;

                }
            }
        }
    }
}
