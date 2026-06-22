using Microsoft.AspNetCore.Mvc;
using TrieuDoanKy_W2.Services;

namespace TrieuDoanKy_W2.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IGeminiService _geminiService;

        public ChatbotController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return Json(new { reply = "Vui lòng nhập câu hỏi!" });

            if (request.Message.Length > 500)
                return Json(new { reply = "Câu hỏi quá dài. Vui lòng giới hạn 500 ký tự." });

            var reply = await _geminiService.AskAsync(request.Message);
            return Json(new { reply });
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
    }
}
