using IdealWeightNutrition.Models;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdealWeightNutrition.DataAccess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdealWeightNutrition.DataAccess.DbInitializer {
    public class DbInitializer : IDbInitializer {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDBContext _db;

        public DbInitializer(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
			ApplicationDBContext db) {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }


        public void Initialize() {

            // Apply pending migrations so BlogPosts table exists before seeding
            try {
                if (_db.Database.GetPendingMigrations().Any()) {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Migration skipped or failed: {ex.Message}");
            }

            //create roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult()) {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();


                //if roles are not created, then we will create admin user as well
                _userManager.CreateAsync(new ApplicationUser {
                    UserName = "admin@dotnetmastery.com",
                    Email = "admin@dotnetmastery.com",
                    Name = "Bhrugen Patel",
                    PhoneNumber = "1112223333",
                    StreetAddress = "test 123 Ave",
                    State = "IL",
                    PostalCode = "23422",
                    City = "Chicago"
                }, "Admin123*").GetAwaiter().GetResult();


                //ApplicationUser user = _db.app.FirstOrDefault(u => u.Email == "admin@dotnetmastery.com");
                //_userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();

            }

            // Seed blog posts if table is empty
            try
            {
                BlogPostSeed.Seed(_db);
            }
            catch (Exception ex)
            {
                // Log but don't fail - blog table might not exist yet before migration
                System.Diagnostics.Debug.WriteLine($"Blog seed skipped or failed: {ex.Message}");
            }

            return;
        }
    }
}
