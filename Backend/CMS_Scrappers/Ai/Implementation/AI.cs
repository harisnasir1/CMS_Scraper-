using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Ai.Implementation
{
    public class AI : IAi
    {
        private readonly IProductRepository _productRepository;
        private readonly AISettings _Settings;
        private readonly HttpClient _httpClient;

        public AI(IProductRepository productRepository, AISettings Settings)
        {
            _productRepository = productRepository;
            _Settings=Settings;
            _httpClient = new HttpClient();
        }

        /// <summary>Shortest description we accept. Anything below this is treated as a truncated answer.</summary>
        public const int MinDescriptionLength = 40;

        public async Task<string> GenerateDescription(Guid id)
        {
            var data = await _productRepository.Getproductbyid(id);
            if (data == null) throw new Exception("Invalid product ID.");

            string prompt = $"Generate a compelling product description based on the following details:\n" +
                            $"Title: {data.Title}\n" +
                            $"Brand: {data.Brand}\n" +
                            $"Category: {data.Category}\n" +
                            $"Condition: {data.Condition}\n" +
                            $"Gender: {data.Gender}\n" ;

            // gpt-oss is a reasoning model: its hidden reasoning counts against max_tokens, so a
            // small budget returns an empty or cut-off answer. Keep reasoning low and the budget
            // generous, and retry once if the visible answer still comes back too short.
            var body = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a professional e-commerce product description generator. Write a short, complete product description of two to four sentences in plain text, without any special characters, markdown, or headings." },
                    new { role = "user", content = prompt }
                },
                model = "openai/gpt-oss-20b",
                reasoning_effort = "low",
                temperature = 0.7,
                max_tokens = 1024,
                top_p = 1,
                stream = false,
                stop = (string?)null
            };

            string description = "";
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                description = await CallGroq(body);
                if (description.Length >= MinDescriptionLength) break;
            }

            return description;
        }

        private async Task<string> CallGroq(object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _Settings.Apikey);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Groq API call failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
            var description = message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String
                ? content.GetString()
                : null;

            return (description ?? "").Trim();
        }
    }
}
