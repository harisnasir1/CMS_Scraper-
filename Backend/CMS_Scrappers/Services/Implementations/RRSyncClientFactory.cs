using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Suppliers;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations;

public sealed class RRSyncClientFactory : IRRSyncClientFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly RRSyncConfig _config;
    private readonly IRRSyncAuthService _auth;
    private readonly ILoggerFactory _loggers;

    public RRSyncClientFactory(IHttpClientFactory httpFactory, RRSyncConfig config, IRRSyncAuthService auth, ILoggerFactory loggers)
    {
        _httpFactory = httpFactory;
        _config = config;
        _auth = auth;
        _loggers = loggers;
    }

    public IRRSyncService For(RRSyncAccount account) =>
        new RRSyncService(_loggers.CreateLogger<RRSyncService>(), _config, _auth, _httpFactory.CreateClient(), account);
}
