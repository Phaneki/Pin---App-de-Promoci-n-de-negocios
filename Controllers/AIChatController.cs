using Microsoft.AspNetCore.Mvc;
using PinAppdePromo.Services;
using System.Threading.Tasks;

namespace PinAppdePromo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIChatController : ControllerBase
    {
        private readonly ISemanticKernelService _aiService;

        public AIChatController(ISemanticKernelService aiService)
        {
            _aiService = aiService;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { response = "Por favor, escribe un mensaje válido." });
            }

            var response = await _aiService.GetRecommendationAsync(request.Message);

            return Ok(new { response });
        }
    }
}
