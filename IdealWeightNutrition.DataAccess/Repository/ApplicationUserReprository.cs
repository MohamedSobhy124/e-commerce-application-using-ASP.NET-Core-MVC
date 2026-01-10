using IdealWeightNutrition.DataAccess.Data;
using IdealWeightNutrition.DataAccess.Repository.IRepository;
using IdealWeightNutrition.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.DataAccess.Repository
{
    public class ApplicationUserReprository : Repository<ApplicationUser>, IApplicationUserReprository
    {
        private ApplicationDBContext _db;

        public ApplicationUserReprository(ApplicationDBContext db):base(db) 
        {
                _db = db;   
        }

        /// <summary>
        /// Get all users from AspNetUsers table without Discriminator filter
        /// This returns both IdentityUser (Google/external logins) and ApplicationUser records
        /// </summary>
        public IEnumerable<IdentityUser> GetAllUsersWithoutDiscriminator()
        {
            // Query the Users DbSet from IdentityDbContext which includes all users
            // This bypasses the Discriminator filter that ApplicationUser uses
            return _db.Users.AsNoTracking().ToList();
        }

        /// <summary>
        /// Get all ApplicationUser records (with Discriminator filter - only custom users)
        /// </summary>
        public IEnumerable<ApplicationUser> GetAllApplicationUsers()
        {
            return _db.applicationUsers.AsNoTracking().ToList();
        }
    }
}
