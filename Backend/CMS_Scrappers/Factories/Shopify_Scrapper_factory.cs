using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Suppliers;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

/// <summary>
/// No per-supplier code here: the profile comes from the Scrappers row, the strategy and mappers from
/// the supplier module registered in Program.cs. Adding a supplier = a row + a module.
/// </summary>
public class Shopify_Scrapper_factory : IShopifyScrapperFact
{
    private readonly ShoipfyScrapper _shopifyclient;
    private readonly ILogger<ShopifyStoreScraper> _logger;
    private readonly IScrapperRepository _scrapperRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISdataRepository _sdataRepository;
    private readonly IUpdateShopifyTaskQueue _updateShopifyTaskQueue;
    private readonly IProductSyncCoordinator _productSyncCoordinator;
    private readonly IRRsyncCoordinator _rsyncCoordinator;
    private readonly ISupplierProfileProvider _profiles;
    private readonly ISupplierRegistry _registry;

    public Shopify_Scrapper_factory(
        ShoipfyScrapper shoipfyScrapper,
        ILogger<ShopifyStoreScraper> logger,
        IScrapperRepository scrapperRepository,
        IServiceProvider serviceProvider,
        ISdataRepository sdataRepository,
        IUpdateShopifyTaskQueue updateShopifyTaskQueue,
        IProductSyncCoordinator productSyncCoordinator,
        IRRsyncCoordinator rsyncCoordinator,
        ISupplierProfileProvider profiles,
        ISupplierRegistry registry)
    {
        _shopifyclient = shoipfyScrapper;
        _logger = logger;
        _scrapperRepository = scrapperRepository;
        _serviceProvider = serviceProvider;
        _sdataRepository = sdataRepository;
        _updateShopifyTaskQueue = updateShopifyTaskQueue;
        _productSyncCoordinator = productSyncCoordinator;
        _rsyncCoordinator = rsyncCoordinator;
        _profiles = profiles;
        _registry = registry;
    }

    public async Task<ShopifyStoreScraper> CreateScraperAsync(string scraperName)
    {
        var profile = await _profiles.RequireByNameAsync(scraperName);
        var module = _registry.Get(profile.Name);   // throws with the registered keys listed when the row has no code
        return new ShopifyStoreScraper(profile, module, _logger, _shopifyclient, _scrapperRepository, _serviceProvider,
            _sdataRepository, _updateShopifyTaskQueue, _productSyncCoordinator, _rsyncCoordinator);
    }
}
