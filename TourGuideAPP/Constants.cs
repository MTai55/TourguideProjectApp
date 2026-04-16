namespace TourGuideAPP;

public static class Constants
{
    public const string SupabaseUrl = "https://ktwhtiacpuelgrhascyw.supabase.co";
    public const string SupabaseKey = "sb_publishable_hFhO_pjP3djUzOJ0OpfJ3w_i7glriOV"; // paste full key vào đây

    // ── Thông tin ngân hàng để tạo VietQR ─────────────────────────────────────
    // ⚠️ QUAN TRỌNG: Thay bằng thông tin tài khoản THẬT trước khi deploy
    // Để test, sử dụng tài khoản/mã ngân hàng hợp lệ hoặc kiểm tra VietQR docs
    public const string BankId          = "VIB";           // ← Mã ngân hàng của bạn
    public const string BankAccount     = "310822005";   // ← Số tài khoản của bạn
    public const string BankAccountName = "NGUYEN HUY TOAN"; // ← Tên chủ tài khoản
}