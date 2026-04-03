using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Places;
using TourGuideAPI.Models;
using TourGuideAPI.Services;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/places")]
[EnableRateLimiting("general")]
public class PlacesController(AppDbContext db, IGeoLocationService geo, ILogger<PlacesController> logger) : ControllerBase
{
    private int OwnerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/places ───────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] string? sortBy,
        [FromQuery] string? district,
        [FromQuery] int? maxPrice,
        [FromQuery] string? specialty,
        [FromQuery] bool? hasAircon,
        [FromQuery] bool? hasParking,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Places
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Where(p => p.Status == "Active" && p.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Address.ToLower().Contains(s) ||
                (p.Specialty != null && p.Specialty.ToLower().Contains(s)));
        }

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrEmpty(district)) query = query.Where(p => p.District == district);
        if (maxPrice.HasValue) query = query.Where(p => p.PricePerPerson <= maxPrice);
        if (hasAircon.HasValue) query = query.Where(p => p.HasAircon == hasAircon);
        if (hasParking.HasValue) query = query.Where(p => p.HasParking == hasParking);
        if (!string.IsNullOrEmpty(specialty))
        {
            var s = specialty.ToLower();
            query = query.Where(p => p.Specialty != null && p.Specialty.ToLower().Contains(s));
        }

        query = sortBy switch
        {
            "name" => query.OrderBy(p => p.Name),
            "rating" => query.OrderByDescending(p => p.AverageRating),
            "visits" => query.OrderByDescending(p => p.TotalVisits),
            "price_asc" => query.OrderBy(p => p.PricePerPerson),
            "price_desc" => query.OrderByDescending(p => p.PricePerPerson),
            _ => query.OrderByDescending(p => p.AverageRating)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PlaceDto(
                p.PlaceId, p.Name, p.Description, p.Address,
                p.Latitude, p.Longitude, p.Phone,
                p.OpenTime != null ? p.OpenTime.ToString() : null,
                p.CloseTime != null ? p.CloseTime.ToString() : null,
                p.AverageRating, p.TotalReviews, p.TotalVisits,
                p.Category != null ? p.Category.Name : null,
                p.Images.FirstOrDefault() != null ? p.Images.First().ImageUrl : null,
                null,
                p.Specialty, p.PricePerPerson, p.PriceMin, p.PriceMax, p.District,
                p.HasParking, p.HasAircon))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // ── GET /api/places/nearby ────────────────────────────────────
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] NearbyQueryDto query)
        => Ok(await geo.GetNearbyAsync(query));

    // ── DEBUG ENDPOINT ─────────────────────────────────────────────
    [HttpGet("debug/info")]
    public async Task<IActionResult> DebugInfo()
    {
        var totalPlaces = await db.Places.CountAsync();
        var owners = await db.Places.Select(p => p.OwnerId).Distinct().ToListAsync();
        var allUsers = await db.Users.Select(u => new { u.UserId, u.Email, u.FullName, u.Role, u.IsActive }).ToListAsync();
        
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var isAuthorized = User.Identity?.IsAuthenticated ?? false;
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role);
        
        return Ok(new
        {
            message = "🐛 DEBUG INFO",
            database = new
            {
                totalPlaces,
                totalOwners = owners.Count,
                ownerIds = owners,
                allUsers
            },
            auth = new
            {
                isAuthenticated = isAuthorized,
                userId,
                role,
                claims
            }
        });
    }

    // ── GET /api/places/mine ──────────────────────────────────────
    [HttpGet("mine")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetMyPlaces(
        [FromQuery] string? search,
        [FromQuery] int page = 1)
    {
        logger.LogInformation($"📍 GetMyPlaces called - OwnerId: {OwnerId}, Page: {page}, Search: {search ?? "null"}");
        
        var query = db.Places
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Promotions)
            .Where(p => p.OwnerId == OwnerId && p.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(p => new
            {
                p.PlaceId,
                p.Name,
                p.Description,
                p.Address,
                p.Latitude,
                p.Longitude,
                p.Phone,
                OpenTime = p.OpenTime != null ? p.OpenTime.ToString() : null,
                CloseTime = p.CloseTime != null ? p.CloseTime.ToString() : null,
                p.AverageRating,
                p.TotalReviews,
                p.TotalVisits,
                CategoryName = p.Category != null ? p.Category.Name : null,
                MainImageUrl = p.Images.FirstOrDefault(i => i.IsMain) != null ? p.Images.First(i => i.IsMain).ImageUrl : null,
                p.Specialty,
                p.PricePerPerson,
                p.PriceMin,
                p.PriceMax,
                p.District,
                p.HasParking,
                p.HasAircon,
                p.Status,
                p.OpenStatus,
                p.IsApproved,
                ActivePromotions = p.Promotions.Count(pr => pr.IsActive && pr.EndDate > DateTime.UtcNow),
            })
            .ToListAsync();

        logger.LogInformation($"✅ GetMyPlaces result - Total: {total}, Items: {items.Count}");
        return Ok(new { total, page, items });
    }

    // ── GET /api/places/{id} ──────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var place = await db.Places
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsMain))
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.IsActive);

        if (place == null) return NotFound();

        var dto = new PlaceDto(
            place.PlaceId, place.Name, place.Description, place.Address,
            place.Latitude, place.Longitude, place.Phone,
            place.OpenTime?.ToString(), place.CloseTime?.ToString(),
            place.AverageRating, place.TotalReviews, place.TotalVisits,
            place.Category?.Name,
            place.Images.FirstOrDefault()?.ImageUrl,
            null,
            place.Specialty, place.PricePerPerson, place.PriceMin, place.PriceMax,
            place.District, place.HasParking, place.HasAircon);

        return Ok(dto);
    }

    // ── POST /api/places ──────────────────────────────────────────
    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePlaceDto dto)
    {
        try
        {
            var openTime = string.IsNullOrEmpty(dto.OpenTime) ? (TimeOnly?)null : TimeOnly.Parse(dto.OpenTime);
            var closeTime = string.IsNullOrEmpty(dto.CloseTime) ? (TimeOnly?)null : TimeOnly.Parse(dto.CloseTime);

            var place = new Place
            {
                Name = dto.Name,
                Description = dto.Description,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Phone = dto.Phone,
                CategoryId = dto.CategoryId.HasValue && dto.CategoryId > 0 ? dto.CategoryId : null,
                PriceMin = dto.PriceMin,
                PriceMax = dto.PriceMax,
                Specialty = dto.Specialty,
                PricePerPerson = dto.PricePerPerson,
                District = dto.District,
                OpenTime = openTime,
                CloseTime = closeTime,
                HasParking = dto.HasParking,
                HasAircon = dto.HasAircon,
                OwnerId = OwnerId,
                Status = "Active",
                OpenStatus = "Closed",
                IsActive = true,
            };

            db.Places.Add(place);
            await db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = place.PlaceId }, place);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                title = "Lỗi hệ thống",
                status = 500,
                detail = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    // ── PUT /api/places/{id} ──────────────────────────────────────
    [HttpPut("{id}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePlaceDto dto)
    {
        try
        {
            var place = await db.Places
                .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
            if (place == null) return Forbid();

            place.Name = dto.Name;
            place.Description = dto.Description;
            place.Address = dto.Address;
            place.Phone = dto.Phone;
            place.OpenTime = string.IsNullOrEmpty(dto.OpenTime) ? null : TimeOnly.Parse(dto.OpenTime);
            place.CloseTime = string.IsNullOrEmpty(dto.CloseTime) ? null : TimeOnly.Parse(dto.CloseTime);
            place.Specialty = dto.Specialty;
            place.PricePerPerson = dto.PricePerPerson;
            place.PriceMin = dto.PriceMin;
            place.PriceMax = dto.PriceMax;
            place.District = dto.District;
            place.HasParking = dto.HasParking;
            place.HasAircon = dto.HasAircon;
            place.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Ok(place);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                title = "Lỗi hệ thống",
                status = 500,
                detail = ex.Message,
                inner = ex.InnerException?.Message
            });
        }
    }

    // ── PUT /api/places/{id}/status ───────────────────────────────
    [HttpPut("{id}/status")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string openStatus)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return NotFound();

        if (openStatus is not ("Open" or "Closed" or "Busy"))
            return BadRequest(new { message = "Trạng thái không hợp lệ. Chỉ chấp nhận: Open, Closed, Busy" });

        place.OpenStatus = openStatus;
        await db.SaveChangesAsync();
        return Ok(new { openStatus });
    }

    // ── POST /api/places/{id}/images ──────────────────────────────
    [HttpPost("{id}/images")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> AddImage(int id, [FromBody] AddImageDto dto)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return Forbid();

        if (dto.IsMain)
            await db.PlaceImages
                .Where(i => i.PlaceId == id && i.IsMain)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsMain, false));

        var image = new PlaceImage
        {
            PlaceId = id,
            ImageUrl = dto.ImageUrl,
            IsMain = dto.IsMain,
        };
        db.PlaceImages.Add(image);
        await db.SaveChangesAsync();
        return Ok(image);
    }

    // ── DELETE /api/places/{id}/images/{imageId} ──────────────────
    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return Forbid();

        var image = await db.PlaceImages
            .FirstOrDefaultAsync(i => i.ImageId == imageId && i.PlaceId == id);
        if (image == null) return NotFound();

        db.PlaceImages.Remove(image);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── DELETE /api/places/{id} ───────────────────────────────────
    [HttpDelete("{id}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return Forbid();

        place.IsActive = false;
        place.Status = "Closed";
        place.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}