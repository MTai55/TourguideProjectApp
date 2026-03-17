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
public class PlacesController(AppDbContext db, IGeoLocationService geo) : ControllerBase
{
    // GET /api/places?search=&categoryId=&page=
    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] string? search,
    [FromQuery] int? categoryId,
    [FromQuery] bool? isApproved,
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
            .Include(p => p.Images.Where(i => i.IsMain))
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Address.ToLower().Contains(s) ||
                (p.Specialty != null && p.Specialty.ToLower().Contains(s)));
        }

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (isApproved.HasValue) query = query.Where(p => p.IsApproved == isApproved);
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
                p.OpenTime.ToString(), p.CloseTime.ToString(),
                p.AverageRating, p.TotalReviews, p.TotalVisits,
                p.Category!.Name,
                p.Images.FirstOrDefault()!.ImageUrl,
                null,
                p.Specialty, p.PricePerPerson, p.District,
                p.HasParking, p.HasAircon))
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/places/nearby?lat=10.77&lng=106.69&radiusKm=5
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby([FromQuery] NearbyQueryDto query)
        => Ok(await geo.GetNearbyAsync(query));

    // GET /api/places/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var place = await db.Places
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Reviews.OrderByDescending(r => r.CreatedAt).Take(10))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.IsApproved);
        return place == null ? NotFound() : Ok(place);
    }

    // POST /api/places  [Owner only]
    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePlaceDto dto)
    {
        var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var place = new Place
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Phone = dto.Phone,
            CategoryId = dto.CategoryId,
            PriceMin = dto.PriceMin,
            PriceMax = dto.PriceMax,
            OwnerId = ownerId
        };
        db.Places.Add(place);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = place.PlaceId }, place);
    }

    // PUT /api/places/{id}  [Owner only]
    [HttpPut("{id}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePlaceDto dto)
    {
        var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var place = await db.Places.FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == ownerId);
        if (place == null) return NotFound();
        place.Name = dto.Name; place.Description = dto.Description; place.Address = dto.Address;
        place.Latitude = dto.Latitude; place.Longitude = dto.Longitude;
        place.Phone = dto.Phone; place.CategoryId = dto.CategoryId;
        place.PriceMin = dto.PriceMin; place.PriceMax = dto.PriceMax;
        place.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(place);
    }

    // DELETE /api/places/{id}  [Owner only]
    [HttpDelete("{id}")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Delete(int id)
    {
        var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var place = await db.Places.FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == ownerId);
        if (place == null) return NotFound();
        place.IsActive = false; // soft delete
        await db.SaveChangesAsync();
        return NoContent();
    }
}