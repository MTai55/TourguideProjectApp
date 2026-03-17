using TourGuideAPP.Views;
using TourGuideAPP.Services;

namespace TourGuideAPP;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        Routing.RegisterRoute("MapPage", typeof(MapPage));
        Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
        Routing.RegisterRoute("QRScanPage", typeof(QRScanPage));
        Routing.RegisterRoute("PlaceDetailPage", typeof(PlaceDetailPage));
    }
}