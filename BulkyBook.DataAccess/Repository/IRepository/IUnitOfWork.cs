    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
     public interface IUnitOfWork
    {
        ICategryReprository categry{ get; }
        IProductReprository product { get; }
        ICompanyRepository company { get; }
        IShoppingCartReprository shoppingCart { get; }
        IApplicationUserReprository applicationUser { get; }
        IOrderDetailRepository OrderDetail { get; }
        IOrderHeaderRepository OrderHeader { get; }

        void save();
    }
}
