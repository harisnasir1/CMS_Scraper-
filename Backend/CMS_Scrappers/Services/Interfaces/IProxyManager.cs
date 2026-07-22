using System.Net;
namespace CMS_Scrappers.Services.Interfaces;




public interface IProxyManager
{

    Task RefreshProxiesAsync(CancellationToken cancellationToken = default);
    bool HasProxies { get; }
    WebProxy? GetNextProxy();
}