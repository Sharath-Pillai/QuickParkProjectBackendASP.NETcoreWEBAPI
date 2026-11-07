using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickParkAPI.DTOs;
using QuickParkAPI.Services;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/support/chat")]
    [Authorize]
    public sealed class QuickChatController : ControllerBase
    {
        private const int MaxMessageLength = 2000;
        private const int MaxHistoryMessages = 8;

        private readonly QuickChatAgent _agent;
        private readonly ILogger<QuickChatController> _logger;

        public QuickChatController(QuickChatAgent agent, ILogger<QuickChatController> logger)
        {
            _agent = agent;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Chat(
            [FromBody] QuickChatRequestDto request,
            CancellationToken cancellationToken)
        {
            var userId = ClaimsHelper.GetUserId(User);
            if (ClaimsHelper.GetRole(User) != "user" || userId <= 0)
                return Forbid();

            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > MaxMessageLength)
                return BadRequest(new { error = $"Message must be between 1 and {MaxMessageLength} characters." });

            if (request.History is null ||
                request.History.Count > MaxHistoryMessages ||
                request.History.Any(item =>
                    item is null ||
                    item.Role is not ("user" or "assistant") ||
                    string.IsNullOrWhiteSpace(item.Content) ||
                    item.Content.Length > MaxMessageLength))
                return BadRequest(new { error = "Chat history is invalid or too long." });

            try
            {
                var response = await _agent.RespondAsync(
                    userId,
                    request.History,
                    request.Message.Trim(),
                    cancellationToken);
                return Ok(response);
            }
            catch (QuickChatNotConfiguredException)
            {
                _logger.LogWarning("Quick chat is unavailable because the Gemini API key is not configured.");
                return StatusCode(503, new { error = "Quick support chat is not configured yet." });
            }
            catch (QuickChatProviderException exception)
            {
                _logger.LogError(exception, "Quick chat could not complete its Gemini request.");
                return StatusCode(502, new { error = "Quick is temporarily unavailable. Please try again." });
            }
            catch (QuickChatResponseException exception)
            {
                _logger.LogError(exception, "Quick returned an unusable structured response.");
                return StatusCode(502, new { error = "Quick could not prepare a response. Please try again." });
            }
        }
    }
}
