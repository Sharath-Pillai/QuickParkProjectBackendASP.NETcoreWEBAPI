using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuickParkAPI.Models;

namespace QuickParkAPI.Data
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(AppDbContext db, IConfiguration config)
        {
            // Seed default admin if none exists
            var adminExists = await db.Users.AnyAsync(u => u.Role == "admin");
            if (!adminExists)
            {
                // Read admin password from configuration (set via Render env var: AdminPassword)
                var password = config["AdminPassword"];
                if (string.IsNullOrWhiteSpace(password) || password == "PLACEHOLDER_SET_VIA_ENV")
                {
                    throw new InvalidOperationException(
                        "AdminPassword is not configured. " +
                        "Set the 'AdminPassword' environment variable before starting the application.");
                }

                var admin = new User
                {
                    Name = "QuickPark Admin",
                    Email = "admin@quickpark.com",
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = "admin",
                    Verified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
                Console.WriteLine("Default admin seeded: admin@quickpark.com");
            }
        }
    }
}

