using System.Text.Json;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations
{
    public class GoogleImageService:IGoogleImageService
    {
        private readonly HttpClient _httpClient;

        private readonly GoogleAPISettings _thirdParties;
        public GoogleImageService(HttpClient httpClient,GoogleAPISettings thiredparty)
        {
            _httpClient = httpClient;
 
            _thirdParties = thiredparty;
        }

        public async Task<ApiResponse<Object>> SearchImagesAsync(string query,int start=1)
        {
            
           try  {
                   string requestUrl = $"{_thirdParties.GoogleAPIURL}?q={Uri.EscapeDataString(query)}&cx={_thirdParties.GoogleCseId}&searchType=image&key={_thirdParties.GoogleAPIKey}&start={start}";
                  HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
      
                
                  if (!response.IsSuccessStatusCode)
                  {
                      
                  }
                  string jsonResponse = await response.Content.ReadAsStringAsync();
                  using JsonDocument doc = JsonDocument.Parse(jsonResponse);
             
                  List<string> imageUrls = new List<string>();
                  if (doc.RootElement.TryGetProperty("items", out JsonElement items))
                  {
                      foreach (JsonElement item in items.EnumerateArray())
                      {
                          if (item.TryGetProperty("link", out JsonElement link))
                          {
                              imageUrls.Add(link.GetString());
                          }
                      }
                  }
                  else
                  {
                      return ApiResponse<object>.Failure("No images found for the given product title");
                  }
                  return ApiResponse<object>.Success(imageUrls, "Product updated successfully");
             }
            catch (Exception ex)
            {
                return ApiResponse<object>.Failure(ex.Message);
            }
        }
    }
}
