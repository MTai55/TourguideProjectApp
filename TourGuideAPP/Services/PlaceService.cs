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
        // Bước 1: Load Places — tách riêng để lỗi PlaceImage không ảnh hưởng
        try
        {
            var result = await _supabase
                .From<Place>()
                .Get();

            _cachedPlaces = result.Models;
            System.Diagnostics.Debug.WriteLine($"✅ Load được {_cachedPlaces.Count} Places");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Lỗi lấy Places: {ex.Message}");
            return _cachedPlaces; // trả về cache cũ nếu có
        }

        // Bước 2: Load ảnh — lỗi thì bỏ qua, vẫn hiện Places
        try
        {
            var imagesResult = await _supabase
                .From<PlaceImage>()
                .Filter("IsMain", Postgrest.Constants.Operator.Equals, "true")
                .Get();

            var images = imagesResult.Models;

            foreach (var place in _cachedPlaces)
            {
                var image = images.FirstOrDefault(i => i.PlaceId == place.PlaceId);
                if (image != null)
                    place.ImageUrl = image.ImageUrl;
            }

            System.Diagnostics.Debug.WriteLine($"✅ Load được {images.Count} PlaceImages");
        }
        catch (Exception ex)
        {
            // Ảnh lỗi thì dùng placeholder — Places vẫn hiện bình thường
            System.Diagnostics.Debug.WriteLine($"⚠️ Lỗi lấy PlaceImages: {ex.Message}");
        }

        return _cachedPlaces;
    }

    public List<Place> GetCachedPlaces() => _cachedPlaces;
}