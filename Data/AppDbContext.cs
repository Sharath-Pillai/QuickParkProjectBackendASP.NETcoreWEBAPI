using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Models;

namespace QuickParkAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<ParkingSlot> ParkingSlots { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<BlockedDate> BlockedDates { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();
                e.Property(u => u.Role).HasDefaultValue("user");
                e.Property(u => u.Verified).HasDefaultValue(false);
                e.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // ParkingSlot -> Owner (restrict delete to prevent cascade issues)
            modelBuilder.Entity<ParkingSlot>(e =>
            {
                e.HasOne(p => p.Owner)
                 .WithMany(u => u.ParkingSlots)
                 .HasForeignKey(p => p.OwnerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(p => p.City);
                e.HasIndex(p => new { p.Status, p.IsActive });
            });

            // Review -> ParkingSlot
            modelBuilder.Entity<Review>(e =>
            {
                e.HasOne(r => r.ParkingSlot)
                 .WithMany(p => p.Reviews)
                 .HasForeignKey(r => r.ParkingSlotId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(r => r.User)
                 .WithMany(u => u.Reviews)
                 .HasForeignKey(r => r.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // BlockedDate -> ParkingSlot
            modelBuilder.Entity<BlockedDate>(e =>
            {
                e.HasOne(b => b.ParkingSlot)
                 .WithMany(p => p.BlockedDates)
                 .HasForeignKey(b => b.ParkingSlotId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Booking -> User, ParkingSlot
            modelBuilder.Entity<Booking>(e =>
            {
                e.HasOne(b => b.User)
                 .WithMany(u => u.Bookings)
                 .HasForeignKey(b => b.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(b => b.ParkingSlot)
                 .WithMany(p => p.Bookings)
                 .HasForeignKey(b => b.ParkingSlotId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(b => new { b.UserId, b.CreatedAt });
                e.HasIndex(b => new { b.ParkingSlotId, b.Status });
            });
        }
    }
}
