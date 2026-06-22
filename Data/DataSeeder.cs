using Microsoft.AspNetCore.Identity;
using TrieuDoanKy_W2.Models;

namespace TrieuDoanKy_W2.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@vlxd.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    EmailConfirmed = true,
                    PhoneNumber = "0123456789",
                    Address = "Hệ thống VLXD Shop"
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin@123");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Default Categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Xi Măng & Gạch", Description = "Vật liệu xây dựng cơ bản" },
                    new Category { Name = "Sơn & Chống Thấm", Description = "Sơn nội/ngoại thất, vật liệu chống thấm" },
                    new Category { Name = "Thép & Sắt", Description = "Sắt thép định hình, thép cuộn" },
                    new Category { Name = "Thiết Bị Vệ Sinh", Description = "Bồn cầu, lavabo, sen vòi" },
                    new Category { Name = "Điện & Chiếu Sáng", Description = "Dây cáp, công tắc, bóng đèn" },
                    new Category { Name = "Cửa & Cổng", Description = "Cửa cuốn, cửa nhôm kính" }
                };
                
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed basic products if empty
            if (!context.Products.Any() && context.Categories.Any())
            {
                var cats = context.Categories.ToList();
                var catXiMang = cats.FirstOrDefault(c => c.Name.Contains("Xi Măng"))?.Id ?? cats.First().Id;
                var catSon = cats.FirstOrDefault(c => c.Name.Contains("Sơn"))?.Id ?? cats.First().Id;
                var catThep = cats.FirstOrDefault(c => c.Name.Contains("Thép"))?.Id ?? cats.First().Id;
                var catVS = cats.FirstOrDefault(c => c.Name.Contains("Vệ Sinh"))?.Id ?? cats.First().Id;

                var products = new List<Product>
                {
                    new Product { Name = "Xi Măng Hà Tiên PCB40", Price = 95000, Unit = "bao", Stock = 100, CategoryId = catXiMang, Description = "Xi măng đa dụng chất lượng cao, phù hợp xây dựng dân dụng và công nghiệp", ImageUrl = "/images/cement.jpg" },
                    new Product { Name = "Gạch Ốp Lát Đồng Tâm 60x60", Price = 185000, Unit = "m²", Stock = 500, CategoryId = catXiMang, Description = "Gạch granite bóng kính cao cấp, độ bền cao", ImageUrl = "/images/tiles.jpg" },
                    new Product { Name = "Sơn Dulux Nội Thất 5L", Price = 420000, Unit = "thùng", Stock = 50, CategoryId = catSon, Description = "Sơn lau chùi hiệu quả, màu sắc tươi sáng bền lâu", ImageUrl = "/images/paint.jpg" },
                    new Product { Name = "Thép Hòa Phát D10", Price = 125000, Unit = "cây", Stock = 200, CategoryId = catThep, Description = "Thép gân phi 10, dùng trong kết cấu bê tông cốt thép", ImageUrl = "/images/steel.jpg" }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // 5. Update images for existing products missing images
            await SeedProductImagesAsync(context);
        }

        private static async Task SeedProductImagesAsync(ApplicationDbContext context)
        {
            var products = context.Products.Where(p => p.ImageUrl == null || p.ImageUrl == "").ToList();
            if (!products.Any()) return;

            foreach (var p in products)
            {
                var nameLower = p.Name.ToLower();
                p.ImageUrl = nameLower switch
                {
                    var n when n.Contains("xi măng") || n.Contains("ximăng") || n.Contains("cement") || n.Contains("pcb") => "/images/cement.jpg",
                    var n when n.Contains("gạch ốp") || n.Contains("gạch lát") || n.Contains("ceramic") || n.Contains("granite") || n.Contains("đồng tâm") => "/images/tiles.jpg",
                    var n when n.Contains("gạch xây") || n.Contains("gạch đặc") || n.Contains("gạch block") || n.Contains("gạch ống") => "/images/brick.jpg",
                    var n when n.Contains("thép") || n.Contains("sắt") || n.Contains("steel") || n.Contains("hòa phát") || n.Contains("vina") => "/images/steel.jpg",
                    var n when n.Contains("sơn") || n.Contains("paint") || n.Contains("dulux") || n.Contains("nippon") || n.Contains("kova") => "/images/paint.jpg",
                    var n when n.Contains("ống") || n.Contains("nhựa") || n.Contains("pipe") || n.Contains("tiền phong") || n.Contains("bình minh") => "/images/pipe.jpg",
                    var n when n.Contains("đá") || n.Contains("đá granit") || n.Contains("đá hoa") || n.Contains("marble") => "/images/granite.jpg",
                    var n when n.Contains("gỗ") || n.Contains("sàn gỗ") || n.Contains("wood") || n.Contains("laminate") => "/images/wood.jpg",
                    var n when n.Contains("cát") || n.Contains("sand") => "/images/sand.jpg",
                    var n when n.Contains("vệ sinh") || n.Contains("lavabo") || n.Contains("bồn cầu") || n.Contains("inax") || n.Contains("toto") => "/images/pipe.jpg",
                    _ => "/images/cement.jpg"
                };
            }

            await context.SaveChangesAsync();
        }
    }
}
