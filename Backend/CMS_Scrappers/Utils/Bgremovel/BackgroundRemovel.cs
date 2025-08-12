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
    public async Task<Stream> ResizeImageAsync(Stream instream, int boxWidth, int boxHeight, int margin)
    {
        instream.Position = 0;
        using var image = await Image.LoadAsync<Rgba32>(instream);

        // Reduce the max drawable area by the margin
        int targetWidth = boxWidth - (margin * 2);
        int targetHeight = boxHeight - (margin * 2);

        // Calculate proportional scale so image fits entirely inside the reduced area
        double scale = Math.Min((double)targetWidth / image.Width, (double)targetHeight / image.Height);
        int newWidth = (int)Math.Round(image.Width * scale);
        int newHeight = (int)Math.Round(image.Height * scale);

        // Resize the image (upscale or downscale)
        image.Mutate(x => x.Resize(newWidth, newHeight));

        // Create a transparent canvas for final output
        using var canvas = new Image<Rgba32>(boxWidth, boxHeight, Color.Transparent);

        // Center the resized image within the box, leaving margin space
        int xPos = (boxWidth - newWidth) / 2;
        int yPos = (boxHeight - newHeight) / 2;
        canvas.Mutate(x => x.DrawImage(image, new Point(xPos, yPos), 1f));

        // Output to PNG stream (keeps transparency)
        var outStream = new MemoryStream();
        await canvas.SaveAsync(outStream, new PngEncoder());
        outStream.Position = 0;
        return outStream;
    }

}
