namespace CMS_Scrappers.Suppliers;

/// <summary>The only code that turns Scrappers rows (plus configuration secrets) into supplier profiles.</summary>
public interface ISupplierProfileProvider
{
    Task<SupplierProfile?> GetByNameAsync(string name);
    Task<SupplierProfile> RequireByNameAsync(string name);
    /// <summary>Enabled suppliers, ordered by name so runs are deterministic.</summary>
    Task<List<SupplierProfile>> GetEnabledAsync();
}
