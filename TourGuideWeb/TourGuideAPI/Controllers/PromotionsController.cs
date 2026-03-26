using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Promotions;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{placeId}")]
    public async Task<IActionResult> GetByPlace(int placeId)
        => Ok(await db.Promotions
            .Where(p => p.PlaceId == placeId && p.IsActive && p.EndDate > DateTime.UtcNow)
            .OrderBy(p => p.EndDate).ToListAsync());

    [HttpGet("mine")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetMine()
    {
        var myPlaceIds = await db.Places
            .Where(p => p.OwnerId == UserId).Select(p => p.PlaceId).ToListAsync();
        return Ok(await db.Promotions
            .Include(p => p.Place)
            .Where(p => myPlaceIds.Contains(p.PlaceId))
            .OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePromoDto dto)
    {
        var place = await db.Places.FirstOrDefaultAsync(p => p.PlaceId == dto.PlaceId && p.OwnerId == UserId);
        if (place == null) return Forbid();
        var promo = new Promotion
        {
            PlaceId = dto.PlaceId,
            Title = dto.Title,
            Description = dto.Description,
            Discount = dto.Discount,
            VoucherCode = dto.VoucherCode,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate
        };
        db.Promotions.Add(promo);
        await db.SaveChangesAsync();
        return Ok(promo);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var promo = await db.Promotions.Include(p => p.Place).FirstOrDefaultAsync(p => p.PromoId == id);
        if (promo?.Place?.OwnerId != UserId) return Forbid();
        promo.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}