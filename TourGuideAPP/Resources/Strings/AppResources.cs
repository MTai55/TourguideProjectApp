using System.Globalization;
using System.Resources;

namespace TourGuideAPP.Resources.Strings;

/// <summary>
/// Accessor tĩnh cho resource strings đa ngôn ngữ.
/// Culture được set trước khi UI build nên x:Static tự dùng đúng ngôn ngữ.
/// </summary>
public static class AppResources
{
    private static readonly ResourceManager _rm =
        new("TourGuideAPP.Resources.Strings.AppResources", typeof(AppResources).Assembly);

    private static string Get(string key) =>
        _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    // Language Selection
    public static string LangTitle         => Get(nameof(LangTitle));
    public static string LangSubtitle      => Get(nameof(LangSubtitle));
    public static string LangVietnamese    => Get(nameof(LangVietnamese));
    public static string LangEnglish       => Get(nameof(LangEnglish));
    public static string LangContinue      => Get(nameof(LangContinue));

    // Tab Bar
    public static string TabTours          => Get(nameof(TabTours));
    public static string TabFeatured       => Get(nameof(TabFeatured));
    public static string TabMap            => Get(nameof(TabMap));
    public static string TabSettings       => Get(nameof(TabSettings));

    // SubscriptionPage
    public static string SubChoosePackage  => Get(nameof(SubChoosePackage));
    public static string SubPackage1hLabel => Get(nameof(SubPackage1hLabel));
    public static string SubPackage1hDesc  => Get(nameof(SubPackage1hDesc));
    public static string SubPackage1hSub   => Get(nameof(SubPackage1hSub));
    public static string SubPackage2hLabel => Get(nameof(SubPackage2hLabel));
    public static string SubPackage2hBadge => Get(nameof(SubPackage2hBadge));
    public static string SubPackage2hDesc  => Get(nameof(SubPackage2hDesc));
    public static string SubPackage2hSub   => Get(nameof(SubPackage2hSub));
    public static string SubPackage1dLabel => Get(nameof(SubPackage1dLabel));
    public static string SubPackage1dDesc  => Get(nameof(SubPackage1dDesc));
    public static string SubPackage1dSub   => Get(nameof(SubPackage1dSub));
    public static string SubPackage3dLabel => Get(nameof(SubPackage3dLabel));
    public static string SubPackage3dDesc  => Get(nameof(SubPackage3dDesc));
    public static string SubPackage3dSub   => Get(nameof(SubPackage3dSub));
    public static string SubSelectBtn      => Get(nameof(SubSelectBtn));
    public static string SubPaymentMethod  => Get(nameof(SubPaymentMethod));
    public static string SubAutoUnlock     => Get(nameof(SubAutoUnlock));
    public static string SubDevBypass      => Get(nameof(SubDevBypass));

    // PaymentPage
    public static string PayTitle          => Get(nameof(PayTitle));
    public static string PaySelectedPkg    => Get(nameof(PaySelectedPkg));
    public static string PayScanLabel      => Get(nameof(PayScanLabel));
    public static string PayDeviceCodeLabel=> Get(nameof(PayDeviceCodeLabel));
    public static string PayDeviceCodeHint => Get(nameof(PayDeviceCodeHint));
    public static string PayWaiting        => Get(nameof(PayWaiting));
    public static string PayConnError      => Get(nameof(PayConnError));
    public static string PayInstructions   => Get(nameof(PayInstructions));
    public static string PayStep1          => Get(nameof(PayStep1));
    public static string PayStep2          => Get(nameof(PayStep2));
    public static string PayStep3          => Get(nameof(PayStep3));
    public static string PayStep4          => Get(nameof(PayStep4));

