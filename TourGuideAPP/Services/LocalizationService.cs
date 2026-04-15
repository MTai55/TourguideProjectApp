using System.Globalization;

namespace TourGuideAPP.Services;

public static class LocalizationService
{
    private const string PrefKey = "app_language";

    public static readonly (string Code, string DisplayName)[] SupportedLanguages =
    [
        ("vi", "Tiếng Việt"),
        ("en", "English"),
    ];

    /// <summary>True nếu người dùng đã chọn ngôn ngữ (không phải lần đầu mở app).</summary>
    public static bool IsLanguageSelected => Preferences.ContainsKey(PrefKey);

    /// <summary>Ngôn ngữ đã lưu, hoặc "vi" nếu chưa chọn.</summary>
    public static string SavedLanguage => Preferences.Get(PrefKey, "vi");

    /// <summary>Lưu lựa chọn và set culture ngay. Gọi trước khi build UI.</summary>
    public static void SaveAndApply(string langCode)
    {
        Preferences.Set(PrefKey, langCode);
        Apply(langCode);
    }

    /// <summary>Đọc preference và set culture. Gọi ở App constructor.</summary>
    public static void ApplySaved()
    {
        var lang = Preferences.Get(PrefKey, "vi");
        Apply(lang);
    }

    private static void Apply(string langCode)
    {
        var culture = new CultureInfo(langCode);
        CultureInfo.CurrentCulture   = culture;
        CultureInfo.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture   = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
}
