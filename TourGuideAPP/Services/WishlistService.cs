using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class WishlistService
{
    private readonly Supabase.Client _supabase;
    private readonly UserProfileService _profileService;

    public WishlistService(Supabase.Client supabase, UserProfileService profileService)
    {
        _supabase = supabase;
        _profileService = profileService;
    }

    // Lấy danh sách wishlist của user hiện tại
    public async Task<List<WishlistItem>> GetWishlistAsync()
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return new List<WishlistItem>();

            var result = await _supabase
                .From<WishlistItem>()
                .Filter("UserId", Postgrest.Constants.Operator.Equals, userId.ToString()!)
                .Get();

            return result.Models;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi lấy Wishlist: {ex.Message}");
            return new List<WishlistItem>();
        }
    }

    // Thêm vào wishlist kèm ghi chú
    public async Task<bool> AddWishlistAsync(int placeId, string? note = null)
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return false;

            await _supabase.From<WishlistItem>().Insert(new WishlistItem
            {
                UserId = userId.Value,
                PlaceId = placeId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            Console.WriteLine($"✅ Đã thêm PlaceId {placeId} vào Wishlist");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi thêm Wishlist: {ex.Message}");
            return false;
        }
    }

    // Xóa khỏi wishlist
    public async Task<bool> RemoveWishlistAsync(int placeId)
    {
        try
        {
            var userId = await _profileService.GetCurrentUserIdAsync();
            if (userId == null) return false;

            await _supabase
                .From<WishlistItem>()
                .Filter("UserId", Postgrest.Constants.Operator.Equals, userId.ToString()!)
                .Filter("PlaceId", Postgrest.Constants.Operator.Equals, placeId.ToString())
                .Delete();

            Console.WriteLine($"✅ Đã xóa PlaceId {placeId} khỏi Wishlist");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi xóa Wishlist: {ex.Message}");
            return false;
        }
    }
}