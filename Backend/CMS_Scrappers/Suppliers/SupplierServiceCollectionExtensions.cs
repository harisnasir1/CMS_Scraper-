namespace CMS_Scrappers.Suppliers;

public static class SupplierServiceCollectionExtensions
{
    /// <summary>Registers a supplier module. The module registers its own strategy and mappers.</summary>
    public static IServiceCollection AddSupplier<TModule>(this IServiceCollection services)
        where TModule : class, ISupplierModule, new()
    {
        var module = new TModule();
        module.RegisterServices(services);
        services.AddSingleton<ISupplierModule>(module);
        return services;
    }

    private static void RegisterServices(this ISupplierModule module, IServiceCollection services)
    {
        if (module is ISupplierServiceRegistrar registrar) registrar.Register(services);
    }
}

/// <summary>Optional: a module that needs its own DI registrations implements this.</summary>
public interface ISupplierServiceRegistrar
{
    void Register(IServiceCollection services);
}
