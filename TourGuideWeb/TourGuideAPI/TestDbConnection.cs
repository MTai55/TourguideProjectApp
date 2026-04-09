using TourGuideAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace TourGuideAPI;

/// <summary>
/// Database connection test service - use in Program.cs
/// </summary>
public static class DbConnectionTest
{
    public static async Task TestConnectionAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                Console.WriteLine("\n🔄 Testing database connection...");
                
                // Test 1: Can we connect?
                var canConnect = await context.Database.CanConnectAsync();
                Console.WriteLine($"✅ Can Connect: {canConnect}");
                
                if (!canConnect)
                {
                    Console.WriteLine("❌ Cannot connect to database!");
                    return;
                }
                
                // Test 2: How many places?
                var placeCount = await context.Places.CountAsync();
                Console.WriteLine($"📍 Total Places: {placeCount}");
                
                // Test 3: Pending places
                var pendingCount = await context.Places
                    .Where(p => p.Status == "Pending")
                    .CountAsync();
                Console.WriteLine($"⏳ Pending Places: {pendingCount}");
                
                // Test 4: Try admin endpoint simulation
                var adminPlaces = await context.Places
                    .Include(p => p.Owner)
                    .Include(p => p.Category)
                    .Include(p => p.Images)
                    .Where(p => !false || p.Status == "Pending")  // Pending filter
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new { p.PlaceId, p.Name, p.Status })
                    .ToListAsync();
                
                Console.WriteLine($"✅ Admin query works: {adminPlaces.Count} places returned");
                
                Console.WriteLine("✅ All database tests passed!\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Database Connection Error: {ex.Message}");
            Console.WriteLine($"🔍 InnerException: {ex.InnerException?.Message}");
            Console.WriteLine($"📍 StackTrace: {ex.StackTrace}\n");
            throw;
        }
    }
}
