using CMS_Scrappers.Suppliers;

namespace CMS_Scrappers.Services.Interfaces;

/// <summary>Logs in to RRSync per account and caches the token until shortly before it expires. Singleton.</summary>
public interface IRRSyncAuthService
{
    Task<string> GetTokenAsync(RRSyncAccount account);
}
