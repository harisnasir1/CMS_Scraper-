 using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Suppliers;
namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
public class ShopifyStoreScraper : IScrappers
{
    private readonly ILogger<ShopifyStoreScraper> _logger;
    private readonly ShoipfyScrapper _shopifyClient;
    private readonly IShopifyParsingStrategy _parsingStrategy;
    private readonly IScrapperRepository _scrapperRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISdataRepository _sdataRepository;
    private readonly SupplierProfile _profile;
    private readonly ISupplierModule _module;
    private readonly IProductSyncCoordinator _productSyncCoordinator;
    private readonly IUpdateShopifyTaskQueue _updateShopifyTaskQueue;
    private readonly IRRsyncCoordinator _rrsyncCoordinator;
    private DateTime TimeStart { get; set; }
    private DateTime TimeEnd { get; set; }
    public ShopifyStoreScraper(
        SupplierProfile profile,
        ISupplierModule module,
        ILogger<ShopifyStoreScraper> logger,
        ShoipfyScrapper shopifyClient,
        IScrapperRepository scrapperRepository
        ,IServiceProvider serviceProvider
        , ISdataRepository sdataRepository
        ,IUpdateShopifyTaskQueue updateShopifyTaskQueue,
        IProductSyncCoordinator productSyncCoordinator,
        IRRsyncCoordinator rrsyncCoordinator
        )
        {
         _profile = profile;
         _module = module;
         _logger = logger;
         _shopifyClient = shopifyClient;
         _parsingStrategy = module.ResolveStrategy(serviceProvider);
         _scrapperRepository = scrapperRepository;
         _serviceProvider=serviceProvider;
         _sdataRepository = sdataRepository;
         _updateShopifyTaskQueue = updateShopifyTaskQueue;
         _productSyncCoordinator = productSyncCoordinator;
         _rrsyncCoordinator = rrsyncCoordinator;
        }

    public async Task ScrapeAsync()
    {
        _logger.LogInformation("Starting scraping for {Name} at {Base}", _profile.Name, _profile.BaseUrl);

        Guid scrapperid = _profile.Id;
        List<ShopifyFlatProduct> FullflatBatch = new List<ShopifyFlatProduct>();
        var start = await _scrapperRepository.Startrun(_profile.Name);

        TimeStart = DateTime.UtcNow;
        
        try
        {
            var categoryMapper = new CategoryMapper(
                _serviceProvider.GetRequiredService<ILogger<CategoryMapper>>(),
                _module.ResolveCategoryMapper(_serviceProvider));
            var rawProduct = await _shopifyClient.Getproducts(_profile.BaseUrl, _profile.StorefrontPassword);
            if (rawProduct.Pages.Count == 0)
            {
                // Nothing fetched means the supplier or proxies are blocked, not an empty catalogue; don't sync anything off it
                _logger.LogError("Scrape for {Name} fetched 0 pages from {Url}; skipping DB/RRSync/Shopify updates for this run", _profile.Name, _profile.BaseUrl);
                return;
            }
            int i = 0;
            foreach (var page in rawProduct.Pages)
            {
                _logger.LogWarning($"Scrapping page {i} done! ");
                var batchResponse = new ShopifyGetAllProductsResponse
                {
                    Pages = new List<ShopifyStoreProductsResponse> { page }
                };

                List<ShopifyFlatProduct> flatBatch = await _parsingStrategy.MapAndEnrichProductAsync(batchResponse, _profile);

                List<ShopifyFlatProduct> trendBatch = categoryMapper.TrendCategoryMapper(flatBatch);

                await _sdataRepository.Add(trendBatch, scrapperid);
                FullflatBatch.AddRange(flatBatch);
                i++;
            }

            await updateRrsyncData(TimeStart);
            await Updateliveproducts(FullflatBatch);

            _logger.LogInformation("Finished scraping for {Name} in {Elapsed}", _profile.Name, DateTime.UtcNow - TimeStart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape for {Name} failed after {Elapsed}", _profile.Name, DateTime.UtcNow - TimeStart);
            throw;
        }
        finally
        {
            // Always release the "Running" status, otherwise the reconcile/cleanup jobs wait on a scraper that is not running
            TimeEnd = DateTime.UtcNow;
            await _scrapperRepository.Stoprun((TimeEnd - TimeStart).ToString(), _profile.Name);
        }
    }

    public async Task MarkUnseenProductsAsSourceDeleted()
    {
        Guid scrapperid = _profile.Id;
        var maxWait = 6;
        var waited = 0;
        while (waited < maxWait)
        {
            var current = await _scrapperRepository.Getscrapeid(scrapperid);
            if (current == null || current.Status != "Running") break;
            _logger.LogInformation(
                "Scraper still running — waiting 5 min ({Attempt}/{Max})", 
                waited + 1, maxWait);
            await Task.Delay(TimeSpan.FromMinutes(5));
            waited++;
        }
        if (waited >= maxWait)
        {
            _logger.LogError(
                "Scraper still running after {Min} min — aborting cleanup", maxWait * 5);
            return;
        }
       
        var scraper = await _scrapperRepository.Getscrapeid(scrapperid);
        if (scraper == null) return;
        if (scraper.Lastrun < DateTime.UtcNow.AddHours(-8))
        {
            _logger.LogWarning(
                "Scraper {Name} last ran at {Last} — too old, skipping cleanup",
                scraper.Name, scraper.Lastrun);
            return;
        }
        if (!TimeSpan.TryParse(scraper.Runtime, out var runtime) || runtime.TotalMinutes < 5)
        {
            _logger.LogError(
                "Scraper runtime {Runtime} is under 5 min — something is wrong",
                scraper.Runtime);
            return;
        }
        var threshold = DateTime.UtcNow.AddHours(-48);
        await _sdataRepository.DelunseenData(scrapperid, threshold);
        _logger.LogInformation("Cleanup done: {Products} products marked SourceDeleted");
    }

    private async Task updateRrsyncData(DateTime time)
    {
        try
        {
            await _rrsyncCoordinator.Syncportal(time, _profile);
            await _rrsyncCoordinator.DeleteStaleFromRRSync(_profile);
        }
        catch (Exception e)
        {
            _logger.LogError("error updating RRSync products {Message} ",e.ToString());
            throw;
        }
    }

    private async Task Updateliveproducts(List<ShopifyFlatProduct> data)
    { 
        var existingProducts = data.Where(p => !p.New).ToList();
        
        // Early return if no existing products to update
        if (!existingProducts.Any())
        {
            _logger.LogInformation("No existing products found to update");
            return;
        }
        await _productSyncCoordinator.UpdateProduct_Coordinator(existingProducts);
        await _productSyncCoordinator.DeleteLiveProducts(_profile.Id);
    }
}