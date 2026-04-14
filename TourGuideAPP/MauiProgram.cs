using Microsoft.Extensions.Logging;
using Supabase;
using TourGuideAPP.Services;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TourGuideAPP.Views;
 
namespace TourGuideAPP;
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
 
        // Supabase client
        var supabase = new Client(
            Constants.SupabaseUrl,
            Constants.SupabaseKey
        );
        builder.Services.AddSingleton(supabase);
 
        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ToursPage>();
        builder.Services.AddTransient<AccountPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<SubscriptionPage>();

        // Services
        builder.Services.AddSingleton<LocationService>();
        builder.Services.AddSingleton<GeofenceEngine>();
        builder.Services.AddSingleton<POIService>();
        builder.Services.AddSingleton<NarrationService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<PlaceService>();
        builder.Services.AddSingleton<AccessSessionService>();
        builder.Services.AddSingleton<UserProfileService>();
 
        // App
        builder.Services.AddSingleton<App>();
 
#if DEBUG
        builder.Logging.AddDebug();
#endif
 
        return builder.Build();
    }
}