namespace CMS_Scrappers.Suppliers;

/// <summary>
/// Everything the pipeline needs to know about one supplier, loaded from the Scrappers row plus
/// the secrets in configuration. Immutable for the duration of a run.
/// </summary>
public sealed record SupplierProfile(
    Guid Id,
    string Name,
    string BaseUrl,
    string Currency,
    bool Enabled,
    decimal MinFirstPushPrice,
    string? StorefrontPassword)
{
    /// <summary>Normalised lookup key, shared with the registry and the pricing resolver.</summary>
    public string Key => Normalize(Name);

    public static string Normalize(string name) => (name ?? "").Trim().ToLowerInvariant();
}
