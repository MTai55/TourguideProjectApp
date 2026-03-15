using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Models;

namespace TourGuideAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Place> Places { get; set; }
    public DbSet<PlaceImage> PlaceImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<UserTracking> UserTracking { get; set; }
    public DbSet<VisitHistory> VisitHistory { get; set; }
    public DbSet<Review> Reviews { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e => {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("User");
        });

        mb.Entity<Category>(e => {
            e.ToTable("Categories");
            e.HasKey(c => c.CategoryId);
        });

        mb.Entity<Place>(e => {
            e.ToTable("Places");
            e.HasKey(p => p.PlaceId);
            e.HasOne(p => p.Owner)
             .WithMany(u => u.OwnedPlaces)
             .HasForeignKey(p => p.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PlaceImage>(e => {
            e.ToTable("PlaceImages");
            e.HasKey(i => i.ImageId);
            e.HasOne(i => i.Place)
             .WithMany(p => p.Images)
             .HasForeignKey(i => i.PlaceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserTracking>(e => {
            e.ToTable("UserTracking");
            e.HasKey(t => t.TrackId);
            e.HasIndex(t => new { t.UserId, t.RecordedAt });
        });

        mb.Entity<VisitHistory>(e => {
            e.ToTable("VisitHistory");
            e.HasKey(v => v.VisitId);
            e.HasOne(v => v.User)
             .WithMany(u => u.VisitHistory)
             .HasForeignKey(v => v.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Place)
             .WithMany(p => p.VisitHistory)
             .HasForeignKey(v => v.PlaceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Review>(e => {
            e.ToTable("Reviews");
            e.HasKey(r => r.ReviewId);
            e.HasIndex(r => new { r.UserId, r.PlaceId }).IsUnique();
            e.Property(r => r.Rating).HasColumnType("smallint");
            e.HasOne(r => r.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Place)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.PlaceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Nhà hàng", Icon = "🍜", ColorHex = "#FF5733" },
            new Category { CategoryId = 2, Name = "Cà phê", Icon = "☕", ColorHex = "#8B4513" },
            new Category { CategoryId = 3, Name = "Khách sạn", Icon = "🏨", ColorHex = "#2196F3" },
            new Category { CategoryId = 4, Name = "Tham quan", Icon = "🏛️", ColorHex = "#4CAF50" },
            new Category { CategoryId = 5, Name = "Vui chơi", Icon = "🎡", ColorHex = "#9C27B0" },
            new Category { CategoryId = 6, Name = "Mua sắm", Icon = "🛍️", ColorHex = "#FF9800" }
        );
    }
}