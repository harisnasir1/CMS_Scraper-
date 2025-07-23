using HtmlAgilityPack;
using System.Text;
using System.Net.Http.Headers;
namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
public class ShopifyStoreScraper : IScrappers
{
    private readonly ILogger<ShopifyStoreScraper> _logger;
    private readonly ShoipfyScrapper _shopifyClient;
    private readonly IShopifyParsingStrategy _parsingStrategy;
    private readonly IScrapperRepository _scrapperRepository;

     private readonly IServiceProvider _serviceProvider;
    private readonly string _scraperName;
    private readonly string _storeBaseUrl;
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
        )
    {
        _scraperName = scraperName;
        _storeBaseUrl = storeBaseUrl;
        _logger = logger;
        _shopifyClient = shopifyClient;
        _parsingStrategy = parsingStrategy;
        _scrapperRepository = scrapperRepository;
        _serviceProvider=serviceProvider;
      
    }

    public async Task ScrapeAsync()
    {

        _logger.LogInformation($"Starting scraping for the {_scraperName}");

        var start = await _scrapperRepository.Startrun("SavonchesStrategy");

        TimeStart = DateTime.UtcNow;

        var rawProduct = await _shopifyClient.Getproducts(_storeBaseUrl);

        _logger.LogWarning(System.Text.Json.JsonSerializer.Serialize(rawProduct));

        List<ShopifyFlatProduct> flatProduct = await _parsingStrategy.MapAndEnrichProductAsync(rawProduct, _storeBaseUrl);


        TimeEnd = DateTime.UtcNow;

        var Categoyfact=_serviceProvider.GetRequiredService<ICategoryMapperFact>();
        var Category_Mapper=Categoyfact.GetCategoryMapper("savonches");
        var Trenddata=Category_Mapper.TrendCategoryMapper(flatProduct);

        _logger.LogError(System.Text.Json.JsonSerializer.Serialize(Trenddata));
        TimeSpan Diff = TimeEnd - TimeStart;

        await _scrapperRepository.Stoprun(Diff.ToString(), "Savonches");

        _logger.LogInformation("Finished scraping for {ScraperName}", _scraperName, "in time ", Diff);
    }


}