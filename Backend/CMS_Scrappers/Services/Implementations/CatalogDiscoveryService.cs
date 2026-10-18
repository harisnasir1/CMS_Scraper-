using System.Globalization;
using CMS_Scrappers.Data.DTO;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

namespace CMS_Scrappers.Services.Implementations;

public class CatalogDiscoveryService : ICatalogDiscoveryService
{
    private const int MaxListedValues = 300;
    private const int SampleSize = 5;

    private readonly Scrap_shopify _fetcher;
    private readonly IScrapperRepository _scrapperRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CatalogDiscoveryService> _logger;

    public CatalogDiscoveryService(
        Scrap_shopify fetcher,
        IScrapperRepository scrapperRepository,
        IConfiguration configuration,
        ILogger<CatalogDiscoveryService> logger)
    {
        _fetcher = fetcher;
        _scrapperRepository = scrapperRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CatalogSummary> SummarizeAsync(string scraperName)
    {
        var scraper = await _scrapperRepository.GetByName(scraperName)
                      ?? throw new InvalidOperationException($"No Scrappers row named '{scraperName}'");

        if (string.IsNullOrWhiteSpace(scraper.Baseurl))
            throw new InvalidOperationException($"Scrappers row '{scraper.Name}' has no Baseurl");

        // Secrets stay in configuration (.env on the server), keyed by the supplier's Name
        var password = _configuration[$"Suppliers:{scraper.Name}:StorefrontPassword"];

        _logger.LogInformation("Catalog discovery for {Name} at {Base} (storefront password: {HasPw})",
            scraper.Name, scraper.Baseurl, !string.IsNullOrEmpty(password));

        var raw = await _fetcher.Getproducts(scraper.Baseurl, password);
        var products = raw.Pages.SelectMany(p => p.Products).ToList();

        var summary = new CatalogSummary
        {
            Scraper = scraper.Name,
            BaseUrl = scraper.Baseurl,
            UsedStorefrontPassword = !string.IsNullOrEmpty(password),
            Pages = raw.Pages.Count,
            TotalProducts = products.Count,
        };

        if (products.Count == 0)
        {
            _logger.LogWarning("Catalog discovery for {Name} fetched 0 products; check the base URL, the password and the proxies", scraper.Name);
            return summary;
        }

        var variants = products.SelectMany(p => p.Variants ?? new List<Variant>()).ToList();
        summary.TotalVariants = variants.Count;

        summary.ProductTypes = CountValues(products.Select(p => p.ProductType));
        summary.Tags = CountValues(products.SelectMany(p => p.Tags ?? new List<string>()));
        summary.Vendors = CountValues(products.Select(p => p.Vendor));
        summary.OptionNames = CountValues(products.SelectMany(p => (p.Options ?? new List<Option>()).Select(o => o.Name)));
        summary.Option1Values = CountValues(variants.Select(v => v.Option1));

        var prices = variants
            .Select(v => decimal.TryParse(v.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : (decimal?)null)
            .Where(d => d.HasValue && d.Value > 0)
            .Select(d => d!.Value)
            .ToList();
        if (prices.Count > 0)
        {
            summary.PriceMin = prices.Min();
            summary.PriceMax = prices.Max();
            summary.PriceAverage = Math.Round(prices.Average(), 2);
        }

        summary.VariantsWithCompareAtPrice = variants.Count(v => !string.IsNullOrWhiteSpace(v.CompareAtPrice?.ToString()));
        summary.ProductsWithDescription = products.Count(p => !string.IsNullOrWhiteSpace(p.BodyHtml));
        summary.ProductsWithNoImages = products.Count(p => p.Images == null || p.Images.Count == 0);

        summary.Samples = products
            .Take(SampleSize)
            .Select(p => new CatalogSampleProduct
            {
                Title = p.Title ?? "",
                Handle = p.Handle ?? "",
                Vendor = p.Vendor ?? "",
                ProductType = p.ProductType ?? "",
                Tags = p.Tags ?? new List<string>(),
                OptionNames = (p.Options ?? new List<Option>()).Select(o => o.Name ?? "").ToList(),
                Sizes = (p.Variants ?? new List<Variant>()).Select(v => v.Option1 ?? "").ToList(),
                Price = p.Variants?.FirstOrDefault()?.Price ?? "",
                CompareAtPrice = p.Variants?.FirstOrDefault()?.CompareAtPrice?.ToString(),
                Images = p.Images?.Count ?? 0,
                HasDescription = !string.IsNullOrWhiteSpace(p.BodyHtml),
            })
            .ToList();

        _logger.LogInformation("Catalog discovery for {Name}: {Products} products, {Types} product types, {Sizes} distinct option1 values",
            scraper.Name, summary.TotalProducts, summary.ProductTypes.Count, summary.Option1Values.Count);

        return summary;
    }

    private static List<ValueCount> CountValues(IEnumerable<string?> values)
    {
        return values
            .Select(v => (v ?? "").Trim())
            .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ValueCount { Value = g.Key == "" ? "(empty)" : g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .Take(MaxListedValues)
            .ToList();
    }
}
