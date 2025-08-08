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
    private const string ApiUrl = "https://api.remove.bg/v1.0/removebg";

    public BackgroundRemover(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<Stream> RemoveBackgroundAsync(string imageUrl)
    {

        using var formdata = new MultipartFormDataContent();
        formdata.Add(new StringContent(imageUrl), "image_url");
        formdata.Add(new StringContent("auto"), "size");


        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("X-API-KEY", _apiKey);
        request.Content = formdata;

      

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var imgaebytes=await response.Content.ReadAsByteArrayAsync();

        return new MemoryStream(imgaebytes);
    }
}
