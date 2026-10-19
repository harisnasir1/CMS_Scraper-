using CMS_Scrappers.Suppliers;

namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IRRsyncCoordinator
{
    /// <summary>Pushes new/changed Live products of ONE supplier to RRSync.</summary>
    Task<bool> Syncportal(DateTime scrapeStartedAt, SupplierProfile profile);
    /// <summary>Deletes ONE supplier's variants from RRSync when the scrape has not seen them for 24h, as that supplier's account.</summary>
    Task DeleteStaleFromRRSync(SupplierProfile profile);
}
