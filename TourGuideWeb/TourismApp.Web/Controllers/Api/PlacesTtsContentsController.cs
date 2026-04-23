using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;

namespace TourismApp.Web.Controllers.Api;

[ApiController]
[Route("api/places/{placeId}/tts-contents")]
[OwnerOnly]
public class PlacesTtsContentsController(ApiService api, ILogger<PlacesTtsContentsController> logger) : ControllerBase
{
    // GET /api/places/{placeId}/tts-contents
    [HttpGet]
    public async Task<IActionResult> GetTtsContents(int placeId)
    {
        try
        {
            var result = await api.GetTtsContentsAsync(placeId);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error getting TTS contents for place {placeId}: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/places/{placeId}/tts-contents/{locale}
    [HttpGet("{locale}")]
    public async Task<IActionResult> GetTtsContentByLocale(int placeId, string locale)
    {
        try
        {
            var result = await api.GetTtsContentByLocaleAsync(placeId, locale);
            return result == null ? NotFound() : Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error getting TTS content for place {placeId}, locale {locale}: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    // POST /api/places/{placeId}/tts-contents
    [HttpPost]
    public async Task<IActionResult> CreateTtsContent(int placeId, [FromBody] CreateTtsContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Locale) || string.IsNullOrWhiteSpace(request.Script))
                return BadRequest(new { error = "Locale and Script are required" });

            var (success, content, error) = await api.CreateTtsContentAsync(placeId, request.Locale, request.Script);
            if (!success)
                return BadRequest(new { error });

            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error creating TTS content for place {placeId}: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT /api/places/{placeId}/tts-contents/{contentId}
    [HttpPut("{contentId}")]
    public async Task<IActionResult> UpdateTtsContent(int placeId, int contentId, [FromBody] UpdateTtsContentRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Script))
                return BadRequest(new { error = "Script is required" });

            var (success, content, error) = await api.UpdateTtsContentAsync(placeId, contentId, request.Script);
            if (!success)
                return BadRequest(new { error });

            return Ok(content);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error updating TTS content {contentId} for place {placeId}: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE /api/places/{placeId}/tts-contents/{contentId}
    [HttpDelete("{contentId}")]
    public async Task<IActionResult> DeleteTtsContent(int placeId, int contentId)
    {
        try
        {
            var success = await api.DeleteTtsContentAsync(placeId, contentId);
            if (!success)
                return BadRequest(new { error = "Failed to delete TTS content" });

            return Ok(new { message = "Deleted successfully" });
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error deleting TTS content {contentId} for place {placeId}: {ex.Message}");
            return BadRequest(new { error = ex.Message });
        }
    }

    public class CreateTtsContentRequest
    {
        public string Locale { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
    }

    public class UpdateTtsContentRequest
    {
        public string Script { get; set; } = string.Empty;
    }
}
