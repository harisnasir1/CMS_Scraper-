namespace CMS_Scrappers.Services.Interfaces;

public interface IRRSyncAuthService
{
    Task<string> GetTokenAsync();
}