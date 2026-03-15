using Microsoft.Extensions.Logging;
using Supabase;
using TourGuideAPP.Services;
namespace TourGuideAPP;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TourGuideAPP.Views;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
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
        
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<GeofenceEngine>();
        builder.Services.AddSingleton<POIService>();
        builder.Services.AddSingleton<NarrationService>();
        builder.Services.AddTransient<MapPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}