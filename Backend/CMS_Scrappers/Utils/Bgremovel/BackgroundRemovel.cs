using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CMS_Scrappers.Utils.Bgremovel
{
    public class BackgroundRemover
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiToken;
        private readonly string _apiUrl = "https://api.removal.ai/3.0/remove";

        public BackgroundRemover(HttpClient httpClient, string apiToken)
        {
            _httpClient = httpClient;
            _apiToken = apiToken;
        }

        public async Task<string> RemoveBackgroundAsync(string imagePath = null, string imageUrl = null)
        {
            using (var form = new MultipartFormDataContent())
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Rm-Token", _apiToken);

                if (!string.IsNullOrEmpty(imagePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    form.Add(fileContent, "image_file", System.IO.Path.GetFileName(imagePath));
                }

                if (!string.IsNullOrEmpty(imageUrl))
                {
                    form.Add(new StringContent(imageUrl), "image_url");
                }

                var response = await _httpClient.PostAsync(_apiUrl, form);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
