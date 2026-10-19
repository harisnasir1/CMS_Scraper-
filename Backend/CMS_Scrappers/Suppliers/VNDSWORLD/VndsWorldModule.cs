using CMS_Scrappers.Strategies.Interface;

namespace CMS_Scrappers.Suppliers.VNDSWORLD;

/// <summary>VNDSWORLD (GBP, storefront password, no enrichment).</summary>
public sealed class VndsWorldModule : ISupplierModule, ISupplierServiceRegistrar
{
    public string Key => "vndsworld";

    public void Register(IServiceCollection services)
    {
        services.AddScoped<VndsWorldStrategy>();
        services.AddScoped<VndsWorldCategoryMapper>();
        services.AddScoped<VndsWorldSizeMapper>();
    }

    public IShopifyParsingStrategy ResolveStrategy(IServiceProvider services) => services.GetRequiredService<VndsWorldStrategy>();
    public ICategoryMappingStrategy ResolveCategoryMapper(IServiceProvider services) => services.GetRequiredService<VndsWorldCategoryMapper>();
    public ISizeMappingStrategy ResolveSizeMapper(IServiceProvider services) => services.GetRequiredService<VndsWorldSizeMapper>();
}
