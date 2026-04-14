namespace TourGuideAPP;

public static class Constants
{
    public const string SupabaseUrl = "https://ktwhtiacpuelgrhascyw.supabase.co";
    public const string SupabaseKey = "sb_publishable_hFhO_pjP3djUzOJ0OpfJ3w_i7glriOV"; // paste full key vào đây

    // ── Thông tin ngân hàng để tạo VietQR ─────────────────────────────────────
    // ⚠️ QUAN TRỌNG: Thay bằng thông tin tài khoản THẬT trước khi deploy
    // Để test, sử dụng tài khoản/mã ngân hàng hợp lệ hoặc kiểm tra VietQR docs
    public const string BankId          = "MB";                    // Mã ngân hàng (MB, VCB, TCB, ACB...)
    public const string BankAccount     = "0123456789";            // ⚠️ SỬA: Số tài khoản thật
    public const string BankAccountName = "TOUR GUIDE APPLICATION"; // ⚠️ SỬA: Tên chủ TK thật
}