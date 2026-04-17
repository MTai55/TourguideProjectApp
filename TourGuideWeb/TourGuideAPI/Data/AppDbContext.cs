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
    public DbSet<Message> Messages { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Subscription>     Subscriptions     { get; set; }
    public DbSet<DevicePoiVisit>   DevicePoiVisits   { get; set; }
    public DbSet<AccessPackage>      AccessPackages      { get; set; }
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<AccessSession>      AccessSessions      { get; set; }
    
    // ✅ Suppress strict migration warnings
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => 
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
    
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>(e => {
            e.ToTable("Users");
            e.HasKey(u => u.UserId);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20).HasDefaultValue("User");
            e.Property(u => u.PasswordHash).IsRequired(false);
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
            e.Property(r => r.IsHidden).HasDefaultValue(false); // thêm mới
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

        // Thêm cấu hình cho Promotion
        mb.Entity<Promotion>(e => {
            e.ToTable("Promotions");
            e.HasKey(p => p.PromoId);
            e.HasOne(p => p.Place)
             .WithMany(p => p.Promotions)
             .HasForeignKey(p => p.PlaceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Thêm cấu hình cho Staff
        mb.Entity<Staff>(e => {
            e.ToTable("Staff");
            e.HasKey(s => s.StaffId);
            e.HasOne(s => s.Place)
             .WithMany()
             .HasForeignKey(s => s.PlaceId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed dữ liệu cho Category
        mb.Entity<Category>().HasData(
            new Category { CategoryId = 1, Name = "Nhà hàng", Icon = "🍽️", ColorHex = "#ef4444" },
            new Category { CategoryId = 2, Name = "Quán ăn vặt", Icon = "🥢", ColorHex = "#f97316" },
            new Category { CategoryId = 3, Name = "Cà phê", Icon = "☕", ColorHex = "#8b5cf6" },
            new Category { CategoryId = 4, Name = "Trà sữa & Đồ uống", Icon = "🧋", ColorHex = "#ec4899" },
            new Category { CategoryId = 5, Name = "Bánh & Tráng miệng", Icon = "🍰", ColorHex = "#eab308" }
        );

        mb.Entity<DevicePoiVisit>(e => {
            e.ToTable("DevicePoiVisits");
            e.HasKey(v => v.VisitId);
            e.HasIndex(v => v.DeviceId);
            e.HasIndex(v => v.VisitedAt);
        });

        mb.Entity<AccessPackage>(e => {
            e.ToTable("AccessPackages");
            e.HasKey(p => p.PackageId);
        });

        mb.Entity<DeviceRegistration>(e => {
            e.ToTable("DeviceRegistrations");
            e.HasKey(d => d.DeviceId);
        });

        mb.Entity<AccessSession>(e => {
            e.ToTable("AccessSessions");
            e.HasKey(s => s.SessionId);
            e.HasIndex(s => s.DeviceId);
            e.HasIndex(s => s.IsActive);
        });

        mb.Entity<Message>().ToTable("Messages").HasKey(m => m.MessageId);
        mb.Entity<Promotion>().ToTable("Promotions").HasKey(p => p.PromoId);
        mb.Entity<SubscriptionPlan>().ToTable("SubscriptionPlans").HasKey(p => p.PlanId);
        
        mb.Entity<Subscription>().ToTable("Subscriptions").HasKey(s => s.SubId);
        mb.Entity<Subscription>()
            .HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Subscription>()
            .HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId);
    }
}
