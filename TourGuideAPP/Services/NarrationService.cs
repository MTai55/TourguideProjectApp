namespace TourGuideAPP.Services;

public class NarrationService
{
    private const string LocaleKey = "tts_preferred_locale";
    private const string DefaultLocale = "vi-VN";

    private CancellationTokenSource? _cts;
    private bool _isSpeaking = false;

    // Locale user chọn trong cài đặt, lưu Preferences
    public string PreferredLocale
    {
        get => Preferences.Get(LocaleKey, DefaultLocale);
        set => Preferences.Set(LocaleKey, value);
    }

    // Danh sách locale hỗ trợ để hiển thị trong UI
    public static readonly IReadOnlyList<(string Display, string Locale)> SupportedLocales =
    [
        ("Tiếng Việt",   "vi-VN"),
        ("English",      "en-US"),
        ("中文",          "zh-CN"),
        ("한국어",        "ko-KR"),
        ("日本語",        "ja-JP"),
        ("Français",     "fr-FR"),
        ("ภาษาไทย",      "th-TH"),
    ];

    /// <summary>
    /// Đọc text. locale ưu tiên theo thứ tự:
    /// 1. placeLocale (nếu place có TtsLocale riêng trong DB)
    /// 2. PreferredLocale (user chọn trong cài đặt)
    /// 3. vi-VN (mặc định)
    /// </summary>
    public async Task SpeakAsync(string text, string? placeLocale = null)
    {
        if (_isSpeaking || string.IsNullOrEmpty(text)) return;

        var locale = !string.IsNullOrWhiteSpace(placeLocale)
            ? placeLocale
            : PreferredLocale;

        try
        {
            _cts = new CancellationTokenSource();
            _isSpeaking = true;

            var options = new SpeechOptions { Volume = 1.0f, Pitch = 1.0f };

            // Tìm locale phù hợp trên thiết bị
            var availableLocales = await TextToSpeech.GetLocalesAsync();
            var parts = locale.Split('-');
            var lang = parts[0];
            var country = parts.Length > 1 ? parts[1] : "";

            // Tìm khớp chính xác lang + country trước
            var match = availableLocales.FirstOrDefault(l =>
                l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase) &&
                l.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

            // Fallback: chỉ cần khớp ngôn ngữ
            match ??= availableLocales.FirstOrDefault(l =>
                l.Language.Equals(lang, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                options.Locale = match;

            await TextToSpeech.SpeakAsync(text, options, _cts.Token);
        }
        finally
        {
            _isSpeaking = false;
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _isSpeaking = false;
    }

    public bool IsSpeaking => _isSpeaking;
}
