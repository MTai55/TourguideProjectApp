using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Complaints;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/complaints")]
public class ComplaintsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/complaints — Admin: tất cả | Owner: của mình
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] string status = "Pending")
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var q = db.Complaints.Include(c => c.User).Include(c => c.Place).AsQueryable();

        if (role != "Admin")
            q = q.Where(c => c.UserId == UserId); // Owner chỉ thấy của mình
        if (status != "All")
            q = q.Where(c => c.Status == status);

        return Ok(await q.OrderByDescending(c => c.CreatedAt).ToListAsync());
    }

    // POST /api/complaints — Owner gửi khiếu nại
    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreateComplaintDto dto)
    {
        var complaint = new Complaint
        {
            UserId = UserId,
            PlaceId = dto.PlaceId,
            ReviewId = dto.ReviewId,
            Type = dto.Type,
            Title = dto.Title,
            Content = dto.Content
        };
        db.Complaints.Add(complaint);
        await db.SaveChangesAsync();
        return Ok(complaint);
    }

    // PUT /api/complaints/{id}/resolve — Admin xử lý
    [HttpPut("{id}/resolve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveComplaintDto dto)
    {
        var c = await db.Complaints.FindAsync(id);
        if (c == null) return NotFound();
        c.Status = dto.Status;
        c.AdminReply = dto.Note;
        c.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(c);
    }
}