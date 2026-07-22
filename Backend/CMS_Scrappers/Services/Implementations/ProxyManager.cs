using System.Net;
using CMS_Scrappers.Services.Interfaces;

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
    private int _currentIndex = 0;
    private readonly object _lock = new();

    public ProxyManager(IConfiguration configuration, string localFilePath = "prox.txt")
    {
        
        var envProxies = configuration["PROXY_LIST"];

        if (!string.IsNullOrWhiteSpace(envProxies))
        {
            
            var lines = envProxies.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            LoadProxies(lines);
            Console.WriteLine($"[ProxyManager] Loaded {_proxies.Count} proxies from Railway Environment.");
        }
      
        else if (File.Exists(localFilePath))
        {
            var lines = File.ReadAllLines(localFilePath);
            LoadProxies(lines);
            Console.WriteLine($"[ProxyManager] Loaded {_proxies.Count} proxies from local file.");
        }
        else
        {
            Console.WriteLine("[Warning] No proxies found in Environment or Local File.");
        }
    }
    private void LoadProxies(string[] lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Trim().Split(':');
            if (parts.Length >= 2)
            {
                var proxy = new ProxyModel
                {
                    Address = parts[0],
                    Port = int.Parse(parts[1]),
                    Username = parts.Length >= 4 ? parts[2] : null,
                    Password = parts.Length >= 4 ? parts[3] : null
                };
                _proxies.Add(proxy);
            }
        }
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
}