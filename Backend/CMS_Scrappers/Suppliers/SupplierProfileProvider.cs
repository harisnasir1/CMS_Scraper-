using CMS_Scrappers.Repositories.Interfaces;

namespace CMS_Scrappers.Suppliers;

public sealed class SupplierProfileProvider : ISupplierProfileProvider
{
    private const string RRSyncProvider = "RRSync";

    private readonly IScrapperRepository _scrappers;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupplierProfileProvider> _logger;

    public SupplierProfileProvider(IScrapperRepository scrappers, IConfiguration configuration, ILogger<SupplierProfileProvider> logger)
    {
        _scrappers = scrappers;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SupplierProfile?> GetByNameAsync(string name)
    {
        var row = await _scrappers.GetByName(name);
        return row == null ? null : await ToProfileAsync(row);
    }

    public async Task<SupplierProfile> RequireByNameAsync(string name)
    {
        return await GetByNameAsync(name)
               ?? throw new InvalidOperationException($"No Scrappers row named '{name}'");
    }

    public async Task<List<SupplierProfile>> GetEnabledAsync()
    {
        var rows = await _scrappers.GetEnabled();
        var profiles = new List<SupplierProfile>(rows.Count);
        foreach (var row in rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
            profiles.Add(await ToProfileAsync(row));
        return profiles;
    }

    private async Task<SupplierProfile> ToProfileAsync(Scrapper row)
    {
        if (string.IsNullOrWhiteSpace(row.Baseurl))
            throw new InvalidOperationException($"Scrappers row '{row.Name}' has no Baseurl");

        // Secrets never live in the tables: Suppliers__<Name>__StorefrontPassword / __RRSyncPassword in .env / appsettings
        var storefrontPassword = _configuration[$"Suppliers:{row.Name}:StorefrontPassword"];

        RRSyncAccount? rrsync = null;
        var account = await _scrappers.GetSyncAccount(row.ID, RRSyncProvider);
        if (account != null)
        {
            var password = _configuration[$"Suppliers:{row.Name}:RRSyncPassword"];
            if (string.IsNullOrWhiteSpace(password))
                _logger.LogWarning("Supplier {Name} has an RRSync account row but no Suppliers__{Name}__RRSyncPassword; RRSync sync will refuse to run", row.Name, row.Name);
            else if (string.IsNullOrWhiteSpace(account.SourceName) || string.IsNullOrWhiteSpace(account.UserName))
                _logger.LogWarning("Supplier {Name}: ScraperSyncAccounts row is missing SourceName or UserName; RRSync sync will refuse to run", row.Name);
            else
                rrsync = new RRSyncAccount(account.SourceName.Trim(), account.UserName.Trim(), password);
        }

        return new SupplierProfile(
            Id: row.ID,
            Name: row.Name,
            BaseUrl: row.Baseurl.TrimEnd('/') + "/",
            Currency: string.IsNullOrWhiteSpace(row.Currency) ? "USD" : row.Currency.Trim().ToUpperInvariant(),
            Enabled: row.Enabled,
            MinFirstPushPrice: row.MinFirstPushPrice,
            StorefrontPassword: string.IsNullOrWhiteSpace(storefrontPassword) ? null : storefrontPassword,
            RRSync: rrsync);
    }
}
