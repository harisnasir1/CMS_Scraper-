using CMS_Scrappers.Strategies.Interface;

namespace CMS_Scrappers.Suppliers;

/// <summary>
/// The code side of a supplier: which parsing strategy, category mapper and size mapper it uses.
/// One implementation per supplier, registered with services.AddSupplier&lt;TModule&gt;().
/// Configuration (URL, currency, secrets) is NOT here; that is the SupplierProfile.
/// </summary>
public interface ISupplierModule
{
    /// <summary>Must equal SupplierProfile.Normalize(Scrappers.Name).</summary>
    string Key { get; }

    IShopifyParsingStrategy ResolveStrategy(IServiceProvider services);
    ICategoryMappingStrategy ResolveCategoryMapper(IServiceProvider services);
    ISizeMappingStrategy ResolveSizeMapper(IServiceProvider services);
}
