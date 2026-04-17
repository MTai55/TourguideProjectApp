namespace TourGuideAPP;

public static class Constants
{
    public const string SupabaseUrl = "https://ktwhtiacpuelgrhascyw.supabase.co";
    public const string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imt0d2h0aWFjcHVlbGdyaGFzY3l3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI1NDAxODgsImV4cCI6MjA4ODExNjE4OH0.DbexaSAhLnto6SB4yqFPecYX_diWYQ0S9Ac1GJFEi0A";

    // ── Thông tin ngân hàng để tạo VietQR ─────────────────────────────────────
    // ⚠️ QUAN TRỌNG: Thay bằng thông tin tài khoản THẬT trước khi deploy
    // Để test, sử dụng tài khoản/mã ngân hàng hợp lệ hoặc kiểm tra VietQR docs
    public const string BankId          = "VIB";           // ← Mã ngân hàng của bạn
    public const string BankAccount     = "310822005";   // ← Số tài khoản của bạn
    public const string BankAccountName = "NGUYEN HUY TOAN"; // ← Tên chủ tài khoản
}