using CMS_Scrappers.Data.DTO;

namespace CMS_Scrappers.Services.Interfaces;

public interface ICatalogDiscoveryService
{
    /// <summary>
    /// Fetches the supplier's whole products.json through the normal fetcher (proxies, storefront
    /// password from configuration) and aggregates it. Reads one Scrappers row; writes nothing.
    /// </summary>
    Task<CatalogSummary> SummarizeAsync(string scraperName, int? maxPages = null);
}
