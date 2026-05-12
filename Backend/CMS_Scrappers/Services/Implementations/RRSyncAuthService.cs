using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations;

public class RRSyncAuthService:IRRSyncAuthService
{
    private readonly HttpClient _http;
    private readonly RRSyncConfig _configs;
    private readonly ILogger<RRSyncAuthService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _token;
    private DateTime _expiresAt = DateTime.MinValue;

    public RRSyncAuthService(
        HttpClient http,
        RRSyncConfig options,
        ILogger<RRSyncAuthService> logger)
    {
        _http = http;
        _configs = options;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync()
    {
        // happy path: token still valid (with 60s buffer to avoid edge expiry)
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt.AddSeconds(-60))
            return _token;

        await _lock.WaitAsync();
        try
        {
            // re-check after acquiring lock — another caller may have refreshed
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _expiresAt.AddSeconds(-60))
                return _token;

            await RefreshTokenAsync();
            return _token!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshTokenAsync()
    {
        var payload = new
        {
            userName = _configs.useremail,
            password = _configs.userpassword
        };

        var response = await _http.PostAsJsonAsync(
            $"{_configs.BaseURl}/api/Admin/login",
            payload);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Empty login response");

        _token = body.BearerToken;
        _expiresAt = DateTime.UtcNow.AddSeconds(body.ExpiresIn);

        _logger.LogInformation("RRSync token refreshed, expires at {Expiry}", _expiresAt);
    }

    private record LoginResponse(string BearerToken, string UserId, int ExpiresIn);
}