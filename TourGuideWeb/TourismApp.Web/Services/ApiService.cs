using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http.Headers;
using System.Text;
using TourismApp.Web.Models;

namespace TourismApp.Web.Services;

public class ApiService(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiService> logger)
{
    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        }
    };

    // ── Tự động lấy token từ Session ─────────────────────────────
    private void SetAuthHeader()
    {
        var token = accessor.HttpContext?.Session.GetString("JwtToken");
        if (!string.IsNullOrEmpty(token))
        {
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            var prefix = token.Length > 50 ? token.Substring(0, 50) + "..." : token;
            logger.LogInformation($"✅ JWT Token set: {prefix} (total {token.Length} chars)");
        }
        else
        {
            logger.LogWarning("⚠️ No JWT Token in session");
            http.DefaultRequestHeaders.Remove("Authorization");  // Clear any old token
        }
    }

    // ── Helper GET ────────────────────────────────────────────────
    private async Task<T?> GetAsync<T>(string url)
    {
        SetAuthHeader();
        try
        {
            logger.LogInformation($"📡 GET {url}");
            var res = await http.GetAsync(url);
            var json = await res.Content.ReadAsStringAsync();
            logger.LogInformation($"   Response: [{res.StatusCode}] {json.Substring(0, Math.Min(100, json.Length))}");
            if (!res.IsSuccessStatusCode)
            {
                logger.LogError($"❌ API Error [{res.StatusCode}]: {url}\nFull response: {json}");
                return default;
            }
            return JsonConvert.DeserializeObject<T>(json, JsonSettings);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Exception calling {url}: {ex.Message}\n{ex.StackTrace}");
            return default;
        }
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
            return (true, JsonConvert.DeserializeObject<T>(json, JsonSettings), string.Empty);
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
    public async Task<(bool, PlaceViewModel?, string)> CreatePlaceAsync(CreatePlaceViewModel vm)
    {
        var body = new
        {
            name           = vm.Name,
            description    = vm.Description,
            address        = vm.Address,
            latitude       = vm.Latitude,
            longitude      = vm.Longitude,
            phone          = vm.Phone,
            categoryId     = vm.CategoryId,
            priceMin       = vm.PriceMin,
            priceMax       = vm.PriceMax,
            specialty      = vm.Specialty,
            pricePerPerson = vm.PricePerPerson,
            district       = vm.District,
            openTime       = vm.OpenTime,
            closeTime      = vm.CloseTime,
            hasParking     = vm.HasParking,
            hasAircon      = vm.HasAircon,
        };
        var (success, place, error) = await PostAsync<PlaceViewModel>("/api/places", body);
        return (success, place, error);
    }

    public async Task<(bool, string)> UpdatePlaceAsync(int id, CreatePlaceViewModel vm)
    {
        var body = new
        {
            name           = vm.Name,
            description    = vm.Description,
            address        = vm.Address,
            phone          = vm.Phone,
            openTime       = vm.OpenTime,
            closeTime      = vm.CloseTime,
            specialty      = vm.Specialty,
            pricePerPerson = vm.PricePerPerson,
            priceMin       = vm.PriceMin,
            priceMax       = vm.PriceMax,
            district       = vm.District,
            hasParking     = vm.HasParking,
            hasAircon      = vm.HasAircon,
        };
        var (success, error) = await PutAsync($"/api/places/{id}", body);
        return (success, error);    
    }
    public async Task<(bool, string)> UpdateTtsScriptAsync(int id, string? script)
    {
        var (ok, err) = await PutAsync($"/api/places/{id}/tts", new { ttsScript = script });
        return (ok, err);
    }
    // ══════════════════════════════════════════════════════════════
    // REVIEWS
    // ══════════════════════════════════════════════════════════════
    public Task<List<ReviewViewModel>?> GetReviewsAsync(int placeId)
        => GetAsync<List<ReviewViewModel>>($"/api/reviews/{placeId}");

    public Task<(bool, string)> ReplyReviewAsync(int reviewId, string reply)
        => PutAsync($"/api/reviews/{reviewId}/reply", reply);

    public async Task<List<ReviewViewModel>?> GetAllReviewsAsync(bool hiddenOnly = false)
    {
        var response = await GetAsync<AdminReviewsResponse>($"/api/admin/reviews?hiddenOnly={hiddenOnly}");
        return response?.Items ?? new List<ReviewViewModel>();
    }
    
    // Response wrapper cho admin reviews endpoint
    private class AdminReviewsResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; }
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        
        [JsonProperty("items")]
        public List<ReviewViewModel> Items { get; set; } = new();
    }

    public Task<(bool, string)> HideReviewAsync(int id, string note)
        => PutAsync($"/api/admin/reviews/{id}/hide", new { note });

    public Task<(bool, string)> ShowReviewAsync(int id)
        => PutAsync($"/api/admin/reviews/{id}/show", new { });

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

    public async Task<List<PlaceViewModel>?> GetAdminPlacesAsync(bool pendingOnly = false)
    {
        var response = await GetAsync<AdminPlacesResponse>($"/api/admin/places?pendingOnly={pendingOnly}");
        return response?.Items ?? new List<PlaceViewModel>();
    }
    
    // Response wrapper cho admin places endpoint
    private class AdminPlacesResponse
    {
        [JsonProperty("total")]
        public int Total { get; set; }
        
        [JsonProperty("page")]
        public int Page { get; set; }
        
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }
        
        [JsonProperty("items")]
        public List<PlaceViewModel> Items { get; set; } = new();
    }

    public Task<(bool, string)> SuspendPlaceAsync(int id)
        => PutAsync($"/api/admin/places/{id}/suspend", new { });

    // ── Helper class phân trang ───────────────────────────────────────
    public class PlaceListResponse
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<PlaceViewModel> Items { get; set; } = new List<PlaceViewModel>();
    }

    public Task<ProfileViewModel?> GetProfileAsync()
        => GetAsync<ProfileViewModel>("/api/auth/profile");
    public async Task<(bool, string)> UpdateProfileAsync(UpdateProfileViewModel vm)
    {
        var body = new { fullName = vm.FullName, email = vm.Email, phone = vm.Phone };
        var (ok, err) = await PutAsync("/api/auth/profile", body);
        return (ok, err);
    }
    public async Task<(bool, string)> ChangePasswordAsync(ChangePasswordViewModel vm)
    {
        var body = new { currentPassword = vm.CurrentPassword, newPassword = vm.NewPassword };
        var (ok, err) = await PutAsync("/api/auth/change-password", body);
        return (ok, err);
    }

    public Task<List<PlaceImageViewModel>?> GetPlaceImagesAsync(int placeId)
    => GetAsync<List<PlaceImageViewModel>>($"/api/places/{placeId}/images");

    // SUBSCRIPTION
    public Task<List<SubscriptionPlanViewModel>?> GetSubscriptionPlansAsync()
        => GetAsync<List<SubscriptionPlanViewModel>>("/api/subscriptions/plans");

    public Task<SubscriptionDto?> GetMySubscriptionAsync()
        => GetAsync<SubscriptionDto>("/api/subscriptions/mine");

    public Task<List<SubscriptionDto>?> GetSubscriptionHistoryAsync()
        => GetAsync<List<SubscriptionDto>>("/api/subscriptions/history");

    public async Task<(bool, string?, string)> CreateSubscriptionAsync(int planId, string paymentMethod)
    {
        var (ok, data, err) = await PostAsync<SubscriptionCreateResponse>(
            "/api/subscriptions",
            new { planId, paymentMethod });
        return (ok, data?.PaymentUrl, err);
    }

    public async Task<(bool, string)> CancelSubscriptionAsync(int subId)
    {
        var (ok, err) = await PutAsync($"/api/subscriptions/{subId}/cancel", new { });
        return (ok, err);
    }

    private class SubscriptionCreateResponse
    {
        [Newtonsoft.Json.JsonProperty("paymentUrl")]
        public string? PaymentUrl { get; set; }
        [Newtonsoft.Json.JsonProperty("subId")]
        public int SubId { get; set; }
    }
}