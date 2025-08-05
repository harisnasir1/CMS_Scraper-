using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class BackgroundRemover
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.developer.pixelcut.ai/v1/remove-background";

    public BackgroundRemover(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> RemoveBackgroundAsync(string imageUrl)
    {
        var payload = new
        {
            image_url = imageUrl,
            format = "png"
        };

        string jsonPayload = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-API-KEY", _apiKey);

        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}
