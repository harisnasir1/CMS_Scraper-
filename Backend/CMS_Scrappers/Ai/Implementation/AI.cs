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

        public async Task<string> GenerateDescription(Guid id)
        {
            var data = await _productRepository.Getproductbyid(id);
            if (data == null) throw new Exception("Invalid product ID.");

            string prompt = $"Generate a compelling product description based on the following details:\n" +
                            $"Title: {data.Title}\n" +
                            $"Brand: {data.Brand}\n" +
                            $"Category: {data.Category}\n" +
                            $"Condition: {data.Condition}\n" +
                            $"Gender: {data.Gender}\n" +
                            $"Price: {data.Price}";

            var body = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a professional e-commerce product description generator.Which write beautiful small Description of the product witout any specail character" },
                    new { role = "user", content = prompt }
                },
                model = "llama-3.3-70b-versatile", 
                temperature = 0.7,
                max_tokens = 300,
                top_p = 1,
                stream = false,
                stop = (string?)null
            };

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
            var description = doc.RootElement
                                 .GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();



            return description ?? "";
        }
    }
}
