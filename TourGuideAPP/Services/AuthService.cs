namespace TourGuideAPP.Services;

public class AuthService
{
    private readonly Supabase.Client _supabase;

    public AuthService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public bool IsLoggedIn => _supabase.Auth.CurrentUser != null;

    public string? CurrentUserId => _supabase.Auth.CurrentUser?.Id;

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Auth.SignIn(email, password);
            return session?.User != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi đăng nhập: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password, string fullName)
    {
        try
        {
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "full_name", fullName }
                }
            };
            var session = await _supabase.Auth.SignUp(email, password, options);
            Console.WriteLine($"✅ Đăng ký: {session?.User?.Email}");
            return session?.User != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi đăng ký: {ex.Message}");
            Console.WriteLine($"❌ Chi tiết: {ex.InnerException?.Message}");
            return false;
        }
    }
    // Lấy email của user đang đăng nhập
    public string? CurrentUserEmail => _supabase.Auth.CurrentUser?.Email;

    public async Task LogoutAsync()
    {
        await _supabase.Auth.SignOut();
    }
}