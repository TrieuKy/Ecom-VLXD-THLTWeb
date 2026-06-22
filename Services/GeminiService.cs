using System.Text;
using System.Text.Json;

namespace TrieuDoanKy_W2.Services
{
    public interface IGeminiService
    {
        Task<string> AskAsync(string question);
    }

    public class GeminiService : IGeminiService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GeminiService> logger)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<string> AskAsync(string question)
        {
            try
            {
                var apiKey = _config["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return "Chatbot chưa được cấu hình API key.";

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

                var systemContext = """
                    Bạn là trợ lý tư vấn của VLXD Shop - cửa hàng vật liệu xây dựng.
                    Chỉ trả lời các câu hỏi liên quan đến vật liệu xây dựng, sản phẩm của shop, 
                    tư vấn xây dựng, giá cả, chính sách giao hàng và hỗ trợ khách hàng.
                    Trả lời ngắn gọn, thân thiện bằng tiếng Việt.
                    """;

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = systemContext + "\n\nKhách hỏi: " + question } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 512
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                    return "Xin lỗi, tôi đang gặp sự cố. Vui lòng thử lại sau.";

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "Xin lỗi, tôi không thể trả lời câu hỏi này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gọi Gemini API");
                return "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại!";
            }
        }
    }
}
