using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses;

namespace CMS_Scrappers.Suppliers.VNDSWORLD;

/// <summary>
/// VNDSWORLD products.json -> ShopifyFlatProduct. No product-page enrichment: everything we need
/// (description, condition, prices in GBP) is already in the JSON, so there is no per-run cap either.
/// Decisions of 2026-10-18: Dubai (" DXB") variants are dropped, gender defaults to Men,
/// "Used" tag maps to "like new condition." (RRSync rating 4), the "Price review" tag is ignored.
/// </summary>
public class VndsWorldStrategy : IShopifyParsingStrategy
{
    private const string UsedTag = "Used";
    private const string DefaultGender = "Men";
    private const string UsedConditionGrade = "like new condition.";   // RRSyncService.MapConditionToRating -> 4

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VndsWorldStrategy> _logger;

    public VndsWorldStrategy(IServiceProvider serviceProvider, ILogger<VndsWorldStrategy> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<ShopifyFlatProduct>> MapAndEnrichProductAsync(ShopifyGetAllProductsResponse rawProduct, SupplierProfile profile)
    {
        var baseUrl = profile.BaseUrl;
        var products = rawProduct.Pages.SelectMany(p => p.Products).ToList();
        var result = new List<ShopifyFlatProduct>(products.Count);
        var droppedDubai = 0;

        foreach (var product in products)
        {
            var variants = (product.Variants ?? new List<Variant>())
                .Where(v =>
                {
                    var isDubai = (v.Option1 ?? "").TrimEnd().EndsWith(VndsWorldSizeMapper.DubaiSuffix, StringComparison.OrdinalIgnoreCase);
                    if (isDubai) droppedDubai++;
                    return !isDubai;
                })
                .Select(v => new ProductVariantRecordDTO
                {
                    Size = v.Option1 ?? "",
                    SKU = v.Sku ?? "",
                    Available = v.Available ? 1 : 0,
                    Price = ParsePrice(v.Price),
                })
                .ToList();

            if (variants.Count == 0)
            {
                _logger.LogInformation("Skipping {Handle}: only Dubai variants", product.Handle);
                continue;
            }

            var isUsed = (product.Tags ?? new List<string>()).Any(t => string.Equals(t, UsedTag, StringComparison.OrdinalIgnoreCase));
            var productUrl = $"{baseUrl}products/{product.Handle}";
            var (exists, existingDescription) = await ShouldSkip(productUrl);

            result.Add(new ShopifyFlatProduct
            {
                Id = product.Id,
                Title = product.Title ?? "",
                Handle = product.Handle ?? "",
                ProductUrl = productUrl,
                ScraperName = profile.Name,
                Images = (product.Images ?? new List<Image>())
                    .Select(i => new ProductImageRecordDTO { Priority = i.Position, Url = i.Src ?? "" })
                    .ToList(),
                Variants = variants,
                Category = product.ProductType ?? "",      // raw; the category mapper turns it into (Category, ProductType)
                Brand = product.Vendor ?? "",
                Gender = DefaultGender,
                Price = variants.First().Price,
                Retail_Price = 0,                          // the store never fills compare_at_price
                Description = exists ? existingDescription : StripHtml(product.BodyHtml),
                Condition = isUsed ? "Pre-Owned" : "New",
                ConditionGrade = isUsed ? UsedConditionGrade : "New",
                Enriched = true,                           // nothing left to fetch
                Status = "raw",
                New = !exists,
            });
        }

        if (droppedDubai > 0)
            _logger.LogInformation("VNDSWORLD: dropped {Count} Dubai-location variants", droppedDubai);

        return result;
    }

    public async Task<(bool ShouldSkip, string ExistingDescription)> ShouldSkip(string productUrl)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await db.Sdata
            .AsNoTracking()
            .Where(p => p.ProductUrl == productUrl)
            .Select(p => new { p.Description })
            .FirstOrDefaultAsync();
        return (existing != null, existing?.Description ?? "");
    }

    private static decimal ParsePrice(string? raw) =>
        decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0;

    /// <summary>body_html -> plain text: tags removed, entities decoded, whitespace collapsed, paragraphs kept as line breaks.</summary>
    public static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        foreach (var br in doc.DocumentNode.SelectNodes("//br|//p|//li|//div") ?? Enumerable.Empty<HtmlNode>())
            br.AppendChild(doc.CreateTextNode("\n"));
        var text = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\s*\n\s*", "\n");
        return text.Trim();
    }
}
