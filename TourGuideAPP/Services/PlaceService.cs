using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class PlaceService
{
    private readonly Supabase.Client _supabase;
    private List<Place> _cachedPlaces = new();

    public PlaceService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Place>> GetAllPlacesAsync()
    {
        try
        {
            // Load tất cả places
            var result = await _supabase
                .From<Place>()
                .Get();

            _cachedPlaces = result.Models;

            // Load ảnh cho từng place
            var imagesResult = await _supabase
                .From<PlaceImage>()
                .Filter("IsMain", Postgrest.Constants.Operator.Equals, "true")
                .Get();

            var images = imagesResult.Models;

            // Gán ảnh vào từng place
            foreach (var place in _cachedPlaces)
            {
                var image = images.FirstOrDefault(i => i.PlaceId == place.PlaceId);
                if (image != null)
                    place.ImageUrl = image.ImageUrl;
            }

            Console.WriteLine($"✅ Load được {_cachedPlaces.Count} Places");
            return _cachedPlaces;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi lấy Places: {ex.Message}");
            return _cachedPlaces;
        }
    }

    public List<Place> GetCachedPlaces() => _cachedPlaces;
}