    // AccountPage
    public static string AccHistorySection  => Get(nameof(AccHistorySection));
    public static string AccHistoryEmpty    => Get(nameof(AccHistoryEmpty));
    public static string AccHistoryClearBtn => Get(nameof(AccHistoryClearBtn));
    public static string AccHistoryClearTitle => Get(nameof(AccHistoryClearTitle));
    public static string AccHistoryClearMsg => Get(nameof(AccHistoryClearMsg));
    public static string AccHistoryClearYes => Get(nameof(AccHistoryClearYes));
    public static string AccApp            => Get(nameof(AccApp));
    public static string AccSettings       => Get(nameof(AccSettings));
    public static string AccExperience     => Get(nameof(AccExperience));
    public static string AccTtsVoice       => Get(nameof(AccTtsVoice));
    public static string AccNotifications  => Get(nameof(AccNotifications));
    public static string AccNotificationsOn=> Get(nameof(AccNotificationsOn));
    public static string AccDarkMode       => Get(nameof(AccDarkMode));
    public static string AccDarkModeOn     => Get(nameof(AccDarkModeOn));
    public static string AccMapSection     => Get(nameof(AccMapSection));
    public static string AccGpsAuto        => Get(nameof(AccGpsAuto));
    public static string AccGpsAutoSub     => Get(nameof(AccGpsAutoSub));
    public static string AccSearchRadius   => Get(nameof(AccSearchRadius));
    public static string AccSearchRadiusVal=> Get(nameof(AccSearchRadiusVal));
    public static string AccInfoSection    => Get(nameof(AccInfoSection));
    public static string AccAbout          => Get(nameof(AccAbout));
    public static string AccVersion        => Get(nameof(AccVersion));
    public static string AccUserGuide      => Get(nameof(AccUserGuide));
    public static string AccUserGuideSub   => Get(nameof(AccUserGuideSub));
    public static string AccClearCache     => Get(nameof(AccClearCache));
    public static string AccClearCacheSub  => Get(nameof(AccClearCacheSub));
    public static string AccDevDeactivate  => Get(nameof(AccDevDeactivate));

    // MainPage
    public static string MainGreeting      => Get(nameof(MainGreeting));
    public static string MainTraveler      => Get(nameof(MainTraveler));
    public static string MainSearchPlaceholder => Get(nameof(MainSearchPlaceholder));
    public static string MainWaitGps       => Get(nameof(MainWaitGps));
    public static string MainNoNearby      => Get(nameof(MainNoNearby));
    public static string MainExploreCategory => Get(nameof(MainExploreCategory));
    public static string MainCatAll        => Get(nameof(MainCatAll));
    public static string MainCatCafe       => Get(nameof(MainCatCafe));
    public static string MainCatFood       => Get(nameof(MainCatFood));
    public static string MainCatBeer       => Get(nameof(MainCatBeer));
    public static string MainCatBubbleTea  => Get(nameof(MainCatBubbleTea));
    public static string MainFeatured      => Get(nameof(MainFeatured));
    public static string MainGastronomy    => Get(nameof(MainGastronomy));
    public static string MainViewAll       => Get(nameof(MainViewAll));
    public static string MainSpecialty     => Get(nameof(MainSpecialty));
    public static string MainUserLabel     => Get(nameof(MainUserLabel));
    public static string MainGpsStarting   => Get(nameof(MainGpsStarting));
    public static string MainNoConnection  => Get(nameof(MainNoConnection));
    public static string MainNearbyPrefix  => Get(nameof(MainNearbyPrefix));

    // MapPage
    public static string MapExplore        => Get(nameof(MapExplore));
    public static string MapTitle          => Get(nameof(MapTitle));
    public static string MapFilterAll      => Get(nameof(MapFilterAll));
    public static string MapFilterCafe     => Get(nameof(MapFilterCafe));
    public static string MapFilterFood     => Get(nameof(MapFilterFood));
    public static string MapCancelRoute    => Get(nameof(MapCancelRoute));
    public static string MapNavigatingTo   => Get(nameof(MapNavigatingTo));
    public static string MapDestination    => Get(nameof(MapDestination));
    public static string MapAddressUpdating=> Get(nameof(MapAddressUpdating));
    public static string MapNoNearby       => Get(nameof(MapNoNearby));
    public static string MapOpenNow        => Get(nameof(MapOpenNow));
    public static string MapDirections     => Get(nameof(MapDirections));
    public static string MapNarrate        => Get(nameof(MapNarrate));
    public static string MapCall           => Get(nameof(MapCall));
    public static string MapDetails        => Get(nameof(MapDetails));

