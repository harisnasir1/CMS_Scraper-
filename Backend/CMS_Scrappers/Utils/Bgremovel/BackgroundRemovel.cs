using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
public class BackgroundRemover
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.remove.bg/v1.0/removebg";
    private bool retry = false;

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
        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retry==false) 
        {
            await Task.Delay(2000);
            retry = true;
            return await RemoveBackgroundAsync(imageUrl); 
        }
        response.EnsureSuccessStatusCode();

        var imgaebytes=await response.Content.ReadAsByteArrayAsync();

        return new MemoryStream(imgaebytes);
    }
    public async Task<Stream> ResizeImageAsync(Stream instream, int boxWidth, int boxHeight, int margin, bool fillBox )
    {
        instream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(instream);

        if (!fillBox)
        {
           margin = Math.Min(margin, Math.Min(boxWidth / 2 - 1, boxHeight / 2 - 1));
            int targetWidth = boxWidth - (margin * 2);
            int targetHeight = boxHeight - (margin * 2);

            double scale = Math.Min((double)targetWidth / image.Width, (double)targetHeight / image.Height);
            int newWidth = (int)Math.Round(image.Width * scale);
            int newHeight = (int)Math.Round(image.Height * scale);

            if (newWidth <= 0 || newHeight <= 0)
                throw new InvalidOperationException("Margin too large compared to image size.");

            image.Mutate(x => x.Resize(newWidth, newHeight));

           
            using var canvas = new Image<Rgba32>(boxWidth, boxHeight, Color.Transparent);
            int offsetX = (boxWidth - newWidth) / 2;
            int offsetY = (boxHeight - newHeight) / 2;

            canvas.Mutate(x => x.DrawImage(image, new Point(offsetX, offsetY), 1f));

            var outStream = new MemoryStream();
            await canvas.SaveAsync(outStream, new PngEncoder());
            outStream.Position = 0;
            return outStream;
        }
        else
        {
          
            double scale = Math.Max((double)boxWidth / image.Width, (double)boxHeight / image.Height);
            int newWidth = (int)Math.Round(image.Width * scale);
            int newHeight = (int)Math.Round(image.Height * scale);

            image.Mutate(x => x.Resize(newWidth, newHeight));

           
            var cropRect = new Rectangle(
                (newWidth - boxWidth) / 2,
                (newHeight - boxHeight) / 2,
                boxWidth,
                boxHeight
            );

            image.Mutate(x => x.Crop(cropRect));

            var outStream = new MemoryStream();
            await image.SaveAsync(outStream, new PngEncoder());
            outStream.Position = 0;
            return outStream;
        }
    }


}
