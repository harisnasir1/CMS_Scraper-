using System.Net;
namespace CMS_Scrappers.Services.Interfaces;




public interface IProxyManager
{

    bool HasProxies { get; }


    WebProxy? GetNextProxy();
}