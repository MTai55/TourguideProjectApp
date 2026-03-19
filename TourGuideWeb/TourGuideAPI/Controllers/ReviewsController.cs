using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Reviews;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/reviews/{placeId}
    [HttpGet("{placeId}")]
    public async Task<IActionResult> GetByPlace(int placeId, [FromQuery] int page = 1)
    {
        var reviews = await db.Reviews
            .Include(r => r.User)
            .Where(r => r.PlaceId == placeId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(r => new ReviewDto(
                r.ReviewId, r.User!.FullName, r.User.AvatarUrl,
                r.Rating, r.Comment, r.OwnerReply, r.CreatedAt,
                r.TasteRating, r.PriceRating, r.SpaceRating))
            .ToListAsync();
        return Ok(reviews);
    }

    // POST /api/reviews  [User]
    [HttpPost]
    [Authorize(Roles = "User,Owner")]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        if (dto.Rating is < 1 or > 5)
            return BadRequest(new { message = "Rating phải từ 1–5." });
        if (await db.Reviews.AnyAsync(r => r.UserId == UserId && r.PlaceId == dto.PlaceId))
            return Conflict(new { message = "Bạn đã đánh giá quán này rồi." });

        var review = new Review
        {
            UserId = UserId,
            PlaceId = dto.PlaceId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            TasteRating = dto.TasteRating,
            PriceRating = dto.PriceRating,
            SpaceRating = dto.SpaceRating
        };
        db.Reviews.Add(review);
        await db.SaveChangesAsync();

        // Cập nhật AverageRating
        var avg = await db.Reviews.Where(r => r.PlaceId == dto.PlaceId).AverageAsync(r => (double)r.Rating);
        var cnt = await db.Reviews.CountAsync(r => r.PlaceId == dto.PlaceId);
        await db.Places.Where(p => p.PlaceId == dto.PlaceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.AverageRating, avg)
                .SetProperty(p => p.TotalReviews, cnt));
        return Ok(review);
    }

    // PUT /api/reviews/{id}/reply  [Owner]
    [HttpPut("{id}/reply")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Reply(int id, [FromBody] string reply)
    {
        var review = await db.Reviews
            .Include(r => r.Place)
            .FirstOrDefaultAsync(r => r.ReviewId == id && r.Place!.OwnerId == UserId);
        if (review == null) return NotFound();
        review.OwnerReply = reply;
        await db.SaveChangesAsync();
        return Ok(new { replied = true });
    }
}