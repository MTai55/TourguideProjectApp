using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourGuideAPI.DTOs.Tracking;
using TourGuideAPI.Services;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController(ITrackingService tracking) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST /api/tracking/location  — MAUI gửi mỗi 30 giây
    [HttpPost("location")]
    public async Task<IActionResult> PostLocation([FromBody] LocationDto dto)
    {
        await tracking.LogLocationAsync(UserId, dto);
        return Ok(new { logged = true });
    }

    // POST /api/tracking/checkin
    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
    {
        var visit = await tracking.CheckInAsync(UserId, dto);
        return Ok(new { visit.VisitId, visit.PlaceId, visit.CheckInTime });
    }

    // PUT /api/tracking/checkout/{visitId}
    [HttpPut("checkout/{visitId}")]
    public async Task<IActionResult> CheckOut(int visitId)
    {
        await tracking.CheckOutAsync(UserId, visitId);
        return Ok(new { checkedOut = true });
    }

    // GET /api/tracking/history?page=1
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1)
        => Ok(await tracking.GetVisitHistoryAsync(UserId, page));

    // GET /api/tracking/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await tracking.GetTripStatsAsync(UserId));
}