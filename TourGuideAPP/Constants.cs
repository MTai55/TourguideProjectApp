namespace TourGuideAPP;

public static class Constants
{
    public const string SupabaseUrl = "https://ktwhtiacpuelgrhascyw.supabase.co";
    public const string SupabaseKey = "sb_publishable_hFhO_pjP3djUzOJ0OpfJ3w_i7glriOV"; // paste full key vào đây

    // ── Thông tin ngân hàng để tạo VietQR ─────────────────────────────────────
    // Thay bằng thông tin tài khoản thật trước khi deploy
    public const string BankId          = "MB";           // Mã ngân hàng (MB, VCB, TCB, ACB...)
    public const string BankAccount     = "0000000000";   // Số tài khoản
    public const string BankAccountName = "TOUR GUIDE APP"; // Tên chủ tài khoản
}