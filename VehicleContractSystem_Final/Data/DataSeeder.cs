using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.ConstantValues;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Data
{
    public static class DataSeeder
    {
        // ==================== SEED FUNCTIONS ====================

        private static ApplicationDbContext _context = default!;
        private static RoleManager<IdentityRole> _roleManager = default!;
        private static UserManager<ApplicationUser> _userManager = default!;

        public static async Task SeedAsync(ApplicationDbContext context, IServiceProvider services)
        {
            _context = context;

            _roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed roles
            bool hasRoles = await _context.Roles.AnyAsync();
            if (!hasRoles)
            {
                //await SeedRoles(_roleManager);
                await AddRolesAsync([RolesNames.SuperAdmin, RolesNames.OfficeAdmin, RolesNames.User]);

                ApplicationUser admin = await CreateUserAsync("admin@vehiclecontract.se", "Super", "Admin", "19800101-1111", "Admin@123", "0760");
                ApplicationUser office = await CreateUserAsync("office@vehiclecontract.se", "Office", "Admin", "19760101-1111", "Office@123", "0780");
                ApplicationUser user = await CreateUserAsync("user@vehiclecontract.se", "User", "User", "19840101-2222", "User@123", "0790");

                await AddRoleToUserAsync(admin, RolesNames.SuperAdmin);
                await AddRoleToUserAsync(office, RolesNames.OfficeAdmin);
                await AddRoleToUserAsync(user, RolesNames.User);
                
            }
            
        }
        private static async Task AddRolesAsync(string[] roleNames)
        {
            foreach (var roleName in roleNames)
            {
                if (await _roleManager.RoleExistsAsync(roleName)) continue;

                IdentityRole role = new() { Name = roleName };
                var result = await _roleManager.CreateAsync(role);

                if (!result.Succeeded) throw new Exception(string.Join("\n", result.Errors));
            }
        }
        private static async Task<ApplicationUser> CreateUserAsync(string email, string fName, string lName, string personalNumber, string password, string phoneNumber)
        {
            ApplicationUser? found = await _userManager.FindByEmailAsync(email);
            if (found is not null) return null!;

            ApplicationUser user = new()
            {
                UserName = email,
                Email = email,
                FirstName = fName,
                LastName = lName,
                EmailConfirmed = true,
                PersonalNumber = personalNumber,
                PhoneNumber = phoneNumber,
                OfficeId = 1
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) throw new Exception(string.Join("\n", result.Errors));

            return user;
        }
        private static async Task AddRoleToUserAsync(ApplicationUser user, string role)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var result = await _userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded) throw new Exception(string.Join("\n", result.Errors));
            }
        }
        //private static async Task<List<ApplicationUser>> GenerateUsers(int numberOfUsers)
        //{
        //    List<ApplicationUser> members = [];
        //    List<string> personNumberList = [];

        //    // Move DateTime.Now back by a random number of days, months, and years
        //    // Add it to the list if it's unique
        //    while (personNumberList.Count < numberOfUsers)
        //    {
        //        DateTime date = DateTime.Now;
        //        date = date.AddDays(_rnd.Next(-20, -1))
        //            .AddMonths(_rnd.Next(-30, 0))
        //            .AddYears(_rnd.Next(-80, -15));
        //        string personNumber = _faker.Random.Replace($"{date.ToString("yyyyMMdd")}-####");
        //        if (!personNumberList.Contains(personNumber))
        //            personNumberList.Add(personNumber);
        //    }

        //    for (int i = 0; i < numberOfUsers; i++)
        //    {
        //        members.Add(await CreateUserAsync(
        //            email: _faker.Internet.Email(),
        //            fName: _faker.Name.FirstName(),
        //            lName: _faker.Name.LastName(),
        //            personalNumber: personNumberList[i],
        //            // Faker.Internet.Password can fail the password requirements
        //            // so, just uses a simple one that works
        //            password: "Aa111!"
        //        ));
        //    }
        //    return members;
        //}
        //private static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
        //{
        //    string[] roles = new[] { "Admin", "OfficeAdmin", "User" };
        //    foreach (var role in roles)
        //    {
        //        if (!await roleManager.RoleExistsAsync(role))
        //        {
        //            await roleManager.CreateAsync(new IdentityRole(role));
        //        }
        //    }
        //}

        private static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Create admin user if not exists
            var adminEmail = "admin@vehiclecontract.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var newAdmin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    PersonalNumber = "1234567890123",
                    EmailConfirmed = true,
                    IsProMember = true,
                    OfficeId = 1
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
            
        }
        private static async Task SeedOffices(ApplicationDbContext context)
        {
            if (!context.Offices.Any())
            {
                context.Offices.AddRange(
                    new Office { Name = "المكتب الرئيسي", Address = "بغداد" },
                    new Office { Name = "فرع البصرة", Address = "البصرة" },
                    new Office { Name = "فرع النجف", Address = "النجف" }
                );
                await context.SaveChangesAsync();
            }
        }        

    }
}