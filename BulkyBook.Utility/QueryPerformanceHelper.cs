using System.Linq.Expressions;
using BulkyBook.Models;

namespace BulkyBook.Utility
{
    /// <summary>
    /// Helper class for optimized query performance
    /// Uses compiled expressions to avoid reflection overhead
    /// </summary>
    public static class QueryPerformanceHelper
    {
        // Compiled expression for IsDeleted check (much faster than reflection)
        private static readonly Func<BaseEntity, bool> IsNotDeletedExpression = 
            entity => !entity.IsDeleted;

        /// <summary>
        /// Fast check if entity is not deleted
        /// </summary>
        public static bool IsNotDeleted(BaseEntity entity)
        {
            return IsNotDeletedExpression(entity);
        }
    }
}

