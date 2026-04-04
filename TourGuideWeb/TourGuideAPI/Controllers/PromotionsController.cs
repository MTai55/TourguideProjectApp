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
public class PromotionsController(AppDbContext db, ILogger<PromotionsController> logger) : ControllerBase
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
        var promos = await db.Promotions
            .Where(p => myPlaceIds.Contains(p.PlaceId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        
        // Map to PromotionViewModel to exclude navigation properties
        var result = promos.Select(p => new
        {
            p.PromoId,
            p.PlaceId,
            p.Title,
            p.Description,
            p.Discount,
            p.VoucherCode,
            p.StartDate,
            p.EndDate,
            p.IsActive,
            IsExpired = p.IsExpired
        }).ToList();
        
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePromoDto dto)
    {
        try
        {
            logger.LogInformation($"📤 Creating promotion with PlaceId={dto.PlaceId}, Title='{dto.Title}'");
            
            var place = await db.Places.FirstOrDefaultAsync(p => p.PlaceId == dto.PlaceId && p.OwnerId == UserId);
            if (place == null)
            {
                logger.LogWarning($"❌ Place not found: PlaceId={dto.PlaceId}, UserId={UserId}");
                return Forbid();
            }
            
            logger.LogInformation($"✅ Found place: {place.Name}");
            
            // Convert StartDate and EndDate to UTC (they come from datetime-local input as Unspecified)
            var startDateUtc = dto.StartDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc) 
                : dto.StartDate.ToUniversalTime();
            
            var endDateUtc = dto.EndDate.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc) 
                : dto.EndDate.ToUniversalTime();
            
            logger.LogInformation($"📅 StartDate: {startDateUtc:O} (Kind={startDateUtc.Kind})");
            logger.LogInformation($"📅 EndDate: {endDateUtc:O} (Kind={endDateUtc.Kind})");
            
            var promo = new Promotion
            {
                PlaceId = dto.PlaceId,
                Title = dto.Title,
                Description = dto.Description,
                Discount = dto.Discount,
                VoucherCode = dto.VoucherCode,
                StartDate = startDateUtc,
                EndDate = endDateUtc,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            logger.LogInformation($"📝 Adding promotion to context");
            db.Promotions.Add(promo);
            
            logger.LogInformation($"💾 Saving to database...");
            await db.SaveChangesAsync();
            
            logger.LogInformation($"✅ Promotion created: PromoId={promo.PromoId}");
            
            // Return PromotionViewModel instead of raw Promotion
            return Ok(new
            {
                PromoId = promo.PromoId,
                PlaceId = promo.PlaceId,
                Title = promo.Title,
                Description = promo.Description,
                Discount = promo.Discount,
                VoucherCode = promo.VoucherCode,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                IsActive = promo.IsActive,
                IsExpired = promo.IsExpired
            });
        }
        catch (DbUpdateException dbEx)
        {
            logger.LogError($"❌ Database error: {dbEx.Message}");
            logger.LogError($"❌ Inner exception: {dbEx.InnerException?.Message}");
            logger.LogError($"❌ Stack: {dbEx.InnerException?.StackTrace}");
            return StatusCode(500, new { title = "Lỗi hệ thống", status = 500, detail = dbEx.InnerException?.Message ?? dbEx.Message });
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ General error: {ex.Message}");
            logger.LogError($"❌ Stack: {ex.StackTrace}");
            return StatusCode(500, new { title = "Lỗi hệ thống", status = 500, detail = ex.Message });
        }
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