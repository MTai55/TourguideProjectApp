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

                if (!IsAccessValid())
                {
                    ClearLocalSession();
                    MainThread.BeginInvokeOnMainThread(() => AccessExpired?.Invoke());
                    return;
                }
            }
        }, token);
    }

    // ── Xóa session local (khi hết hạn hoặc reset) ────────────────────────────

    public void ClearLocalSession()
    {
        _pollCts?.Cancel();
        _expiryCts?.Cancel();
        Preferences.Remove(ExpiresAtKey);
        Preferences.Remove(SessionIdKey);
    }
}
