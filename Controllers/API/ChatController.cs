using GoldenWhistle.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoldenWhistle.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Message))
                    return BadRequest(new { reply = "Please write a message." });

                var reply = await _chatService.GetChatResponseAsync(request.Message);
                return Ok(new { reply });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat error");
                return StatusCode(500, new { reply = "Sorry, an error occurred." });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}