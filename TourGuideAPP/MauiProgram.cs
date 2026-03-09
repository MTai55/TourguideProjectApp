using Microsoft.Extensions.Logging;
using Supabase;

namespace TourGuideAPP;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Khởi tạo Supabase client
        var supabase = new Client(
            Constants.SupabaseUrl,
            Constants.SupabaseKey
        );
        builder.Services.AddSingleton(supabase);
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}