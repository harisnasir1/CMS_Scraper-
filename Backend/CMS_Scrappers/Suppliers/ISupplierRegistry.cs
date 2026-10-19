namespace CMS_Scrappers.Suppliers;

public interface ISupplierRegistry
{
    /// <summary>Module for a supplier name; throws when no module is registered for it.</summary>
    ISupplierModule Get(string scraperName);
    bool Contains(string scraperName);
    IReadOnlyCollection<ISupplierModule> All { get; }
}
