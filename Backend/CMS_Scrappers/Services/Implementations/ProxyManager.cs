using System.Net;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using Microsoft.Extensions.Options;

namespace CMS_Scrappers.Services.Implementations;
public class ProxyModel
{
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class ProxyManager:IProxyManager
{
    private readonly List<ProxyModel> _proxies = new();
    private readonly HttpClient _httpClient;
    private int _currentIndex = 0;
    private readonly object _lock = new();
    private readonly ILogger<ProxyManager> _logger;
    private readonly ProxySettings _settings;
    public ProxyManager(HttpClient httpClient,  ILogger<ProxyManager> logger, ProxySettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
    }
    public async Task RefreshProxiesAsync(CancellationToken cancellationToken = default)
    {
        
        
        if (!string.IsNullOrWhiteSpace(_settings.ApiUrl))
        {
            try
            {
                _logger.LogInformation("[ProxyManager] Fetching fresh proxies from API...");
                var response = await _httpClient.GetStringAsync(_settings.ApiUrl, cancellationToken);
                var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var loadedProxies = ParseProxyLines(lines);
                if (loadedProxies.Count > 0)
                {
                    lock (_lock)
                    {
                        _proxies.Clear();
                        _proxies.AddRange(loadedProxies);
                        _currentIndex = 0;
                    }
                    _logger.LogInformation($"[ProxyManager] Successfully loaded {_proxies.Count} proxies from API.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProxyManager] Failed to fetch proxies from API. Falling back to alternative sources.");
            }
        }

       
        var envProxies =_settings.FallbackList;
        if (!string.IsNullOrWhiteSpace(envProxies))
        {
            var lines = envProxies.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            LoadProxies(ParseProxyLines(lines));
            _logger.LogWarning($"[ProxyManager] Loaded {_proxies.Count} fallback proxies from Railway Environment.");
            return;
        }

        
        if (File.Exists(_settings.LocalFilePath))
        {
            var lines = await File.ReadAllLinesAsync(_settings.LocalFilePath, cancellationToken);
            LoadProxies(ParseProxyLines(lines));
            _logger.LogWarning($"[ProxyManager] Loaded {_proxies.Count} fallback proxies from local proxies.txt file.");
            return;
        }

        _logger.LogError("[ProxyManager] No valid proxies found from API, Environment, or Local File!");
    }
    
    public bool HasProxies => _proxies.Count > 0;

    public WebProxy? GetNextProxy()
    {
        if (!HasProxies) return null;

        lock (_lock)
        {
            var selected = _proxies[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _proxies.Count; 

            var webProxy = new WebProxy(selected.Address, selected.Port)
            {
                BypassProxyOnLocal = false,
                UseDefaultCredentials = false
            };

            if (!string.IsNullOrEmpty(selected.Username))
            {
                webProxy.Credentials = new NetworkCredential(selected.Username, selected.Password);
            }

            return webProxy;
        }
    }
    private List<ProxyModel> ParseProxyLines(string[] lines)
    {
        var parsedList = new List<ProxyModel>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split(':');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int port))
            {
                parsedList.Add(new ProxyModel
                {
                    Address = parts[0],
                    Port = port,
                    Username = parts.Length >= 4 ? parts[2] : null,
                    Password = parts.Length >= 4 ? parts[3] : null
                });
            }
        }

        return parsedList;
    }
    private void LoadProxies(List<ProxyModel> proxies)
    {
        lock (_lock)
        {
            _proxies.Clear();
            _proxies.AddRange(proxies);
            _currentIndex = 0;
        }
    }
}