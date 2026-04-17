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

    // ── Access Sessions ───────────────────────────────────────────
    public Task<SessionListResponse?> GetSessionsAsync(string status = "pending", int page = 1, string? search = null)
    {
        var url = $"/api/admin/sessions?status={status}&page={page}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={search}";
        return GetAsync<SessionListResponse>(url);
    }

    public Task<SessionStatsDto?> GetSessionStatsAsync()
        => GetAsync<SessionStatsDto>("/api/admin/sessions/stats");

    public Task<(bool, string)> ActivateSessionAsync(Guid sessionId)
        => PostAsync2($"/api/admin/sessions/{sessionId}/activate");

    public Task<(bool, string)> DeactivateSessionAsync(Guid sessionId)
        => PostAsync2($"/api/admin/sessions/{sessionId}/deactivate");

    public async Task<(bool, string)> DeleteSessionAsync(Guid sessionId)
    {
        SetAuthHeader();
        var res  = await http.DeleteAsync($"/api/admin/sessions/{sessionId}");
        var body = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, body);
    }

    // Helper POST không cần body
    private async Task<(bool, string)> PostAsync2(string url)
    {
        SetAuthHeader();
        var res  = await http.PostAsync(url, null);
        var body = await res.Content.ReadAsStringAsync();
        return (res.IsSuccessStatusCode, body);
    }

    public class SessionListResponse
    {
        [JsonProperty("total")]    public int Total { get; set; }
        [JsonProperty("page")]     public int Page { get; set; }
        [JsonProperty("pageSize")] public int PageSize { get; set; }
        [JsonProperty("items")]    public List<SessionDto> Items { get; set; } = new();
    }

    public class SessionDto
    {
        [JsonProperty("sessionId")]     public Guid SessionId { get; set; }
        [JsonProperty("deviceId")]      public string DeviceId { get; set; } = string.Empty;
        [JsonProperty("packageId")]     public string PackageId { get; set; } = string.Empty;
        [JsonProperty("durationHours")] public double DurationHours { get; set; }
        [JsonProperty("priceVnd")]      public int PriceVnd { get; set; }
        [JsonProperty("createdAt")]     public DateTime? CreatedAt { get; set; }
        [JsonProperty("activatedAt")]   public DateTime? ActivatedAt { get; set; }
        [JsonProperty("expiresAt")]     public DateTime? ExpiresAt { get; set; }
        [JsonProperty("isActive")]      public bool IsActive { get; set; }
    }

    public class SessionStatsDto
    {
        [JsonProperty("pending")] public int Pending { get; set; }
        [JsonProperty("active")]  public int Active { get; set; }
        [JsonProperty("total")]   public int Total { get; set; }
        [JsonProperty("revenue")] public long Revenue { get; set; }
    }

    // ── Access Packages ───────────────────────────────────────────
    public Task<List<AccessPackageDto>?> GetAccessPackagesAsync()
        => GetAsync<List<AccessPackageDto>>("/api/access-packages");

    public Task<(bool, string)> UpdateAccessPackageAsync(string id, double durationHours, int priceVnd, bool isActive, int sortOrder)
        => PutAsync($"/api/access-packages/{id}", new { durationHours, priceVnd, isActive, sortOrder });

    public class AccessPackageDto
    {
        [JsonProperty("packageId")]     public string PackageId { get; set; } = string.Empty;
        [JsonProperty("durationHours")] public double DurationHours { get; set; }
        [JsonProperty("priceVnd")]      public int PriceVnd { get; set; }
        [JsonProperty("isActive")]      public bool IsActive { get; set; }
        [JsonProperty("sortOrder")]     public int SortOrder { get; set; }
    }

    public Task<DeviceStatsResponse?> GetDeviceStatsAsync(int page = 1, int pageSize = 20, string? search = null)
    {
        var url = $"/api/admin/devices?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&search={search}";
        return GetAsync<DeviceStatsResponse>(url);
    }

    public Task<List<DeviceVisitDto>?> GetDeviceVisitsAsync(string deviceId)
        => GetAsync<List<DeviceVisitDto>>($"/api/admin/devices/{deviceId}/visits");

    public class DeviceStatsResponse
    {
        [JsonProperty("total")]    public int Total { get; set; }
        [JsonProperty("page")]     public int Page { get; set; }
        [JsonProperty("pageSize")] public int PageSize { get; set; }
        [JsonProperty("items")]    public List<DeviceStatItem> Items { get; set; } = new();
    }

    public class DeviceStatItem
    {
        [JsonProperty("deviceId")]    public string DeviceId { get; set; } = string.Empty;
        [JsonProperty("platform")]    public string? Platform { get; set; }
        [JsonProperty("firstSeenAt")] public DateTime? FirstSeenAt { get; set; }
        [JsonProperty("lastSeenAt")]  public DateTime? LastSeenAt { get; set; }
        [JsonProperty("visitCount")]  public int VisitCount { get; set; }
        [JsonProperty("poiCount")]    public int PoiCount { get; set; }
        [JsonProperty("firstVisit")]  public DateTime? FirstVisit { get; set; }
        [JsonProperty("lastVisit")]   public DateTime? LastVisit { get; set; }
        [JsonProperty("hasActive")]   public bool HasActive { get; set; }
        [JsonProperty("lastPackage")] public string? LastPackage { get; set; }
    }

    public class DeviceVisitDto
    {
        [JsonProperty("visitId")]    public long VisitId { get; set; }
        [JsonProperty("placeId")]    public int PlaceId { get; set; }
        [JsonProperty("placeName")]  public string? PlaceName { get; set; }
        [JsonProperty("visitMethod")]public string VisitMethod { get; set; } = string.Empty;
        [JsonProperty("visitedAt")]  public DateTime? VisitedAt { get; set; }
    }

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

    // ══════════════════════════════════════════════════════════════
    // TOURS
    // ══════════════════════════════════════════════════════════════
    public Task<List<TourViewModel>?> GetToursAsync()
        => GetAsync<List<TourViewModel>>("/api/tours");

    public async Task<bool> PostAsync(string url, object body)
    {
        SetAuthHeader();
        try
        {
            var content = new StringContent(
                JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var res = await http.PostAsync(url, content);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Exception calling POST {url}: {ex.Message}");
            return false;
        }
    }
}