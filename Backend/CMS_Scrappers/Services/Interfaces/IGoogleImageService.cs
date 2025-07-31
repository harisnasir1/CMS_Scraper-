namespace CMS_Scrappers.Services.Interfaces
{
    public interface IGoogleImageService
    {
        Task<List<string>> SearchImagesAsync(string query,int start);
    }
}
