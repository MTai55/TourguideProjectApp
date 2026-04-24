using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class AccessSessionService
{
    private const string DeviceIdKey   = "access_device_id";
    private const string ExpiresAtKey  = "access_expires_at";
    private const string SessionIdKey  = "access_session_id";

    private readonly Supabase.Client _supabase;

    private CancellationTokenSource? _pollCts;
    private CancellationTokenSource? _expiryCts;
    private CancellationTokenSource? _heartbeatCts;

    // App.xaml.cs subscribe vào event này để xử lý hết hạn
    public event Action? AccessExpired;

    public AccessSessionService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // ── Device ID ──────────────────────────────────────────────────────────────

    public string GetDeviceId()
    {
        var id = Preferences.Get(DeviceIdKey, string.Empty);
        if (!string.IsNullOrEmpty(id)) return id;

        // Tạo ID ngắn gọn 10 ký tự cho dễ nhận diện khi admin xác nhận
        id = Guid.NewGuid().ToString("N")[..10].ToUpper();
        Preferences.Set(DeviceIdKey, id);
        return id;
    }

    // ── Kiểm tra trạng thái ────────────────────────────────────────────────────

    public bool IsAccessValid()
    {
        var raw = Preferences.Get(ExpiresAtKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return false;
        if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
            return false;
        return DateTime.UtcNow < expiresAt;
    }

    public TimeSpan? GetRemainingTime()
    {
        var raw = Preferences.Get(ExpiresAtKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return null;
        if (!DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiresAt))
            return null;
        var remaining = expiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : null;
    }

    // ── Đăng ký thiết bị khi app khởi động ────────────────────────────────────

    public async Task RegisterDeviceAsync()
    {
        try
        {
            var deviceId = GetDeviceId();
            var platform = DeviceInfo.Platform == DevicePlatform.Android ? "Android" : "iOS";
            var now      = DateTime.UtcNow;

            // Upsert: tạo mới nếu chưa có, chỉ cập nhật LastSeenAt nếu đã có
            await _supabase.From<TourGuideAPP.Data.Models.DeviceRegistration>()
                .Upsert(new TourGuideAPP.Data.Models.DeviceRegistration
                {
                    DeviceId    = deviceId,
                    Platform    = platform,
                    FirstSeenAt = now,
                    LastSeenAt  = now
                });

            System.Diagnostics.Debug.WriteLine($"✅ Device registered: {deviceId} ({platform})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ RegisterDevice failed (non-critical): {ex.Message}");
        }
    }

    // ── Lấy danh sách gói từ Supabase ─────────────────────────────────────────

    // Fallback nếu Supabase không có bảng AccessPackages
    private static readonly List<TourGuideAPP.Data.Models.AccessPackage> _fallbackPackages =
    [
        new() { PackageId = "1h",   DurationHours = 1,  PriceVnd = 10_000,  IsActive = true, SortOrder = 1 },
        new() { PackageId = "2h",   DurationHours = 2,  PriceVnd = 18_000,  IsActive = true, SortOrder = 2 },
        new() { PackageId = "1day", DurationHours = 24, PriceVnd = 50_000,  IsActive = true, SortOrder = 3 },
        new() { PackageId = "3day", DurationHours = 72, PriceVnd = 120_000, IsActive = true, SortOrder = 4 },
    ];

    public async Task<List<TourGuideAPP.Data.Models.AccessPackage>> GetPackagesAsync()
    {
        try
        {
            var result = await _supabase
                .From<TourGuideAPP.Data.Models.AccessPackage>()
                .Filter("IsActive", Postgrest.Constants.Operator.Equals, "true")
                .Order("SortOrder", Postgrest.Constants.Ordering.Ascending)
                .Get();
            return result.Models.Count > 0 ? result.Models : _fallbackPackages;
        }
        catch
        {
            return _fallbackPackages;
        }
    }

    // ── Tạo session chờ thanh toán ─────────────────────────────────────────────

    public async Task<string> CreatePendingSessionAsync(string packageId, double durationHours, int priceVnd)
    {
        var session = new AccessSession
        {
            DeviceId      = GetDeviceId(),
            PackageId     = packageId,
            DurationHours = durationHours,
            PriceVnd      = priceVnd,
            IsActive      = false
        };

        var result  = await _supabase.From<AccessSession>().Insert(session);
        var created = result.Models.First();
        Preferences.Set(SessionIdKey, created.SessionId);
        return created.SessionId;
    }

    // ── Polling chờ admin kích hoạt ────────────────────────────────────────────

    public void StartPollingForActivation(string sessionId, Action onActivated)
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _supabase
                        .From<AccessSession>()
                        .Filter("SessionId", Postgrest.Constants.Operator.Equals, sessionId)
                        .Single();

                    if (result?.IsActive == true && result.ExpiresAt.HasValue)
                    {
                        // Lưu thời gian hết hạn vào local
                        Preferences.Set(ExpiresAtKey, result.ExpiresAt.Value.ToUniversalTime().ToString("O"));
                        _pollCts.Cancel();
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            onActivated();
                            StartExpiryTimer();
                        });
                        return;
                    }
                }
                catch { /* network error — thử lại sau */ }

                try { await Task.Delay(5000, token); }
                catch (TaskCanceledException) { return; }
            }
        }, token);
    }

    public void StopPolling() => _pollCts?.Cancel();

    // ── Timer kiểm tra hết hạn ────────────────────────────────────────────────

    public void StartExpiryTimer()
    {
        _expiryCts?.Cancel();
        _expiryCts = new CancellationTokenSource();
        var token = _expiryCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(60_000, token); }
                catch (TaskCanceledException) { return; }

                // Check local trước
                if (!IsAccessValid())
                {
                    ClearLocalSession();
                    MainThread.BeginInvokeOnMainThread(() => AccessExpired?.Invoke());
                    return;
                }

                // Check server — phát hiện admin thu hồi
                try
                {
                    var sessionId = Preferences.Get(SessionIdKey, string.Empty);
                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        var result = await _supabase
                            .From<TourGuideAPP.Data.Models.AccessSession>()
                            .Where(s => s.SessionId == sessionId)
                            .Single();

                        if (result == null || !result.IsActive)
                        {
                            ClearLocalSession();
                            MainThread.BeginInvokeOnMainThread(() => AccessExpired?.Invoke());
                            return;
                        }
                    }
                }
                catch { /* network error — bỏ qua, check lần sau */ }
            }
        }, token);
    }

    // ── Heartbeat — cập nhật LastSeenAt định kỳ ───────────────────────────────

    public void StopHeartbeat() => _heartbeatCts?.Cancel();

    public void StartHeartbeatTimer()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts = new CancellationTokenSource();
        var token = _heartbeatCts.Token;

        _ = Task.Run(async () =>
        {
            // Cập nhật ngay lập tức khi start (tránh 5s dead zone khi OnResume)
            await UpdateLastSeenAsync();

            while (!token.IsCancellationRequested)
            {
                try { await Task.Delay(5_000, token); }
                catch (TaskCanceledException) { return; }

                await UpdateLastSeenAsync();
            }
        }, token);
    }

    private async Task UpdateLastSeenAsync()
    {
        try
        {
            var deviceId = GetDeviceId();
            await _supabase.From<TourGuideAPP.Data.Models.DeviceRegistration>()
                .Where(d => d.DeviceId == deviceId)
                .Set(d => d.LastSeenAt!, DateTime.UtcNow)
                .Update();
        }
        catch { /* non-critical */ }
    }

    // ── Xóa session local (khi hết hạn hoặc reset) ────────────────────────────

    public void ClearLocalSession()
    {
        _pollCts?.Cancel();
        _expiryCts?.Cancel();
        _heartbeatCts?.Cancel();
        Preferences.Remove(ExpiresAtKey);
        Preferences.Remove(SessionIdKey);
    }
}
