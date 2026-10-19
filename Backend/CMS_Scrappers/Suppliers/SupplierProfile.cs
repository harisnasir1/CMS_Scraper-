namespace CMS_Scrappers.Suppliers;

/// <summary>Login + source for one supplier on RRSync. Password comes from configuration, not the DB.</summary>
public sealed record RRSyncAccount(string SourceName, string UserName, string Password);

/// <summary>
/// Everything the pipeline needs to know about one supplier, loaded from the Scrappers row, its
/// ScraperSyncAccounts row and the secrets in configuration. Immutable for the duration of a run.
/// </summary>
public sealed record SupplierProfile(
    Guid Id,
    string Name,
    string BaseUrl,
    string Currency,
    bool Enabled,
    decimal MinFirstPushPrice,
    string? StorefrontPassword,
    RRSyncAccount? RRSync)
{
    /// <summary>Normalised lookup key, shared with the registry and the pricing resolver.</summary>
    public string Key => Normalize(Name);

    /// <summary>The RRSync account, or a clear error naming what is missing.</summary>
    public RRSyncAccount RequireRRSync() =>
        RRSync ?? throw new InvalidOperationException(
            $"Supplier '{Name}' has no usable RRSync account: add a ScraperSyncAccounts row (Provider 'RRSync') and set Suppliers__{Name}__RRSyncPassword");

    public static string Normalize(string name) => (name ?? "").Trim().ToLowerInvariant();
}
