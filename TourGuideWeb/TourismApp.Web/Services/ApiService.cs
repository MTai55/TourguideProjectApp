using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TourismApp.Web.Models;

namespace TourismApp.Web.Services;

public class ApiService(HttpClient http, IHttpContextAccessor accessor)
{
    // ── Tự động lấy token từ Session ─────────────────────────────
    private void SetAuthHeader()
    {
        var token = accessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    // ── Helper GET ────────────────────────────────────────────────
    private async Task<T?> GetAsync<T>(string url)
    {
        SetAuthHeader();
        var res = await http.GetAsync(url);
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return default;
        return JsonConvert.DeserializeObject<T>(json);
    }

    // ── Helper POST ───────────────────────────────────────────────
    private async Task<(bool Success, T? Data, string Error)> PostAsync<T>(string url, object body)
    {
        SetAuthHeader();
        var content = new StringContent(
            JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var res = await http.PostAsync(url, content);
        var json = await res.Content.ReadAsStringAsync();
        if (res.IsSuccessStatusCode)
            return (true, JsonConvert.DeserializeObject<T>(json), string.Empty);
        return (false, default, json);
    }

    // ── Helper PUT ────────────────────────────────────────────────
    private async Task<(bool Success, string Error)> PutAsync(string url, object body)
    {
        SetAuthHeader();
        var content = new StringContent(
            JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var res = await http.PutAsync(url, content);
        return (res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    // ── Helper DELETE ─────────────────────────────────────────────
    private async Task<bool> DeleteAsync(string url)
    {
        SetAuthHeader();
        var res = await http.DeleteAsync(url);
        return res.IsSuccessStatusCode;
    }

    // ══════════════════════════════════════════════════════════════
    // AUTH
    // ══════════════════════════════════════════════════════════════
    public Task<(bool, AuthResponse?, string)> LoginAsync(string email, string password)
        => PostAsync<AuthResponse>("/api/auth/login", new { email, password });

    public Task<(bool, AuthResponse?, string)> RegisterAsync(RegisterViewModel vm)
        => PostAsync<AuthResponse>("/api/auth/register", vm);

    // ══════════════════════════════════════════════════════════════
    // PLACES
    // ══════════════════════════════════════════════════════════════
    public Task<PlaceListResponse?> GetPlacesAsync(
    int page = 1, int pageSize = 20, string? search = null,
    string? categoryId = null, string? isApproved = null,
    string? sortBy = null, string? district = null,
    string? maxPrice = null)
    {
        var url = $"/api/places?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={search}";
        if (!string.IsNullOrEmpty(categoryId)) url += $"&categoryId={categoryId}";
        if (!string.IsNullOrEmpty(isApproved)) url += $"&isApproved={isApproved}";
        if (!string.IsNullOrEmpty(sortBy)) url += $"&sortBy={sortBy}";
        if (!string.IsNullOrEmpty(district)) url += $"&district={district}";
        if (!string.IsNullOrEmpty(maxPrice)) url += $"&maxPrice={maxPrice}";
        return GetAsync<PlaceListResponse>(url);
    }

    public Task<PlaceViewModel?> GetPlaceAsync(int id)
        => GetAsync<PlaceViewModel>($"/api/places/{id}");

    public Task<(bool, PlaceViewModel?, string)> CreatePlaceAsync(CreatePlaceViewModel vm)
        => PostAsync<PlaceViewModel>("/api/places", vm);

    public Task<(bool, string)> UpdatePlaceAsync(int id, CreatePlaceViewModel vm)
        => PutAsync($"/api/places/{id}", vm);

    public Task<bool> DeletePlaceAsync(int id)
        => DeleteAsync($"/api/places/{id}");

    public Task<PlaceListResponse?> GetMyPlacesAsync(int page = 1, string? search = null)
    {
        var url = $"/api/places/mine?page={page}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={search}";
        return GetAsync<PlaceListResponse>(url);
    }

    public Task<(bool, string)> UpdateOpenStatusAsync(int id, string openStatus)
        => PutAsync($"/api/places/{id}/status", openStatus);

    // ══════════════════════════════════════════════════════════════
    // REVIEWS
    // ══════════════════════════════════════════════════════════════
    public Task<List<ReviewViewModel>?> GetReviewsAsync(int placeId)
        => GetAsync<List<ReviewViewModel>>($"/api/reviews/{placeId}");

    public Task<(bool, string)> ReplyReviewAsync(int reviewId, string reply)
        => PutAsync($"/api/reviews/{reviewId}/reply", reply);

    public Task<List<ReviewViewModel>?> GetAllReviewsAsync(bool hiddenOnly = false)
        => GetAsync<List<ReviewViewModel>>($"/api/admin/reviews?hiddenOnly={hiddenOnly}");

    public Task<(bool, string)> HideReviewAsync(int id, string note)
        => PutAsync($"/api/admin/reviews/{id}/hide", new { note });

    public Task<(bool, string)> ShowReviewAsync(int id)
        => PutAsync($"/api/admin/reviews/{id}/show", new { });
    public Task<(bool, string)> CreateComplaintAsync(CreateComplaintViewModel vm)
    => PutAsync("/api/complaints", vm);

    // ══════════════════════════════════════════════════════════════
    // PROMOTIONS
    // ══════════════════════════════════════════════════════════════
    public Task<List<PromotionViewModel>?> GetPromotionsAllAsync()
        => GetAsync<List<PromotionViewModel>>("/api/promotions/mine"); 

    public Task<(bool, PromotionViewModel?, string)> CreatePromotionAsync(CreatePromotionViewModel vm)
        => PostAsync<PromotionViewModel>("/api/promotions", vm);

    public Task<bool> DeletePromotionAsync(int id)
        => DeleteAsync($"/api/promotions/{id}");

    // ══════════════════════════════════════════════════════════════
    // ANALYTICS
    // ══════════════════════════════════════════════════════════════
    public Task<DashboardViewModel?> GetDashboardAsync()
        => GetAsync<DashboardViewModel>("/api/analytics/dashboard");

    public Task<object?> GetVisitsAnalyticsAsync(int placeId)
        => GetAsync<object>($"/api/analytics/visits/{placeId}");

    // ══════════════════════════════════════════════════════════════
    // ADMIN
    // ══════════════════════════════════════════════════════════════
    public Task<AdminStatsViewModel?> GetAdminStatsAsync()
    => GetAsync<AdminStatsViewModel>("/api/analytics/admin/stats");

    public Task<List<UserViewModel>?> GetUsersAsync(string? search, string? role)
    {
        var url = "/api/admin/users";
        if (!string.IsNullOrEmpty(search)) url += $"?search={search}";
        if (!string.IsNullOrEmpty(role)) url += $"&role={role}";
        return GetAsync<List<UserViewModel>>(url);
    }

    public Task<(bool, string)> ToggleUserLockAsync(int id)
        => PutAsync($"/api/admin/users/{id}/lock", new { });

    public Task<(bool, string)> ChangeUserRoleAsync(int id, string role)
        => PutAsync($"/api/admin/users/{id}/role", new { role });

    public Task<List<PlaceViewModel>?> GetAdminPlacesAsync(bool pendingOnly = false)
        => GetAsync<List<PlaceViewModel>>($"/api/admin/places?pendingOnly={pendingOnly}");

    public Task<(bool, string)> ApprovePlaceAsync(int id)
        => PutAsync($"/api/admin/places/{id}/approve", new { });

    public Task<(bool, string)> RejectPlaceAsync(int id)
        => PutAsync($"/api/admin/places/{id}/reject", new { });

    public Task<List<ComplaintViewModel>?> GetComplaintsAsync(string status)
        => GetAsync<List<ComplaintViewModel>>($"/api/complaints?status={status}");

    public Task<(bool, string)> ResolveComplaintAsync(int id, string note, string action)
        => PutAsync($"/api/complaints/{id}/resolve", new { note, action });

    // ── Helper class phân trang ───────────────────────────────────────
    public class PlaceListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<PlaceViewModel> Items { get; set; } = new List<PlaceViewModel>();
    }
}