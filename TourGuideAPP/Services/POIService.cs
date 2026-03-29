using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

// Wrapper quanh PlaceService — bảng pois đã gộp vào Places
public class POIService
{
    private readonly PlaceService _placeService;

    public POIService(PlaceService placeService)
    {
        _placeService = placeService;
    }

    public async Task<List<Place>> GetAllPOIsAsync() => await _placeService.GetAllPlacesAsync();

    public List<Place> GetCachedPOIs() => _placeService.GetCachedPlaces();
}
