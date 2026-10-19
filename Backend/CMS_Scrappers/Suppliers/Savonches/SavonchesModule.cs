using CMS_Scrappers.Strategies.Interface;
using CMS_Scrappers.Utils.variantMapperSync;

namespace CMS_Scrappers.Suppliers.Savonches;

/// <summary>Savonches (USD, public store, HTML enrichment). Strategy and mappers live in their original files.</summary>
public sealed class SavonchesModule : ISupplierModule, ISupplierServiceRegistrar
{
    public string Key => "savonches";

    public void Register(IServiceCollection services)
    {
        services.AddScoped<SavonchesStrategy>();
        services.AddScoped<SavonchesCategoryMapper>();
        services.AddScoped<SavonchesSyncSize>();
    }

    public IShopifyParsingStrategy ResolveStrategy(IServiceProvider services) => services.GetRequiredService<SavonchesStrategy>();
    public ICategoryMappingStrategy ResolveCategoryMapper(IServiceProvider services) => services.GetRequiredService<SavonchesCategoryMapper>();
    public ISizeMappingStrategy ResolveSizeMapper(IServiceProvider services) => services.GetRequiredService<SavonchesSyncSize>();
}
