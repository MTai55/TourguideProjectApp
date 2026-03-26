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
        Routing.RegisterRoute("QRScanPage", typeof(QRScanPage));
        Routing.RegisterRoute("PlaceDetailPage", typeof(PlaceDetailPage));
    }

    public void ActivateMapTab()
    {
        var mainTabs = Items.FirstOrDefault(i => i.Route == "MainTabs");
        if (mainTabs == null)
            return;

        CurrentItem = mainTabs;

        var mapTab = mainTabs.Items.FirstOrDefault(i => i.Route == "MapPage");
        if (mapTab != null)
        {
            mainTabs.CurrentItem = mapTab;
        }
    }
}

