using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.Models;

namespace QuickParkAPI.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            // Seed default admin if none exists
            var adminExists = await db.Users.AnyAsync(u => u.Role == "admin");
            if (!adminExists)
            {
                var admin = new User
                {
                    Name = "QuickPark Admin",
                    Email = "admin@quickpark.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "admin",
                    Verified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
                Console.WriteLine("Default admin seeded: admin@quickpark.com / admin123");
            }
        }
    }
}
