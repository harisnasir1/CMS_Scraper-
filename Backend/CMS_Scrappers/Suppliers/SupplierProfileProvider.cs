using CMS_Scrappers.Repositories.Interfaces;

namespace CMS_Scrappers.Suppliers;

public sealed class SupplierProfileProvider : ISupplierProfileProvider
{
    private readonly IScrapperRepository _scrappers;
    private readonly IConfiguration _configuration;

    public SupplierProfileProvider(IScrapperRepository scrappers, IConfiguration configuration)
    {
        _scrappers = scrappers;
        _configuration = configuration;
    }

    public async Task<SupplierProfile?> GetByNameAsync(string name)
    {
        var row = await _scrappers.GetByName(name);
        return row == null ? null : ToProfile(row);
    }

    public async Task<SupplierProfile> RequireByNameAsync(string name)
    {
        return await GetByNameAsync(name)
               ?? throw new InvalidOperationException($"No Scrappers row named '{name}'");
    }

    public async Task<List<SupplierProfile>> GetEnabledAsync()
    {
        var rows = await _scrappers.GetEnabled();
        return rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Select(ToProfile).ToList();
    }

    private SupplierProfile ToProfile(Scrapper row)
    {
        if (string.IsNullOrWhiteSpace(row.Baseurl))
            throw new InvalidOperationException($"Scrappers row '{row.Name}' has no Baseurl");

        // Secrets never live in the table: Suppliers__<Name>__StorefrontPassword in .env / appsettings
        var password = _configuration[$"Suppliers:{row.Name}:StorefrontPassword"];

        return new SupplierProfile(
            Id: row.ID,
            Name: row.Name,
            BaseUrl: row.Baseurl.TrimEnd('/') + "/",
            Currency: string.IsNullOrWhiteSpace(row.Currency) ? "USD" : row.Currency.Trim().ToUpperInvariant(),
            Enabled: row.Enabled,
            MinFirstPushPrice: row.MinFirstPushPrice,
            StorefrontPassword: string.IsNullOrWhiteSpace(password) ? null : password);
    }
}
