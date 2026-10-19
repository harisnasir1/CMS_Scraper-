namespace CMS_Scrappers.Suppliers;

/// <summary>Built once from every ISupplierModule in DI. Singleton; modules hold no state.</summary>
public sealed class SupplierRegistry : ISupplierRegistry
{
    private readonly Dictionary<string, ISupplierModule> _modules;

    public SupplierRegistry(IEnumerable<ISupplierModule> modules)
    {
        _modules = new Dictionary<string, ISupplierModule>(StringComparer.Ordinal);
        foreach (var module in modules)
        {
            var key = SupplierProfile.Normalize(module.Key);
            if (_modules.ContainsKey(key))
                throw new InvalidOperationException($"Two supplier modules registered for '{key}'");
            _modules[key] = module;
        }
    }

    public IReadOnlyCollection<ISupplierModule> All => _modules.Values;

    public bool Contains(string scraperName) => _modules.ContainsKey(SupplierProfile.Normalize(scraperName));

    public ISupplierModule Get(string scraperName)
    {
        if (_modules.TryGetValue(SupplierProfile.Normalize(scraperName), out var module)) return module;
        throw new InvalidOperationException(
            $"No supplier module registered for '{scraperName}'. Registered: {string.Join(", ", _modules.Keys)}");
    }
}
