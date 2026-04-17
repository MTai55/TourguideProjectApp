using Microsoft.AspNetCore.Mvc;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/tours")]
public class ToursController : ControllerBase
{
    // GET /api/tours — Lấy danh sách tours cho mobile app
    [HttpGet]
    public IActionResult GetTours()
    {
        var tours = ToursDataStore.GetAllTours().Select(t => new
        {
            t.Id,
            t.Title,
            t.Description,
            t.DurationText,
            t.BudgetText,
            t.Tag,
            StopsText = $"• {t.StopPlaceIds.Count} điểm",
            StopPlaceIds = t.StopPlaceIds.ToArray()
        });

        return Ok(tours);
    }

    // POST /api/tours/sync — Sync tours data từ admin (tạm bợ)
    [HttpPost("sync")]
    public IActionResult SyncTours([FromBody] List<TourData> toursData)
    {
        // Clear existing data
        var existingTours = ToursDataStore.GetAllTours().ToList();
        foreach (var tour in existingTours)
        {
            ToursDataStore.DeleteTour(tour.Id);
        }

        // Add new data
        foreach (var tour in toursData)
        {
            ToursDataStore.AddTour(tour);
        }

        return Ok(new { message = "Tours synced successfully", count = toursData.Count });
    }
}