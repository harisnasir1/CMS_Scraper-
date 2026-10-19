using CMS_Scrappers.Suppliers;

namespace CMS_Scrappers.Services.Interfaces;

/// <summary>
/// The only way to get an RRSync client. Binding it to an account up front means every call
/// is made as the right seller; there is no "default" account to fall back to by accident.
/// </summary>
public interface IRRSyncClientFactory
{
    IRRSyncService For(RRSyncAccount account);
}
