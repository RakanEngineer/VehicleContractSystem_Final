using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleContractSystem_Final.Models;

namespace VehicleContractSystem_Final.Data
{    
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Office> Offices { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<VehicleModel> VehicleModels { get; set; }
        public DbSet<Color> Colors { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique indexes
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PersonalNumber)
                .IsUnique();

            builder.Entity<Vehicle>()
                .HasIndex(v => v.LicenseNumber)
                .IsUnique();

            builder.Entity<Vehicle>()
                .HasIndex(v => v.ChassisNumber)
                .IsUnique();

            builder.Entity<Contract>()
                .HasIndex(c => c.ContractNumber)
                .IsUnique();

            builder.Entity<Contract>()
                .HasIndex(c => c.VerificationToken)
                .IsUnique()
                .HasFilter("[VerificationToken] IS NOT NULL");

            // Relationships

            // Office → ApplicationUser (One-to-Many)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Office)
                .WithMany(o => o.Staff)
                .HasForeignKey(u => u.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Office → Contract (One-to-Many)
            builder.Entity<Contract>()
                .HasOne(c => c.Office)
                .WithMany(o => o.Contracts)
                .HasForeignKey(c => c.OfficeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser → Contract (One-to-Many)
            builder.Entity<Contract>()
                .HasOne(c => c.CreatedByUser)
                .WithMany(u => u.CreatedContracts)
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne(c => c.Vehicle)
                .WithMany()
                .HasForeignKey(c => c.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VehicleModel>()
                .HasOne(vm => vm.VehicleType)
                .WithMany(vt => vt.VehicleModels)
                .HasForeignKey(vm => vm.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict); //  Cascade 

            builder.Entity<City>()
                .HasOne(c => c.Province)
                .WithMany(p => p.Cities)
                .HasForeignKey(c => c.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne(c => c.Province)
                .WithMany()
                .HasForeignKey(c => c.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne(c => c.City)
                .WithMany()
                .HasForeignKey(c => c.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne<VehicleType>()
                .WithMany()
                .HasForeignKey(c => c.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne<VehicleModel>()
                .WithMany()
                .HasForeignKey(c => c.VehicleModelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contract>()
                .HasOne<Color>()
                .WithMany()
                .HasForeignKey(c => c.ColorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Contract)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.RecordedBy)
                .WithMany()
                .HasForeignKey(p => p.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany(u => u.Vehicles)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vehicle>()
                .HasOne(v => v.VehicleType)
                .WithMany()
                .HasForeignKey(v => v.VehicleTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vehicle>()
                .HasOne(v => v.VehicleModel)
                .WithMany(vm => vm.Vehicles)
                .HasForeignKey(v => v.VehicleModelId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vehicle>()
                .HasOne(v => v.Color)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.ColorId)
                .OnDelete(DeleteBehavior.Restrict);


            // Seed data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed Roles
            //var superAdminRoleId = Guid.NewGuid().ToString();
            //var officeAdminRoleId = Guid.NewGuid().ToString();
            //var userRoleId = Guid.NewGuid().ToString();

            //builder.Entity<IdentityRole>().HasData(
            //    new IdentityRole
            //    {
            //        Id = "1a2b3c4d-0000-0000-0000-000000000001",
            //        Name = "SuperAdmin",
            //        NormalizedName = "SUPERADMIN"
            //    },
            //    new IdentityRole
            //    {
            //        Id = "1a2b3c4d-0000-0000-0000-000000000002",
            //        Name = "OfficeAdmin",
            //        NormalizedName = "OFFICEADMIN"
            //    },
            //    new IdentityRole
            //    {
            //        Id = "1a2b3c4d-0000-0000-0000-000000000003",
            //        Name = "User",
            //        NormalizedName = "USER"
            //    }
            //);

            // Seed Vehicle Types
            builder.Entity<VehicleType>().HasData(
                new VehicleType { Id = 1, NameAr = "سيارة", NameEn = "Car" },
                new VehicleType { Id = 2, NameAr = "دراجة نارية", NameEn = "Motorcycle" },
                new VehicleType { Id = 3, NameAr = "شاحنة", NameEn = "Truck" },
                new VehicleType { Id = 4, NameAr = "حافلة", NameEn = "Bus" }
            );

            // =============================
            // VEHICLE MODELS (نماذج فقط)
            // =============================

            builder.Entity<VehicleModel>().HasData(
                new VehicleModel { Id = 1, NameAr = "تويوتا كامري", NameEn = "Toyota Camry", VehicleTypeId = 1 },
                new VehicleModel { Id = 2, NameAr = "نيسان التيما", NameEn = "Nissan Altima", VehicleTypeId = 1 },
                new VehicleModel { Id = 3, NameAr = "كيا سيراتو", NameEn = "Kia Cerato", VehicleTypeId = 1 }
            );

            builder.Entity<Color>().HasData(
                new Color { Id = 1, NameAr = "أبيض", NameEn = "White" },
                new Color { Id = 2, NameAr = "أسود", NameEn = "Black" },
                new Color { Id = 3, NameAr = "فضي", NameEn = "Silver" },
                new Color { Id = 4, NameAr = "رمادي", NameEn = "Gray" },
                new Color { Id = 5, NameAr = "أحمر", NameEn = "Red" },
                new Color { Id = 6, NameAr = "أزرق", NameEn = "Blue" }
            );

            // Seed Offices
            builder.Entity<Office>().HasData(
                new Office
                {
                    Id = 1,
                    Name = "جوهرة الحديث لتجارة السيارات",
                    LicenseNumber = "BG-2024-001",
                    Address = "بغداد - الكرادة",
                    Phone = "07701234567",
                    Email = "info@jawhara.iq",
                    EstablishedDate = new DateTime(2020, 1, 1),
                    IsActive = true
                },
                new Office
                {
                    Id = 2,
                    Name = "الزوراء للسيارات",
                    LicenseNumber = "BG-2024-002",
                    Address = "بغداد - المنصور",
                    Phone = "07709876543",
                    Email = "info@zawraa.iq",
                    EstablishedDate = new DateTime(2020, 1, 1),
                    IsActive = true
                }
            );

            // Seed Iraqi Provinces
            builder.Entity<Province>().HasData(
                new Province { Id = 1, NameAr = "بغداد", NameEn = "Baghdad" },
                new Province { Id = 2, NameAr = "البصرة", NameEn = "Basra" },
                new Province { Id = 3, NameAr = "نينوى", NameEn = "Nineveh" },
                new Province { Id = 4, NameAr = "الأنبار", NameEn = "Anbar" },
                new Province { Id = 5, NameAr = "أربيل", NameEn = "Erbil" },
                new Province { Id = 6, NameAr = "السليمانية", NameEn = "Sulaymaniyah" },
                new Province { Id = 7, NameAr = "دهوك", NameEn = "Dohuk" },
                new Province { Id = 8, NameAr = "كركوك", NameEn = "Kirkuk" },
                new Province { Id = 9, NameAr = "ديالى", NameEn = "Diyala" },
                new Province { Id = 10, NameAr = "صلاح الدين", NameEn = "Salah al-Din" },
                new Province { Id = 11, NameAr = "واسط", NameEn = "Wasit" },
                new Province { Id = 12, NameAr = "بابل", NameEn = "Babylon" },
                new Province { Id = 13, NameAr = "كربلاء", NameEn = "Karbala" },
                new Province { Id = 14, NameAr = "النجف", NameEn = "Najaf" },
                new Province { Id = 15, NameAr = "القادسية", NameEn = "Al-Qadisiyyah" },
                new Province { Id = 16, NameAr = "المثنى", NameEn = "Al Muthanna" },
                new Province { Id = 17, NameAr = "ذي قار", NameEn = "Dhi Qar" },
                new Province { Id = 18, NameAr = "ميسان", NameEn = "Maysan" }
            );

            // Seed Cities (اختصار - نموذج فقط)
            var cities = new List<City>
            {
                // Baghdad
                new City { Id = 1, NameAr = "الكرخ", NameEn = "Karkh", ProvinceId = 1 },
                new City { Id = 2, NameAr = "الرصافة", NameEn = "Rusafa", ProvinceId = 1 },
                new City { Id = 3, NameAr = "الكاظمية", NameEn = "Kadhimiya", ProvinceId = 1 },
                new City { Id = 4, NameAr = "المدائن", NameEn = "Al-Mada'in", ProvinceId = 1 },
                new City { Id = 5, NameAr = "أبو غريب", NameEn = "Abu Ghraib", ProvinceId = 1 },
                new City { Id = 6, NameAr = "الصدر", NameEn = "Sadr City", ProvinceId = 1 },

                // Basra
                new City { Id = 7, NameAr = "البصرة", NameEn = "Basra City", ProvinceId = 2 },
                new City { Id = 8, NameAr = "الزبير", NameEn = "Zubair", ProvinceId = 2 },
                new City { Id = 9, NameAr = "أبو الخصيب", NameEn = "Abu Al-Khasib", ProvinceId = 2 },
                new City { Id = 10, NameAr = "الفاو", NameEn = "Faw", ProvinceId = 2 },
                new City { Id = 11, NameAr = "القرنة", NameEn = "Qurna", ProvinceId = 2 },

                // Nineveh
                new City { Id = 12, NameAr = "الموصل", NameEn = "Mosul", ProvinceId = 3 },
                new City { Id = 13, NameAr = "تلعفر", NameEn = "Tal Afar", ProvinceId = 3 },
                new City { Id = 14, NameAr = "سنجار", NameEn = "Sinjar", ProvinceId = 3 },
                new City { Id = 15, NameAr = "الحمدانية", NameEn = "Hamdaniya", ProvinceId = 3 }
            };

            builder.Entity<City>().HasData(cities);
        }
    }
}
