using BidNest.Models;
using Microsoft.EntityFrameworkCore;

namespace BidNest.Services
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(BidnestContext context, IAuthService authService)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();

                if (!await context.Roles.AnyAsync())
                {
                    var roles = new List<Role>
                    {
                        new Role { Name = "Admin" },
                        new Role { Name = "User" }
                    };

                    context.Roles.AddRange(roles);
                    await context.SaveChangesAsync();
                    Console.WriteLine("Roles seeded successfully");
                }

                // Ensure roles exist before seeding admin user
                var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    Console.WriteLine("Admin role not found!");
                    return;
                }

                // Seed Admin User
                if (!await context.Users.AnyAsync(u => u.Email == "admin@gmail.com"))
                {
                    try
                    {
                        await authService.RegisterAsync("admin", "admin@gmail.com", "admin123", "System Administrator", adminRole.RoleId);
                        Console.WriteLine("Admin user seeded successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error seeding admin user: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Admin user already exists");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in database seeding: {ex.Message}");
            }

            // Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electronics", IsActive = true },
                    new Category { Name = "Furniture", IsActive = true },
                    new Category { Name = "Art & Collectibles", IsActive = true },
                    new Category { Name = "Jewelry", IsActive = true },
                    new Category { Name = "Vehicles", IsActive = true },
                    new Category { Name = "Books", IsActive = true }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // Seed Sample Items
            if (!await context.Items.AnyAsync())
            {
                var adminUser = await context.Users.FirstAsync(u => u.Email == "admin@gmail.com");
                var electronicsCategory = await context.Categories.FirstAsync(c => c.Name == "Electronics");
                var furnitureCategory = await context.Categories.FirstAsync(c => c.Name == "Furniture");

                var items = new List<Item>
                {
                    new Item
                    {
                        SellerId = adminUser.UserId,
                        Title = "Vintage Laptop Computer",
                        Description = "A rare vintage laptop in excellent condition. Perfect for collectors or retro computing enthusiasts.",
                        CategoryId = electronicsCategory.CategoryId,
                        MinBid = 50.00m,
                        BidIncrement = 5.00m,
                        StartDate = DateTime.UtcNow.AddDays(-1),
                        EndDate = DateTime.UtcNow.AddDays(7),
                        Status = "A",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Item
                    {
                        SellerId = adminUser.UserId,
                        Title = "Antique Wooden Chair",
                        Description = "Beautiful handcrafted wooden chair from the 1950s. Solid oak construction with original finish.",
                        CategoryId = furnitureCategory.CategoryId,
                        MinBid = 75.00m,
                        BidIncrement = 10.00m,
                        StartDate = DateTime.UtcNow.AddDays(-2),
                        EndDate = DateTime.UtcNow.AddDays(5),
                        Status = "A",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Item
                    {
                        SellerId = adminUser.UserId,
                        Title = "Classic Digital Camera",
                        Description = "Professional grade digital camera with original lens and accessories. Great for photography enthusiasts.",
                        CategoryId = electronicsCategory.CategoryId,
                        MinBid = 120.00m,
                        BidIncrement = 15.00m,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(10),
                        Status = "A",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Items.AddRange(items);
                await context.SaveChangesAsync();
            }
        }
    }
}
