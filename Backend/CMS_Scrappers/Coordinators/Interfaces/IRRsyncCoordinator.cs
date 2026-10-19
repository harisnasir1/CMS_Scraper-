using CMS_Scrappers.Suppliers;

namespace CMS_Scrappers.Coordinators.Interfaces;

public interface IRRsyncCoordinator
{
    /// <summary>Pushes new/changed Live products of ONE supplier to RRSync.</summary>
    Task<bool> Syncportal(DateTime scrapeStartedAt, SupplierProfile profile);
    Task DeleteStaleFromRRSync();
}
