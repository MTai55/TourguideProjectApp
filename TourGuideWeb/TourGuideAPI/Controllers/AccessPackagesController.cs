using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/access-packages")]
public class AccessPackagesController(AppDbContext db) : ControllerBase
{
    // GET /api/access-packages — App + web đều gọi (không cần auth)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var packages = await db.AccessPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
        return Ok(packages);
    }

    // PUT /api/access-packages/{id} — Admin cập nhật giá/trạng thái
    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdatePackageDto dto)
    {
        var pkg = await db.AccessPackages.FindAsync(id);
        if (pkg == null) return NotFound();

        pkg.PriceVnd      = dto.PriceVnd;
        pkg.DurationHours = dto.DurationHours;
        pkg.IsActive      = dto.IsActive;
        pkg.SortOrder     = dto.SortOrder;

        await db.SaveChangesAsync();
        return Ok(pkg);
    }
}

public record UpdatePackageDto(double DurationHours, int PriceVnd, bool IsActive, int SortOrder);
