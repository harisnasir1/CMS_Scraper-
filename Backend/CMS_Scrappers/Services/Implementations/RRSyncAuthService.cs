using System.Collections.Concurrent;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Suppliers;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations;

public class RRSyncAuthService : IRRSyncAuthService
{
    private sealed class CachedToken
    {
        public string? Token;
        public DateTime ExpiresAt = DateTime.MinValue;
        public readonly SemaphoreSlim Lock = new(1, 1);
    }

    private readonly IHttpClientFactory _httpFactory;
    private readonly RRSyncConfig _configs;
    private readonly ILogger<RRSyncAuthService> _logger;
    private readonly ConcurrentDictionary<string, CachedToken> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public RRSyncAuthService(IHttpClientFactory httpFactory, RRSyncConfig options, ILogger<RRSyncAuthService> logger)
    {
        _httpFactory = httpFactory;
        _configs = options;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(RRSyncAccount account)
    {
        var entry = _tokens.GetOrAdd(account.UserName, _ => new CachedToken());

        // happy path: token still valid (with 60s buffer to avoid edge expiry)
        if (IsFresh(entry)) return entry.Token!;

        await entry.Lock.WaitAsync();
        try
        {
            // re-check after acquiring lock — another caller may have refreshed
            if (IsFresh(entry)) return entry.Token!;

            await RefreshTokenAsync(entry, account);
            return entry.Token!;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    private static bool IsFresh(CachedToken entry) =>
        !string.IsNullOrEmpty(entry.Token) && DateTime.UtcNow < entry.ExpiresAt.AddSeconds(-60);

    private async Task RefreshTokenAsync(CachedToken entry, RRSyncAccount account)
    {
        var payload = new
        {
            userName = account.UserName,
            password = account.Password
        };

        var http = _httpFactory.CreateClient();
        var response = await http.PostAsJsonAsync($"{_configs.BaseURl}/api/Admin/login", payload);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("RRSync login failed for {User}: HTTP {Status}", account.UserName, (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
        }

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Empty login response");

        entry.Token = body.BearerToken;
        entry.ExpiresAt = DateTime.UtcNow.AddSeconds(body.ExpiresIn);

        _logger.LogInformation("RRSync token refreshed for {User} (source {Source}), expires at {Expiry}",
            account.UserName, account.SourceName, entry.ExpiresAt);
    }

    private record LoginResponse(string BearerToken, string UserId, int ExpiresIn);
}
