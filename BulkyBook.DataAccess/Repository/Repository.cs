    
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
 
using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
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
            // Set audit fields for BaseEntity types
            if (entity is BaseEntity baseEntity)
            {
                if (baseEntity.CreatedDate == default(DateTime))
                {
                    baseEntity.CreatedDate = DateTime.Now;
                }
                baseEntity.IsDeleted = false;
            }
            _db.Add(entity);    
        }

        public T Get(Expression<Func<T, bool>> filter, string? includeProperties = null )
        {
            IQueryable<T> query = dbSet;
            
            // Filter out deleted items for BaseEntity types
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                query = query.Where(e => !((BaseEntity)(object)e).IsDeleted);
            }
            
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
			
			// Filter out deleted items for BaseEntity types
			if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
			{
				query = query.Where(e => !((BaseEntity)(object)e).IsDeleted);
			}
			
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
            // Soft delete for BaseEntity types
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.ModifiedDate = DateTime.Now;
                _db.Update(entity);
            }
            else
            {
                // Hard delete for non-BaseEntity types
                dbSet.Remove(entity);
            }
        }

        public void removeRage(IEnumerable<T> entities)
        {
            // Soft delete for BaseEntity types
            var baseEntities = entities.OfType<BaseEntity>().ToList();
            if (baseEntities.Any())
            {
                foreach (var baseEntity in baseEntities)
                {
                    baseEntity.IsDeleted = true;
                    baseEntity.ModifiedDate = DateTime.Now;
                }
                _db.UpdateRange(baseEntities.Cast<T>());
            }
            else
            {
                // Hard delete for non-BaseEntity types
                dbSet.RemoveRange(entities);
            }
        }

        //public void Update(T entity)
        //{
        //   dbSet.Update(entity);    
        //}
    }
}
