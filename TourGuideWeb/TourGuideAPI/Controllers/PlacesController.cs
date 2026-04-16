using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Places;
using TourGuideAPI.Models;
using TourGuideAPI.Services;
using System.Security.Claims;
using System.Text.Json;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/places")]
[EnableRateLimiting("general")]
public class PlacesController(AppDbContext db, IGeoLocationService geo, ILogger<PlacesController> logger, IConfiguration config) : ControllerBase
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

    // ── DEBUG ENDPOINT (Development Only) ─────────────────────────────────────────────
#if DEBUG
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
            message = "🐛 DEBUG INFO (Development Only)",
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
#endif

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
                ActivePromotions = 0,
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
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.IsActive);

        if (place == null) return NotFound();

        return Ok(new {
            place.PlaceId, place.Name, place.Description, place.Address,
            place.Latitude, place.Longitude, place.Phone,
            OpenTime  = place.OpenTime?.ToString(),
            CloseTime = place.CloseTime?.ToString(),
            place.AverageRating, place.TotalReviews, place.TotalVisits,
            CategoryName = place.Category?.Name,
            CategoryId   = place.CategoryId,
            MainImageUrl = place.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl,
            place.Specialty, place.PricePerPerson,
            place.PriceMin, place.PriceMax,
            place.District, place.HasParking, place.HasAircon,
            place.Status, place.OpenStatus,
            TtsScript      = place.tts_script,
            TtsTranslations= place.tts_translations,
            Radius         = place.radius,
        });
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

        try
        {
            // Manually delete related records (due to Restrict constraints)
            var reviews = await db.Reviews.Where(r => r.PlaceId == id).ToListAsync();
            db.Reviews.RemoveRange(reviews);

            var visitHistory = await db.VisitHistory.Where(v => v.PlaceId == id).ToListAsync();
            db.VisitHistory.RemoveRange(visitHistory);

            var messages = await db.Messages.Where(m => m.PlaceId == id).ToListAsync();
            db.Messages.RemoveRange(messages);

            // Delete the place (PlaceImages and Promotions auto-cascade)
            db.Places.Remove(place);

            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi xóa quán", error = ex.Message });
        }
    }

    [HttpPut("{id}/tts")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> UpdateTtsScript(int id, [FromBody] UpdateTtsDto dto)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return Forbid();

        place.tts_script = string.IsNullOrWhiteSpace(dto.TtsScript) ? null : dto.TtsScript.Trim();
        place.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { ttsScript = place.tts_script });
    }

    [HttpPost("{id}/tts/translate")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> TranslateTtsScript(int id, [FromBody] TranslateScriptRequest request, [FromServices] IHttpClientFactory httpClientFactory)
    {
        var place = await db.Places
            .FirstOrDefaultAsync(p => p.PlaceId == id && p.OwnerId == OwnerId);
        if (place == null) return Forbid();

        // Lấy script từ request hoặc DB
        var scriptToTranslate = !string.IsNullOrWhiteSpace(request?.Script) 
            ? request.Script.Trim() 
            : place.tts_script;

        if (string.IsNullOrWhiteSpace(scriptToTranslate))
            return BadRequest(new { message = "Chưa có script để dịch. Vui lòng lưu script trước." });

        // Kiểm tra ANTHROPIC_API_KEY
        var apiKey = config["ANTHROPIC_API_KEY"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("❌ ANTHROPIC_API_KEY not set in appsettings or environment");
            return StatusCode(500, new { message = "❌ Server chưa được cấu hình. Thiếu ANTHROPIC_API_KEY. Liên hệ admin." });
        }

        // Danh sách ngôn ngữ cần dịch
        var languages = new Dictionary<string, string>
        {
            ["vi"] = "Vietnamese",
            ["en"] = "English",
            ["zh"] = "Chinese (Simplified)",
            ["ko"] = "Korean",
            ["ja"] = "Japanese",
            ["fr"] = "French",
        };

        var prompt = "You are a professional translator for a Vietnamese food discovery app.\n\n" +
        "Translate the following Vietnamese TTS narration script into these languages:\n" +
        string.Join(", ", languages.Values) + "\n\n" +
        "SCRIPT:\n" + scriptToTranslate + "\n\n" +
        "Rules:\n" +
        "- Keep the same friendly, inviting tone\n" +
        "- Preserve place names, food names, addresses as-is\n" +
        "- Keep translations natural and suitable for Text-to-Speech\n" +
        "- Maximum 200 words per translation\n\n" +
        "Return ONLY a valid JSON object with language codes as keys:\n" +
        "{\"vi\":\"...\",\"en\":\"...\",\"zh\":\"...\",\"ko\":\"...\",\"ja\":\"...\",\"fr\":\"...\"}";

        var requestBody = new
        {
            model      = "claude-sonnet-4-20250514",
            max_tokens = 2000,
            messages   = new[] { new { role = "user", content = prompt } }
        };

        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            logger.LogInformation($"🔵 Calling Anthropic API for place {id}...");
            var response = await client.PostAsJsonAsync("https://api.anthropic.com/v1/messages", requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogError($"❌ Anthropic API Error [{(int)response.StatusCode}]: {errorContent}");
                
                // Phân tích lỗi từ Response
                var errorDetail = "";
                if ((int)response.StatusCode == 401)
                    errorDetail = "API key không hợp lệ hoặc hết hiệu lực. Vui lòng kiểm tra ANTHROPIC_API_KEY.";
                else if ((int)response.StatusCode == 429)
                    errorDetail = "Quá nhiều yêu cầu. Vui lòng chờ và thử lại sau.";
                else if ((int)response.StatusCode == 500)
                    errorDetail = "Dịch vụ Anthropic API gặp sự cố. Vui lòng thử lại sau.";
                else
                    errorDetail = errorContent?.Length <= 200 ? errorContent : "Lỗi từ dịch vụ dịch thuật.";
                
                return StatusCode(500, new { message = $"❌ Lỗi từ dịch vụ dịch thuật [{(int)response.StatusCode}]: {errorDetail}" });
            }

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(json);
            var content = parsed.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            logger.LogInformation($"✅ Translation successful for place {id}");

            // Lưu translations vào DB
            place.tts_translations = content;
            place.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Parse và trả về
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
            return Ok(new
            {
                success = true,
                translations,
                languages = languages.Keys.ToList()
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError($"❌ Network error calling translation API: {ex.Message}");
            return StatusCode(500, new { message = "❌ Không thể kết nối đến dịch vụ dịch thuật. Kiểm tra kết nối Internet." });
        }
        catch (JsonException ex)
        {
            logger.LogError($"❌ Invalid JSON from translation service: {ex.Message}");
            return StatusCode(500, new { message = "❌ Phản hồi từ dịch vụ không hợp lệ. Vui lòng thử lại sau." });
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Unexpected error in TranslateTtsScript: {ex.Message}");
            return StatusCode(500, new { message = $"❌ Lỗi không xác định: {ex.Message}" });
        }
    }

    // GET /api/places/{id}/tts — Lấy TTS theo ngôn ngữ (dùng cho app mobile)
    [HttpGet("{id}/tts")]
    public async Task<IActionResult> GetTts(int id, [FromQuery] string lang = "vi")
    {
        var place = await db.Places
            .Where(p => p.PlaceId == id && p.IsActive)
            .Select(p => new {
                p.PlaceId, p.Name,
                p.tts_script, p.tts_translations,
                p.radius
            })
            .FirstOrDefaultAsync();

        if (place == null) return NotFound();

        string? script = place.tts_script; // mặc định tiếng Việt

        // Nếu có translations và lang != vi, lấy bản dịch
        if (lang != "vi" && !string.IsNullOrEmpty(place.tts_translations))
        {
            try
            {
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(place.tts_translations);
                if (translations != null && translations.TryGetValue(lang, out var translated))
                    script = translated;
            }
            catch { /* fallback to original */ }
        }

        return Ok(new {
            placeId         = place.PlaceId,
            name            = place.Name,
            lang,
            ttsScript       = script,
            hasScript       = !string.IsNullOrEmpty(script),
            hasTranslations = !string.IsNullOrEmpty(place.tts_translations),
            radius          = place.radius ?? 100.0,
        });
    }
    [HttpGet("{id}/images")]
    public async Task<IActionResult> GetImages(int id)
    {
        var images = await db.PlaceImages
            .Where(i => i.PlaceId == id)
            .OrderByDescending(i => i.IsMain)
            .ThenBy(i => i.SortOrder)
            .Select(i => new { i.ImageId, i.ImageUrl, i.IsMain, i.SortOrder })
            .ToListAsync();
        return Ok(images);
    }
}