using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.ConstantValues;
using VehicleContractSystem_Final.Enums;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Data
{
    public static class DataSeeder
    {
        private static ApplicationDbContext _context = default!;
        private static RoleManager<IdentityRole> _roleManager = default!;
        private static UserManager<ApplicationUser> _userManager = default!;

        public static async Task SeedDevelopmentAsync(
            ApplicationDbContext context,
            IServiceProvider services,
            IConfiguration configuration)
        {
            _context = context;
            _roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await AddRolesAsync([RolesNames.SuperAdmin, RolesNames.OfficeAdmin, RolesNames.User]);
            await SeedCoreReferenceDataAsync();

            if (configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
            {
                var developmentPassword = GetRequiredConfigurationValue(
                    configuration,
                    "DevelopmentSeed:Password");

                await SeedDevelopmentSuperAdminAsync(developmentPassword);
                await SeedDemoAsync(developmentPassword);
            }

            await BackfillContractVehiclesAsync();
            await SyncVehicleStatusesAsync();
        }

        public static async Task SeedReferenceDataAsync(
            ApplicationDbContext context,
            IServiceProvider services)
        {
            _context = context;
            _roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await AddRolesAsync([RolesNames.SuperAdmin, RolesNames.OfficeAdmin, RolesNames.User]);
            await SeedCoreReferenceDataAsync();
        }

        public static async Task ProvisionFirstAdminAsync(
            ApplicationDbContext context,
            IServiceProvider services,
            IConfiguration configuration)
        {
            if (!configuration.GetValue<bool>("FirstAdminProvisioning:Enabled"))
                return;

            _context = context;
            _roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            _userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            await AddRolesAsync([RolesNames.SuperAdmin, RolesNames.OfficeAdmin, RolesNames.User]);

            var superAdminUsers = await _userManager.GetUsersInRoleAsync(RolesNames.SuperAdmin);
            if (superAdminUsers.Any())
                return;

            var email = GetRequiredProvisioningValue(configuration, "Email");
            var password = GetRequiredProvisioningValue(configuration, "Password");
            var firstName = configuration["FirstAdminProvisioning:FirstName"] ?? "مدير";
            var lastName = configuration["FirstAdminProvisioning:LastName"] ?? "النظام";
            var personalNumber = GetRequiredProvisioningValue(configuration, "PersonalNumber");
            var phoneNumber = configuration["FirstAdminProvisioning:PhoneNumber"] ?? string.Empty;

            await CreateUserAsync(
                email: email,
                firstName: firstName,
                lastName: lastName,
                personalNumber: personalNumber,
                password: password,
                phoneNumber: phoneNumber,
                role: RolesNames.SuperAdmin,
                officeId: null,
                isApproved: true);
        }

        private static async Task SeedDevelopmentSuperAdminAsync(string password)
        {
            await CreateUserAsync(
                email: "admin@vehiclecontract.se",
                firstName: "مدير",
                lastName: "النظام",
                personalNumber: "1980010111111",
                password: password,
                phoneNumber: "0760000000",
                role: RolesNames.SuperAdmin,
                officeId: null,
                isApproved: true);
        }

        private static async Task SeedDemoAsync(string password)
        {
            var mainOffice = await CreateOfficeAsync(
                name: "مكتب بغداد التجريبي",
                licenseNumber: "DEMO-BG-001",
                address: "بغداد - الكرادة",
                phone: "0770000001",
                email: "baghdad.demo@vehiclecontract.local");

            var basraOffice = await CreateOfficeAsync(
                name: "مكتب البصرة التجريبي",
                licenseNumber: "DEMO-BS-001",
                address: "البصرة - العشار",
                phone: "0770000002",
                email: "basra.demo@vehiclecontract.local");

            var officeAdmin = await CreateUserAsync(
                email: "office.admin.demo@vehiclecontract.se",
                firstName: "مدير",
                lastName: "مكتب بغداد",
                personalNumber: "1976010111111",
                password: password,
                phoneNumber: "0780000001",
                role: RolesNames.OfficeAdmin,
                officeId: mainOffice.Id,
                isApproved: true);

            var approvedUser = await CreateUserAsync(
                email: "approved.user.demo@vehiclecontract.se",
                firstName: "موظف",
                lastName: "موافق عليه",
                personalNumber: "1984010122222",
                password: password,
                phoneNumber: "0790000001",
                role: RolesNames.User,
                officeId: mainOffice.Id,
                isApproved: true);

            await CreateUserAsync(
                email: "pending.user.demo@vehiclecontract.se",
                firstName: "مستخدم",
                lastName: "قيد الموافقة",
                personalNumber: "1990010133333",
                password: password,
                phoneNumber: "0790000002",
                role: RolesNames.User,
                officeId: null,
                isApproved: false);

            await SeedDemoVehiclesAsync(officeAdmin, approvedUser);
            await SeedDemoContractsAsync(mainOffice, basraOffice, officeAdmin, approvedUser);
        }

        private static async Task SeedCoreReferenceDataAsync()
        {
            await AddIfMissingAsync(_context.VehicleTypes, v => v.NameAr,
            [
                new VehicleType { NameAr = "خصوصي", NameEn = "Private" },
                new VehicleType { NameAr = "حمل", NameEn = "Cargo" },
                new VehicleType { NameAr = "أجرة", NameEn = "Taxi" },
                new VehicleType { NameAr = "زراعية", NameEn = "Agricultural" },
                new VehicleType { NameAr = "إنشائية", NameEn = "Construction" },
                new VehicleType { NameAr = "فحص مؤقت", NameEn = "Temporary Inspection" },
                new VehicleType { NameAr = "دراجة نارية", NameEn = "Motorcycle" }
            ]);
            await _context.SaveChangesAsync();

            var carType = await _context.VehicleTypes.FirstAsync(v => v.NameAr == "خصوصي");
            await AddVehicleModelIfMissingAsync("تويوتا كامري", "Toyota Camry", carType.Id);
            await AddVehicleModelIfMissingAsync("نيسان التيما", "Nissan Altima", carType.Id);
            await AddVehicleModelIfMissingAsync("كيا سيراتو", "Kia Cerato", carType.Id);

            await AddIfMissingAsync(_context.Colors, c => c.NameAr,
            [
                new Color { NameAr = "أبيض", NameEn = "White" },
                new Color { NameAr = "أسود", NameEn = "Black" },
                new Color { NameAr = "فضي", NameEn = "Silver" },
                new Color { NameAr = "رمادي", NameEn = "Gray" },
                new Color { NameAr = "أحمر", NameEn = "Red" },
                new Color { NameAr = "أزرق", NameEn = "Blue" }
            ]);
            await _context.SaveChangesAsync();

            await AddIfMissingAsync(_context.Provinces, p => p.NameAr,
            [
                new Province { NameAr = "بغداد", NameEn = "Baghdad" },
                new Province { NameAr = "البصرة", NameEn = "Basra" },
                new Province { NameAr = "نينوى", NameEn = "Nineveh" },
                new Province { NameAr = "الأنبار", NameEn = "Anbar" },
                new Province { NameAr = "أربيل", NameEn = "Erbil" },
                new Province { NameAr = "السليمانية", NameEn = "Sulaymaniyah" },
                new Province { NameAr = "دهوك", NameEn = "Dohuk" },
                new Province { NameAr = "كركوك", NameEn = "Kirkuk" },
                new Province { NameAr = "ديالى", NameEn = "Diyala" },
                new Province { NameAr = "صلاح الدين", NameEn = "Salah al-Din" },
                new Province { NameAr = "واسط", NameEn = "Wasit" },
                new Province { NameAr = "بابل", NameEn = "Babylon" },
                new Province { NameAr = "كربلاء المقدسة", NameEn = "Karbala" },
                new Province { NameAr = "النجف الأشرف", NameEn = "Najaf" },
                new Province { NameAr = "القادسية", NameEn = "Al-Qadisiyyah" },
                new Province { NameAr = "المثنى", NameEn = "Al Muthanna" },
                new Province { NameAr = "ذي قار", NameEn = "Dhi Qar" },
                new Province { NameAr = "ميسان", NameEn = "Maysan" },
                new Province { NameAr = "حلبجة", NameEn = "Halabja" }
            ]);
            await _context.SaveChangesAsync();

            await AddCityIfMissingAsync("الكرخ", "Karkh", "بغداد");
            await AddCityIfMissingAsync("الرصافة", "Rusafa", "بغداد");
            await AddCityIfMissingAsync("الكاظمية", "Kadhimiya", "بغداد");
            await AddCityIfMissingAsync("البصرة", "Basra City", "البصرة");
            await AddCityIfMissingAsync("الزبير", "Zubair", "البصرة");
            await AddCityIfMissingAsync("الموصل", "Mosul", "نينوى");

            await _context.SaveChangesAsync();
        }

        private static async Task<ApplicationUser> CreateUserAsync(
            string email,
            string firstName,
            string lastName,
            string personalNumber,
            string password,
            string phoneNumber,
            string role,
            int? officeId,
            bool isApproved)
        {
            var user = await _userManager.FindByEmailAsync(email);
            user ??= await _userManager.Users.FirstOrDefaultAsync(u => u.PersonalNumber == personalNumber);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    PersonalNumber = personalNumber,
                    PhoneNumber = phoneNumber,
                    RegisteredAt = DateTime.Now
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(string.Join("\n", createResult.Errors.Select(e => e.Description)));
            }

            user.UserName = email;
            user.Email = email;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.PersonalNumber = personalNumber;
            user.PhoneNumber = phoneNumber;
            user.OfficeId = officeId;
            user.IsApproved = isApproved;
            user.ApprovedAt = isApproved ? user.ApprovedAt ?? DateTime.Now : null;
            user.ApprovedBy = isApproved ? user.ApprovedBy ?? "Seed" : null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join("\n", updateResult.Errors.Select(e => e.Description)));

            await AddRoleToUserAsync(user, role);
            return user;
        }

        private static async Task AddRolesAsync(string[] roleNames)
        {
            foreach (var roleName in roleNames)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                    continue;

                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join("\n", result.Errors.Select(e => e.Description)));
            }
        }

        private static string GetRequiredProvisioningValue(IConfiguration configuration, string key)
        {
            return GetRequiredConfigurationValue(configuration, $"FirstAdminProvisioning:{key}");
        }

        private static string GetRequiredConfigurationValue(IConfiguration configuration, string key)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{key} is required when the related provisioning option is enabled.");

            return value.Trim();
        }

        private static async Task AddRoleToUserAsync(ApplicationUser user, string role)
        {
            if (await _userManager.IsInRoleAsync(user, role))
                return;

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("\n", result.Errors.Select(e => e.Description)));
        }

        private static async Task<Office> CreateOfficeAsync(
            string name,
            string licenseNumber,
            string address,
            string phone,
            string email)
        {
            var office = await _context.Offices.FirstOrDefaultAsync(o => o.LicenseNumber == licenseNumber);
            if (office == null)
            {
                office = new Office
                {
                    Name = name,
                    LicenseNumber = licenseNumber,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    EstablishedDate = new DateTime(2024, 1, 1),
                    IsActive = true
                };
                _context.Offices.Add(office);
                await _context.SaveChangesAsync();
            }

            return office;
        }

        private static async Task SeedDemoVehiclesAsync(ApplicationUser officeAdmin, ApplicationUser approvedUser)
        {
            var carType = await _context.VehicleTypes.FirstAsync(v => v.NameAr == "خصوصي");
            var camry = await _context.VehicleModels.FirstAsync(v => v.NameAr == "تويوتا كامري");
            var altima = await _context.VehicleModels.FirstAsync(v => v.NameAr == "نيسان التيما");
            var white = await _context.Colors.FirstAsync(c => c.NameAr == "أبيض");
            var black = await _context.Colors.FirstAsync(c => c.NameAr == "أسود");

            await CreateVehicleAsync(officeAdmin.Id, carType.Id, camry.Id, white.Id, "DEMO-1001", "DEMOCHASSIS1001", "تويوتا", "تويوتا كامري", "أبيض", "خصوصي", 2022);
            await CreateVehicleAsync(approvedUser.Id, carType.Id, altima.Id, black.Id, "DEMO-1002", "DEMOCHASSIS1002", "نيسان", "نيسان التيما", "أسود", "خصوصي", 2021);
        }

        private static async Task CreateVehicleAsync(
            string ownerId,
            int vehicleTypeId,
            int vehicleModelId,
            int colorId,
            string licenseNumber,
            string chassisNumber,
            string brand,
            string modelName,
            string colorName,
            string category,
            int manufactureYear)
        {
            if (await _context.Vehicles.AnyAsync(v => v.LicenseNumber == licenseNumber))
                return;

            _context.Vehicles.Add(new Vehicle
            {
                OwnerId = ownerId,
                VehicleTypeId = vehicleTypeId,
                VehicleModelId = vehicleModelId,
                ColorId = colorId,
                LicenseNumber = licenseNumber,
                ChassisNumber = chassisNumber,
                VehicleBrand = brand,
                VehicleModelName = modelName,
                VehicleColorName = colorName,
                VehicleCategory = category,
                ManufactureYear = manufactureYear,
                NumberOfWheels = 4
            });
            await _context.SaveChangesAsync();
        }

        private static async Task SeedDemoContractsAsync(
            Office mainOffice,
            Office basraOffice,
            ApplicationUser officeAdmin,
            ApplicationUser approvedUser)
        {
            var baghdad = await _context.Provinces.FirstAsync(p => p.NameAr == "بغداد");
            var karkh = await _context.Cities.FirstAsync(c => c.NameAr == "الكرخ");
            var basra = await _context.Provinces.FirstAsync(p => p.NameAr == "البصرة");
            var basraCity = await _context.Cities.FirstAsync(c => c.NameAr == "البصرة");

            var openContract = await CreateContractAsync(
                contractNumber: "DEMO-CT-2026-0001",
                officeId: mainOffice.Id,
                createdByUserId: officeAdmin.Id,
                sellerName: "أحمد علي",
                buyerName: "حسن محمد",
                provinceId: baghdad.Id,
                cityId: karkh.Id,
                status: ContractStatus.Open,
                vehiclePrice: 25000000,
                vehicleBrand: "تويوتا",
                vehicleModel: "تويوتا كامري",
                vehicleColor: "أبيض",
                chassisNumber: "DEMO-CONTRACT-CH-001",
                licenseNumber: "بغداد-1001");

            await CreatePaymentAsync(openContract, officeAdmin, 5000000, "دفعة أولى");

            var closedContract = await CreateContractAsync(
                contractNumber: "DEMO-CT-2026-0002",
                officeId: basraOffice.Id,
                createdByUserId: approvedUser.Id,
                sellerName: "سالم كريم",
                buyerName: "علي حسين",
                provinceId: basra.Id,
                cityId: basraCity.Id,
                status: ContractStatus.Closed,
                vehiclePrice: 18000000,
                vehicleBrand: "نيسان",
                vehicleModel: "نيسان التيما",
                vehicleColor: "أسود",
                chassisNumber: "DEMO-CONTRACT-CH-002",
                licenseNumber: "البصرة-2002");

            await CreatePaymentAsync(closedContract, approvedUser, 18000000, "تم تسديد كامل المبلغ");
        }

        private static async Task<Contract> CreateContractAsync(
            string contractNumber,
            int officeId,
            string createdByUserId,
            string sellerName,
            string buyerName,
            int provinceId,
            int cityId,
            ContractStatus status,
            decimal vehiclePrice,
            string vehicleBrand,
            string vehicleModel,
            string vehicleColor,
            string chassisNumber,
            string licenseNumber)
        {
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.ContractNumber == contractNumber);
            if (contract != null)
                return contract;

            var vehicleType = await _context.VehicleTypes.FirstAsync(v => v.NameAr == "خصوصي");
            var vehicleModelLookup = await _context.VehicleModels.FirstAsync(v => v.NameAr == vehicleModel);
            var colorLookup = await _context.Colors.FirstAsync(c => c.NameAr == vehicleColor);

            contract = new Contract
            {
                ContractNumber = contractNumber,
                ContractDate = DateTime.Today,
                OfficeId = officeId,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.Now,
                SellerName = sellerName,
                SellerPhone = "07701112233",
                SellerIdNumber = $"SELL-{contractNumber[^4..]}",
                SellerAddress = "بغداد",
                BuyerName = buyerName,
                BuyerPhone = "07801112233",
                BuyerIdNumber = $"BUY-{contractNumber[^4..]}",
                BuyerAddress = "بغداد",
                CurrentOwnerName = sellerName,
                VehicleBrand = vehicleBrand,
                VehicleTypeId = vehicleType.Id,
                VehicleModelId = vehicleModelLookup.Id,
                ColorId = colorLookup.Id,
                VehicleModel = vehicleModel,
                VehicleCategory = "خصوصي",
                VehicleColor = vehicleColor,
                ManufactureYear = 2022,
                ChassisNumber = chassisNumber,
                LicenseNumber = licenseNumber,
                ProvinceId = provinceId,
                CityId = cityId,
                Neighborhood = "101",
                AlleyNumber = "12",
                HouseNumber = "8",
                VehiclePrice = vehiclePrice,
                Status = status,
                Notes = "بيانات تجريبية لبيئة التطوير"
            };

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();
            return contract;
        }

        private static async Task CreatePaymentAsync(
            Contract contract,
            ApplicationUser recordedBy,
            decimal amount,
            string notes)
        {
            if (await _context.Payments.AnyAsync(p => p.ContractId == contract.Id && p.Amount == amount && p.Notes == notes))
                return;

            _context.Payments.Add(new Payment
            {
                ContractId = contract.Id,
                RecordedByUserId = recordedBy.Id,
                Amount = amount,
                PaymentDate = DateTime.Today,
                PaymentMethod = "نقداً",
                Notes = notes,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        private static async Task BackfillContractVehiclesAsync()
        {
            var contracts = await _context.Contracts
                .Where(c => c.VehicleId == null ||
                            c.VehicleTypeNameSnapshot == null ||
                            c.VehicleModelNameSnapshot == null ||
                            c.VehicleColorNameSnapshot == null ||
                            c.LicenseNumberSnapshot == null ||
                            c.ChassisNumberSnapshot == null ||
                            c.ManufactureYearSnapshot == null)
                .ToListAsync();

            foreach (var contract in contracts)
            {
                var vehicle = contract.VehicleId.HasValue
                    ? await _context.Vehicles.FindAsync(contract.VehicleId.Value)
                    : await FindVehicleForContractAsync(contract);

                if (vehicle == null)
                {
                    vehicle = await CreateVehicleFromContractAsync(contract);
                }

                contract.VehicleId ??= vehicle.Id;
                await ApplyVehicleSnapshotsAsync(contract, vehicle);
            }

            await _context.SaveChangesAsync();
        }

        private static async Task<Vehicle?> FindVehicleForContractAsync(Contract contract)
        {
            if (!string.IsNullOrWhiteSpace(contract.ChassisNumber))
            {
                var vehicleByChassis = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.ChassisNumber == contract.ChassisNumber);
                if (vehicleByChassis != null)
                    return vehicleByChassis;
            }

            if (!string.IsNullOrWhiteSpace(contract.LicenseNumber))
            {
                var vehicleByLicense = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.LicenseNumber == contract.LicenseNumber);
                if (vehicleByLicense != null)
                    return vehicleByLicense;
            }

            return null;
        }

        private static async Task<Vehicle> CreateVehicleFromContractAsync(Contract contract)
        {
            var vehicleType = await ResolveVehicleTypeAsync(contract);
            var vehicleModel = await ResolveVehicleModelAsync(contract, vehicleType.Id);
            var color = await ResolveColorAsync(contract);

            var vehicle = new Vehicle
            {
                OwnerId = null,
                VehicleTypeId = vehicleType.Id,
                VehicleModelId = vehicleModel.Id,
                ColorId = color.Id,
                LicenseNumber = string.IsNullOrWhiteSpace(contract.LicenseNumber)
                    ? $"LEGACY-CONTRACT-{contract.Id}"
                    : contract.LicenseNumber,
                ChassisNumber = string.IsNullOrWhiteSpace(contract.ChassisNumber)
                    ? $"LEGACY-CHASSIS-{contract.Id}"
                    : contract.ChassisNumber,
                VehicleBrand = contract.VehicleBrand,
                VehicleModelName = string.IsNullOrWhiteSpace(contract.VehicleModel)
                    ? vehicleModel.NameAr
                    : contract.VehicleModel,
                VehicleColorName = string.IsNullOrWhiteSpace(contract.VehicleColor)
                    ? color.NameAr
                    : contract.VehicleColor,
                VehicleCategory = string.IsNullOrWhiteSpace(contract.VehicleCategory)
                    ? vehicleType.NameAr
                    : contract.VehicleCategory,
                ManufactureYear = contract.ManufactureYear,
                NumberOfWheels = 4
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return vehicle;
        }

        private static async Task ApplyVehicleSnapshotsAsync(Contract contract, Vehicle vehicle)
        {
            if (string.IsNullOrWhiteSpace(contract.VehicleTypeNameSnapshot))
            {
                contract.VehicleTypeNameSnapshot = !string.IsNullOrWhiteSpace(contract.VehicleCategory)
                    ? contract.VehicleCategory
                    : await _context.VehicleTypes
                        .Where(v => v.Id == vehicle.VehicleTypeId)
                        .Select(v => v.NameAr)
                        .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(contract.VehicleModelNameSnapshot))
            {
                contract.VehicleModelNameSnapshot = !string.IsNullOrWhiteSpace(contract.VehicleModel)
                    ? contract.VehicleModel
                    : await _context.VehicleModels
                        .Where(v => v.Id == vehicle.VehicleModelId)
                        .Select(v => v.NameAr)
                        .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(contract.VehicleColorNameSnapshot))
            {
                contract.VehicleColorNameSnapshot = !string.IsNullOrWhiteSpace(contract.VehicleColor)
                    ? contract.VehicleColor
                    : await _context.Colors
                        .Where(c => c.Id == vehicle.ColorId)
                        .Select(c => c.NameAr)
                        .FirstOrDefaultAsync();
            }

            if (string.IsNullOrWhiteSpace(contract.LicenseNumberSnapshot))
            {
                contract.LicenseNumberSnapshot = !string.IsNullOrWhiteSpace(contract.LicenseNumber)
                    ? contract.LicenseNumber
                    : vehicle.LicenseNumber;
            }

            if (string.IsNullOrWhiteSpace(contract.ChassisNumberSnapshot))
            {
                contract.ChassisNumberSnapshot = !string.IsNullOrWhiteSpace(contract.ChassisNumber)
                    ? contract.ChassisNumber
                    : vehicle.ChassisNumber;
            }

            contract.ManufactureYearSnapshot ??= contract.ManufactureYear > 0
                ? contract.ManufactureYear
                : vehicle.ManufactureYear;
        }

        private static async Task<VehicleType> ResolveVehicleTypeAsync(Contract contract)
        {
            if (contract.VehicleTypeId.HasValue)
            {
                var vehicleTypeById = await _context.VehicleTypes.FindAsync(contract.VehicleTypeId.Value);
                if (vehicleTypeById != null)
                    return vehicleTypeById;
            }

            if (!string.IsNullOrWhiteSpace(contract.VehicleCategory))
            {
                var vehicleTypeByName = await _context.VehicleTypes
                    .FirstOrDefaultAsync(v => v.NameAr == contract.VehicleCategory);
                if (vehicleTypeByName != null)
                    return vehicleTypeByName;
            }

            return await _context.VehicleTypes
                .OrderBy(v => v.Id)
                .FirstAsync();
        }

        private static async Task<VehicleModel> ResolveVehicleModelAsync(Contract contract, int vehicleTypeId)
        {
            if (contract.VehicleModelId.HasValue)
            {
                var vehicleModelById = await _context.VehicleModels.FindAsync(contract.VehicleModelId.Value);
                if (vehicleModelById != null)
                    return vehicleModelById;
            }

            if (!string.IsNullOrWhiteSpace(contract.VehicleModel))
            {
                var vehicleModelByName = await _context.VehicleModels
                    .FirstOrDefaultAsync(v => v.NameAr == contract.VehicleModel);
                if (vehicleModelByName != null)
                    return vehicleModelByName;
            }

            return await _context.VehicleModels
                .Where(v => v.VehicleTypeId == vehicleTypeId)
                .OrderBy(v => v.Id)
                .FirstAsync();
        }

        private static async Task<Color> ResolveColorAsync(Contract contract)
        {
            if (contract.ColorId.HasValue)
            {
                var colorById = await _context.Colors.FindAsync(contract.ColorId.Value);
                if (colorById != null)
                    return colorById;
            }

            if (!string.IsNullOrWhiteSpace(contract.VehicleColor))
            {
                var colorByName = await _context.Colors
                    .FirstOrDefaultAsync(c => c.NameAr == contract.VehicleColor);
                if (colorByName != null)
                    return colorByName;
            }

            return await _context.Colors
                .OrderBy(c => c.Id)
                .FirstAsync();
        }

        private static async Task AddVehicleModelIfMissingAsync(string nameAr, string nameEn, int vehicleTypeId)
        {
            if (await _context.VehicleModels.AnyAsync(v => v.NameAr == nameAr))
                return;

            _context.VehicleModels.Add(new VehicleModel
            {
                NameAr = nameAr,
                NameEn = nameEn,
                VehicleTypeId = vehicleTypeId
            });
            await _context.SaveChangesAsync();
        }

        private static async Task AddCityIfMissingAsync(string nameAr, string nameEn, string provinceNameAr)
        {
            if (await _context.Cities.AnyAsync(c => c.NameAr == nameAr))
                return;

            var province = await _context.Provinces.FirstAsync(p => p.NameAr == provinceNameAr);
            _context.Cities.Add(new City
            {
                NameAr = nameAr,
                NameEn = nameEn,
                ProvinceId = province.Id
            });
            await _context.SaveChangesAsync();
        }

        private static async Task AddIfMissingAsync<TEntity>(
            DbSet<TEntity> dbSet,
            Func<TEntity, string> keySelector,
            IEnumerable<TEntity> entities)
            where TEntity : class
        {
            var existing = (await dbSet.ToListAsync())
                .Select(keySelector)
                .ToHashSet();

            foreach (var entity in entities)
            {
                if (!existing.Contains(keySelector(entity)))
                    dbSet.Add(entity);
            }
        }

        private static async Task SyncVehicleStatusesAsync()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Contracts)
                .ToListAsync();

            foreach (var vehicle in vehicles)
            {
                if (vehicle.Contracts.Any(c => c.Status == ContractStatus.Open))
                {
                    vehicle.Status = VehicleStatus.UnderContract;
                }
                else if (vehicle.Contracts.Any(c => c.Status == ContractStatus.Closed))
                {
                    vehicle.Status = VehicleStatus.Sold;
                }
                else if (vehicle.Status == default)
                {
                    vehicle.Status = VehicleStatus.Available;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
