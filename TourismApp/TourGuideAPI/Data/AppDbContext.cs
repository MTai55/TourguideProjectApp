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
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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
            e.Property(p => p.Specialty).HasMaxLength(500);
            e.Property(p => p.District).HasMaxLength(50);
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
            e.Property(r => r.TasteRating).HasColumnType("smallint");
            e.Property(r => r.PriceRating).HasColumnType("smallint");
            e.Property(r => r.SpaceRating).HasColumnType("smallint");
            e.HasOne(r => r.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Place)
             .WithMany(p => p.Reviews)
             .HasForeignKey(r => r.PlaceId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<RefreshToken>(e => {
            e.ToTable("RefreshTokens");
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Nhà hàng", Icon = "🍽️", ColorHex = "#ef4444" },
            new Category { CategoryId = 2, Name = "Quán ăn vặt", Icon = "🥢", ColorHex = "#f97316" },
            new Category { CategoryId = 3, Name = "Cà phê", Icon = "☕", ColorHex = "#8b5cf6" },
            new Category { CategoryId = 4, Name = "Trà sữa & Đồ uống", Icon = "🧋", ColorHex = "#ec4899" },
            new Category { CategoryId = 5, Name = "Bánh & Tráng miệng", Icon = "🍰", ColorHex = "#eab308" }
        );
    }
}