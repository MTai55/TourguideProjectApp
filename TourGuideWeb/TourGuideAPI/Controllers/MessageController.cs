using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Messages;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/messages/{placeId} — Lấy hỏi đáp công khai
    [HttpGet("{placeId}")]
    public async Task<IActionResult> GetByPlace(int placeId)
    {
        var msgs = await db.Messages
            .Include(m => m.User)
            .Where(m => m.PlaceId == placeId && m.IsPublic && m.ParentId == null)
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .ToListAsync();
        return Ok(msgs);
    }

    // POST /api/messages — Khách gửi câu hỏi
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateMessageDto dto)
    {
        var msg = new Message
        {
            PlaceId = dto.PlaceId,
            UserId = UserId,
            Content = dto.Content,
            IsFromOwner = false,
            IsPublic = true
        };
        db.Messages.Add(msg);
        await db.SaveChangesAsync();
        return Ok(msg);
    }

    // POST /api/messages/{id}/reply — Chủ quán phản hồi
    [HttpPost("{id}/reply")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Reply(int id, [FromBody] string content)
    {
        var parent = await db.Messages
            .Include(m => m.Place)
            .FirstOrDefaultAsync(m => m.MessageId == id);
        if (parent == null) return NotFound();
        if (parent.Place!.OwnerId != UserId) return Forbid();

        var reply = new Message
        {
            PlaceId = parent.PlaceId,
            UserId = UserId,
            Content = content,
            IsFromOwner = true,
            ParentId = id,
            IsPublic = true
        };
        db.Messages.Add(reply);
        await db.SaveChangesAsync();
        return Ok(reply);
    }
}