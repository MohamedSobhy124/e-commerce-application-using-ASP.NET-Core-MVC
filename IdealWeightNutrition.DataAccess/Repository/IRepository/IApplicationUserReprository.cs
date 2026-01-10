using IdealWeightNutrition.Models;
using Microsoft.AspNetCore.Identity;

namespace IdealWeightNutrition.DataAccess.Repository.IRepository
{
    public interface IApplicationUserReprository : IRepository<ApplicationUser> 
    {
        /// <summary>
        /// Get all users from AspNetUsers table without Discriminator filter
        /// Returns both IdentityUser and ApplicationUser records
        /// </summary>
        IEnumerable<IdentityUser> GetAllUsersWithoutDiscriminator();
        
        /// <summary>
        /// Get all ApplicationUser records (standard method with Discriminator filter)
        /// </summary>
        IEnumerable<ApplicationUser> GetAllApplicationUsers();
    }
}
