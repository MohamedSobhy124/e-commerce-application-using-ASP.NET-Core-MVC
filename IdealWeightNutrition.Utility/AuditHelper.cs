using IdealWeightNutrition.Models;
using System.Security.Claims;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper class for setting audit fields on entities
    /// </summary>
    public static class AuditHelper
    {
        /// <summary>
        /// Gets the user's display name from claims or identity
        /// </summary>
        private static string? GetUserName(ClaimsPrincipal? user)
        {
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            // Try to get name from various claims (in order of preference)
            var name = user.FindFirst("Name")?.Value  // Custom Name claim
                    ?? user.GetName()  // Using ClaimsPrincipalExtension
                    ?? user.FindFirst(ClaimTypes.Name)?.Value 
                    ?? user.FindFirst("FullName")?.Value 
                    ?? user.FindFirst("name")?.Value
                    ?? user.Identity.Name;

            // If still no name, try email as fallback
            if (string.IsNullOrEmpty(name))
            {
                name = user.FindFirst(ClaimTypes.Email)?.Value;
            }

            return name;
        }

        /// <summary>
        /// Sets audit fields when creating an entity
        /// </summary>
        public static void SetCreatedAudit(BaseEntity entity, ClaimsPrincipal? user = null)
        {
            if (entity == null) return;
            
            entity.CreatedDate = DateTime.Now;
            entity.IsDeleted = false;
            entity.CreatedBy = GetUserName(user);
        }

        /// <summary>
        /// Sets audit fields when updating an entity
        /// </summary>
        public static void SetModifiedAudit(BaseEntity entity, ClaimsPrincipal? user = null)
        {
            if (entity == null) return;
            
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedBy = GetUserName(user);
        }

        /// <summary>
        /// Sets audit fields when soft deleting an entity
        /// </summary>
        public static void SetDeletedAudit(BaseEntity entity, ClaimsPrincipal? user = null)
        {
            if (entity == null) return;
            
            entity.IsDeleted = true;
            entity.ModifiedDate = DateTime.Now;
            entity.ModifiedBy = GetUserName(user);
        }
    }
}

