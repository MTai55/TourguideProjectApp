using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class FavoriteService
{
    private readonly Supabase.Client _supabase;
    private readonly UserProfileService _profileService;

    public FavoriteService(Supabase.Client supabase, UserProfileService profileService)
    {
        _supabase = supabase;
        _profileService = profileService;
    }

    // Lấy danh sách yêu thích của user hiện tại
    public async Task<List<Favorite>> GetFavoritesAsync()
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return new List<Favorite>();

            var result = await _supabase
                .From<Favorite>()
                .Filter("UserId", Postgrest.Constants.Operator.Equals, userId.ToString()!)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi lấy Favorites: {ex.Message}");
            return new List<Favorite>();
        }
    }

    // Kiểm tra place đã được yêu thích chưa
    public async Task<bool> IsFavoriteAsync(int placeId)
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return false;

            var result = await _supabase
                .From<Favorite>()
                .Filter("UserId", Postgrest.Constants.Operator.Equals, userId.ToString()!)
                .Filter("PlaceId", Postgrest.Constants.Operator.Equals, placeId.ToString())
                .Get();

            return result.Models.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    // Thêm vào yêu thích
    public async Task<bool> AddFavoriteAsync(int placeId)
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return false;

            await _supabase.From<Favorite>().Insert(new Favorite
            {
                UserId = userId.Value,
                PlaceId = placeId,
                CreatedAt = DateTime.UtcNow
            });

            Console.WriteLine($"✅ Đã thêm PlaceId {placeId} vào Favorites");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi thêm Favorite: {ex.Message}");
            return false;
        }
    }

    // Xóa khỏi yêu thích
    public async Task<bool> RemoveFavoriteAsync(int placeId)
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return false;

            await _supabase
                .From<Favorite>()
                .Filter("UserId", Postgrest.Constants.Operator.Equals, userId.ToString()!)
                .Filter("PlaceId", Postgrest.Constants.Operator.Equals, placeId.ToString())
                .Delete();

            Console.WriteLine($"✅ Đã xóa PlaceId {placeId} khỏi Favorites");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi xóa Favorite: {ex.Message}");
            return false;
        }
    }
}