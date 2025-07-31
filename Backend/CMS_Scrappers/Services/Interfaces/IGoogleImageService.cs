using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace CMS_Scrappers.Services.Interfaces
{
    public interface IGoogleImageService
    {
        Task <ApiResponse<Object>> SearchImagesAsync(string query,int start);
    }
}
