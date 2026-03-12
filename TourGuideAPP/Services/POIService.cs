using Supabase;
using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class POIService
{
    private readonly Client _supabase;
    private List<POI> _cachedPOIs = new();

    public POIService(Client supabase)
    {
        _supabase = supabase;
    }

    // Lấy danh sách POI từ Supabase
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        try
        {
            var result = await _supabase
                .From<POI>()
                .Get();
            _cachedPOIs = result.Models;
            return _cachedPOIs;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi lấy POI: {ex.Message}");
            return _cachedPOIs; // Trả về cache nếu lỗi
        }
    }

    public List<POI> GetCachedPOIs() => _cachedPOIs;
}