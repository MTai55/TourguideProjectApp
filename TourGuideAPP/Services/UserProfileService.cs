using System.Text.Json;
using Microsoft.Maui.Storage;
using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class UserProfileService
{
    private const string FavoritesKey = "user_profile_favorites";
    private const string WishlistKey = "user_profile_wishlist";
    private const string HistoryKey = "user_profile_history";
    private const string NotesKey = "user_profile_notes";
    private readonly AuthService _authService;
    private readonly Supabase.Client _supabase;

    public async Task<List<FavoritePlace>> GetFavoritesAsync()
    {
        return LoadList<FavoritePlace>(FavoritesKey);
    }

    public async Task<List<WishlistPlace>> GetWishlistAsync()
    {
        return LoadList<WishlistPlace>(WishlistKey);
    }

    public async Task<List<TripHistoryItem>> GetTripHistoryAsync()
    {
        return LoadList<TripHistoryItem>(HistoryKey);
    }

    public async Task<List<PlaceNote>> GetNotesAsync()
    {
        return LoadList<PlaceNote>(NotesKey);
    }

    public async Task AddFavoriteAsync(Place place)
    {
        var list = await GetFavoritesAsync();
        if (list.Any(x => x.PlaceId == place.PlaceId))
            return;

        list.Add(new FavoritePlace
        {
            PlaceId = place.PlaceId,
            Name = place.Name,
            Address = place.Address,
            ImageUrl = place.ImageUrl,
            AddedAt = DateTime.Now
        });
        SaveList(FavoritesKey, list);
    }

    public async Task RemoveFavoriteAsync(int placeId)
    {
        var list = await GetFavoritesAsync();
        list.RemoveAll(x => x.PlaceId == placeId);
        SaveList(FavoritesKey, list);
    }

    public async Task AddWishlistAsync(Place place)
    {
        var list = await GetWishlistAsync();
        if (list.Any(x => x.PlaceId == place.PlaceId))
            return;

        list.Add(new WishlistPlace
        {
            PlaceId = place.PlaceId,
            Name = place.Name,
            Address = place.Address,
            ImageUrl = place.ImageUrl,
            AddedAt = DateTime.Now
        });
        SaveList(WishlistKey, list);
    }

    public async Task RemoveWishlistAsync(int placeId)
    {
        var list = await GetWishlistAsync();
        list.RemoveAll(x => x.PlaceId == placeId);
        SaveList(WishlistKey, list);
    }

    public async Task AddHistoryAsync(Place place, string visitMethod = "Manual")
    {
        var list = await GetTripHistoryAsync();
        list.Insert(0, new TripHistoryItem
        {
            PlaceId = place.PlaceId,
            Name = place.Name,
            Address = place.Address,
            ImageUrl = place.ImageUrl,
            VisitedAt = DateTime.Now,
            VisitMethod = visitMethod
        });

        list = list.Take(100).ToList();
        SaveList(HistoryKey, list);
    }

    public async Task AddHistoryByGpsAsync(Place place)
    {
        await AddHistoryAsync(place, "GPS");
    }

    public async Task AddHistoryByQRAsync(Place place)
    {
        await AddHistoryAsync(place, "QR Code");
    }

    public async Task AddHistoryByBookingAsync(Place place)
    {
        await AddHistoryAsync(place, "Booking");
    }

    public async Task AddNoteAsync(int placeId, string placeName, string content)
    {
        var list = await GetNotesAsync();
        list.Add(new PlaceNote
        {
            PlaceId = placeId,
            Name = placeName,
            Content = content,
            CreatedAt = DateTime.Now
        });
        SaveList(NotesKey, list);
    }

    public async Task RemoveNoteAsync(PlaceNote note)
    {
        var list = await GetNotesAsync();
        list.RemoveAll(x => x.PlaceId == note.PlaceId && x.CreatedAt == note.CreatedAt && x.Content == note.Content);
        SaveList(NotesKey, list);
    }

    private static List<T> LoadList<T>(string key)
    {
        var json = Preferences.Get(key, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static void SaveList<T>(string key, List<T> list)
    {
        var json = JsonSerializer.Serialize(list);
        Preferences.Set(key, json);
    }

    public async Task ClearHistoryAsync()
    {
        Preferences.Remove(HistoryKey);
    }

    public async Task ClearAllAsync()
    {
        Preferences.Remove(FavoritesKey);
        Preferences.Remove(WishlistKey);
        Preferences.Remove(HistoryKey);
        Preferences.Remove(NotesKey);
    }
    public UserProfileService(AuthService authService, Supabase.Client supabase)
{
    _authService = authService;
    _supabase = supabase;
}

// Lấy UserId từ bảng Users dựa theo email đang đăng nhập
public async Task<int?> GetCurrentUserIdAsync()
{
    try
    {
        var email = _authService.CurrentUserEmail;
        if (string.IsNullOrEmpty(email)) return null;

        var result = await _supabase
            .From<TourGuideAPP.Data.Models.User>()
            .Filter("Email", Postgrest.Constants.Operator.Equals, email)
            .Get();

        return result.Models.FirstOrDefault()?.UserId;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Lỗi lấy UserId: {ex.Message}");
        return null;
    }
}
}
