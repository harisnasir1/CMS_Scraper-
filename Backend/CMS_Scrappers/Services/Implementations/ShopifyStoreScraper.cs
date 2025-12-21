 using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.Services.Interfaces;
namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
public class ShopifyStoreScraper : IScrappers
{
    private readonly ILogger<ShopifyStoreScraper> _logger;
    private readonly ShoipfyScrapper _shopifyClient;
    private readonly IShopifyParsingStrategy _parsingStrategy;
    private readonly IScrapperRepository _scrapperRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISdataRepository _sdataRepository;
    private readonly string _scraperName;
    private readonly string _storeBaseUrl;
    private readonly IUpdateShopifyTaskQueue _updateShopifyTaskQueue;

    private DateTime TimeStart { get; set; }
    private DateTime TimeEnd { get; set; }
    public ShopifyStoreScraper(
        string scraperName,
        string storeBaseUrl,
        ILogger<ShopifyStoreScraper> logger,
        ShoipfyScrapper shopifyClient,
        IShopifyParsingStrategy parsingStrategy,
        IScrapperRepository scrapperRepository
        ,IServiceProvider serviceProvider
        , ISdataRepository sdataRepository
        ,IUpdateShopifyTaskQueue updateShopifyTaskQueue
        )
        {
         _scraperName = scraperName;
         _storeBaseUrl = storeBaseUrl;
         _logger = logger;
         _shopifyClient = shopifyClient;
         _parsingStrategy = parsingStrategy;
         _scrapperRepository = scrapperRepository;
         _serviceProvider=serviceProvider;
         _sdataRepository = sdataRepository;
         _updateShopifyTaskQueue = updateShopifyTaskQueue;
        }

    public async Task ScrapeAsync()
    {
        _logger.LogInformation($"Starting scraping for the {_scraperName}");

        Guid scrapperid = await Getscrapeid("Savonches");

        var start = await _scrapperRepository.Startrun("Savonches");

        TimeStart = DateTime.UtcNow;

        var rawProduct = await _shopifyClient.Getproducts(_storeBaseUrl);
        int i = 0;
        foreach (var page in rawProduct.Pages)
        {
            _logger.LogWarning($"Scrapping page {i} done! ");
            var batchResponse = new ShopifyGetAllProductsResponse
            {
                Pages = new List<ShopifyStoreProductsResponse> { page }
            };

          
            List<ShopifyFlatProduct> flatBatch = await _parsingStrategy.MapAndEnrichProductAsync(batchResponse, _storeBaseUrl);

            var categoryFactory = _serviceProvider.GetRequiredService<ICategoryMapperFact>();
            var categoryMapper = categoryFactory.GetCategoryMapper("savonches");

            List<ShopifyFlatProduct> trendBatch = categoryMapper.TrendCategoryMapper(flatBatch);

            await _sdataRepository.Add(trendBatch, scrapperid);

            await Updateliveproducts(flatBatch);
            i++;
        }


        TimeEnd = DateTime.UtcNow;

        TimeSpan Diff = TimeEnd - TimeStart;

        await _scrapperRepository.Stoprun(Diff.ToString(), "Savonches");

        _logger.LogInformation("Finished scraping for {ScraperName}", _scraperName, "in time ", Diff);
    }

    public async Task<Guid >Getscrapeid(string name)
    {
       var scrape = _serviceProvider.GetRequiredService<IScrapperRepository>();
       return await scrape.Giveidbyname(name);
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

        var dbexistingproducts = await _sdataRepository.Giveliveproduct(existingProducts);
 
        // Check if we have database products to update
        if (dbexistingproducts.Count <= 0)
        { 
            _logger.LogWarning("No live products found in database for updates");
            return; 
        }

       //_updateShopifyTaskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) => {
       //    try
       //    {
       //        _logger.LogInformation($"Shopify update queue is processing {existingProducts.Count} products");
       //        using var scope = serviceProvider.CreateScope();
       //        var shclient = scope.ServiceProvider.GetService<IShopifyService>();
       //        
       //        if (shclient == null)
       //        {
       //            _logger.LogError("IShopifyService not found in service provider");
       //            return;
       //        }
       //        
       //        await shclient.UpdateProduct(existingProducts, dbexistingproducts);
       //        _logger.LogInformation("Shopify update completed successfully");
       //    }
       //    catch (Exception ex)
       //    {
       //        _logger.LogError(ex, "Error occurred while updating Shopify products");
       //    }
       //});
    }
}