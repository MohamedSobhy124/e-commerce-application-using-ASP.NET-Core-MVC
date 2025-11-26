    
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
            
            // PERFORMANCE: Use optimized filter instead of casting
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                var parameter = Expression.Parameter(typeof(T), "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var constant = Expression.Constant(false);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);
                query = query.Where(lambda);
            }
            
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeprop in includeProperties
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeprop.Trim());
                }
            }
            query = query.Where(filter);

            return query.FirstOrDefault();
        }

		public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter, string? includeProperties = null)
		{
			IQueryable<T> query = dbSet;
			
			// PERFORMANCE: Use optimized filter instead of casting
			if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
			{
				var parameter = Expression.Parameter(typeof(T), "e");
				var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
				var constant = Expression.Constant(false);
				var equality = Expression.Equal(property, constant);
				var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);
				query = query.Where(lambda);
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
					query = query.Include(includeProp.Trim());
				}
			}
			return query.ToList();
		}
		
		/// <summary>
		/// PERFORMANCE: Get all with AsNoTracking for read-only queries (faster, less memory)
		/// </summary>
		public IEnumerable<T> GetAllAsNoTracking(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
		{
			IQueryable<T> query = dbSet.AsNoTracking();
			
			// PERFORMANCE: Use optimized filter
			if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
			{
				var parameter = Expression.Parameter(typeof(T), "e");
				var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
				var constant = Expression.Constant(false);
				var equality = Expression.Equal(property, constant);
				var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);
				query = query.Where(lambda);
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
					query = query.Include(includeProp.Trim());
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