    // ToursPage
    public static string TourExplore       => Get(nameof(TourExplore));
    public static string TourPageTitle     => Get(nameof(TourPageTitle));
    public static string TourSubtitle      => Get(nameof(TourSubtitle));
    public static string TourSearchPlaceholder => Get(nameof(TourSearchPlaceholder));
    public static string TourFilterLabel   => Get(nameof(TourFilterLabel));
    public static string TourTimeFilter    => Get(nameof(TourTimeFilter));
    public static string TourBudgetFilter  => Get(nameof(TourBudgetFilter));
    public static string TourBudgetValue   => Get(nameof(TourBudgetValue));
    public static string TourPriorityFilter=> Get(nameof(TourPriorityFilter));
    public static string TourWalkLess      => Get(nameof(TourWalkLess));
    public static string TourApply         => Get(nameof(TourApply));
    public static string TourCuratedLabel  => Get(nameof(TourCuratedLabel));
    public static string TourSelect        => Get(nameof(TourSelect));

    // TourDetailPage
    public static string TourDetailLabel   => Get(nameof(TourDetailLabel));
    public static string TourDetailItinerary => Get(nameof(TourDetailItinerary));
    public static string TourDetailStart   => Get(nameof(TourDetailStart));

    // PlaceDetailPage
    public static string PlaceOpenNow      => Get(nameof(PlaceOpenNow));
    public static string PlaceDirections   => Get(nameof(PlaceDirections));
    public static string PlaceGoogleMaps   => Get(nameof(PlaceGoogleMaps));
    public static string PlaceCall         => Get(nameof(PlaceCall));
    public static string PlaceInfo         => Get(nameof(PlaceInfo));
    public static string PlaceNarrate      => Get(nameof(PlaceNarrate));

    // ToursPage (code-behind)
    public static string TourDuration1      => Get(nameof(TourDuration1));
    public static string TourDuration2      => Get(nameof(TourDuration2));
    public static string TourDurationHalfDay=> Get(nameof(TourDurationHalfDay));
    public static string TourEmptyTitle     => Get(nameof(TourEmptyTitle));
    public static string TourEmptyDesc      => Get(nameof(TourEmptyDesc));
    public static string TourSuggestionTag  => Get(nameof(TourSuggestionTag));
    public static string TourStopsFormat    => Get(nameof(TourStopsFormat));
    public static string TourLowWalkDesc    => Get(nameof(TourLowWalkDesc));
    public static string TourQuickTitle     => Get(nameof(TourQuickTitle));
    public static string TourQuickDesc      => Get(nameof(TourQuickDesc));
    public static string TourBalancedTitle  => Get(nameof(TourBalancedTitle));
    public static string TourBalancedDesc   => Get(nameof(TourBalancedDesc));
    public static string TourFullTitle      => Get(nameof(TourFullTitle));
    public static string TourFullDesc       => Get(nameof(TourFullDesc));
    public static string AlertSelectTour    => Get(nameof(AlertSelectTour));
    public static string AlertNoStops       => Get(nameof(AlertNoStops));
    public static string AlertServiceError  => Get(nameof(AlertServiceError));

    // PlaceDetailPage (code-behind)
    public static string PlaceNoRating       => Get(nameof(PlaceNoRating));
    public static string PlaceReviewsSuffix  => Get(nameof(PlaceReviewsSuffix));
    public static string PlaceClosedNow      => Get(nameof(PlaceClosedNow));
    public static string PlaceNoDescription  => Get(nameof(PlaceNoDescription));
    public static string PlaceNoUpdate       => Get(nameof(PlaceNoUpdate));
    public static string PlaceContactForPrice=> Get(nameof(PlaceContactForPrice));
    public static string PlaceNoPhone        => Get(nameof(PlaceNoPhone));

    // MapPage (code-behind)
    public static string MapNoCurrentLocation=> Get(nameof(MapNoCurrentLocation));
    public static string MapRouteError       => Get(nameof(MapRouteError));
    public static string MapRouteFailed      => Get(nameof(MapRouteFailed));

    // Generic alerts
    public static string AlertOk            => Get(nameof(AlertOk));
    public static string AlertError         => Get(nameof(AlertError));
    public static string AlertInfo          => Get(nameof(AlertInfo));
    public static string AlertNoCoords      => Get(nameof(AlertNoCoords));
    public static string AlertCannotCall    => Get(nameof(AlertCannotCall));
    public static string AlertCannotWebsite => Get(nameof(AlertCannotWebsite));

    // Alerts
    public static string AlertExpiredTitle => Get(nameof(AlertExpiredTitle));
    public static string AlertExpiredMsg   => Get(nameof(AlertExpiredMsg));
    public static string AlertExpiredBtn   => Get(nameof(AlertExpiredBtn));
}
