    
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
 
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using System.Linq;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T :class
    {
        private readonly ApplicationDBContext _db;
        internal DbSet<T> dbSet;  

        public Repository(ApplicationDBContext db)
        {
            _db = db;   
                this.dbSet= _db.Set<T>();
            _db.Products.Include(u => u.categry);
        }   
        public void add(T entity)
        {
            _db.Add(entity);    
        }

        public T Get(Expression<Func<T, bool>> filter, string? includeProperties = null )
        {
            IQueryable<T> query = dbSet;
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeprop in includeProperties
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeprop);
                }
            }
            query = query.Where(filter);

            return query.FirstOrDefault();
        }

		public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter, string? includeProperties = null)
		{
			IQueryable<T> query = dbSet;
			if (filter != null)
			{
				query = query.Where(filter);
			}
			if (!string.IsNullOrEmpty(includeProperties))
			{
				foreach (var includeProp in includeProperties
					.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(includeProp);
				}
			}
			return query.ToList();
		}
		public void remove(T entity)
        {
            dbSet.Remove(entity);   
        }

        public void removeRage(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);

        }

        //public void Update(T entity)
        //{
        //   dbSet.Update(entity);    
        //}
    }
}
