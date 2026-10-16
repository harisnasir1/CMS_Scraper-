using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CMS_Scrappers.Services.Implementations;

/// <summary>
/// Drives an auto-approve run. Resolves the candidate list once, then handles each
/// product in its own DI scope so a long run does not accumulate tracked entities
/// in a single DbContext and one product's failure cannot poison the next.
/// Registered as a singleton; safe to call from hosted services and controllers.
/// </summary>
public class AutoApproveRunner : IAutoApproveRunner
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoApproveRunner> _logger;
    private readonly AutoApproveSettings _settings;
    private  int _slot;
    public AutoApproveRunner(IServiceProvider serviceProvider, ILogger<AutoApproveRunner> logger, AutoApproveSettings settings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settings = settings;
    }

    public async Task<int> RunAsync(Guid scraperId, int? limit, CancellationToken token = default)
    {
        if (Volatile.Read(ref _slot) == 0)
            throw new InvalidOperationException("Call TryReserve() before RunAsync()");
        try
        {

       
        var effectiveLimit = _settings.EffectiveLimit(limit);

        List<Guid> ids;
        using (var scope = _serviceProvider.CreateScope())
        {
            var products = scope.ServiceProvider.GetRequiredService<IProducts>();
            ids = await products.GetAutoApproveCandidates(scraperId, effectiveLimit);
        }

        _logger.LogInformation("AutoApprove: {Count} candidate products for scraper {Sid} (limit {Limit}, hard max {Max})",
            ids.Count, scraperId, effectiveLimit, AutoApproveSettings.HardMaxPerRun);

        int approved = 0;
        int skipped = 0;

        foreach (var id in ids)
        {
            if (token.IsCancellationRequested) break;

            using var scope = _serviceProvider.CreateScope();
            var products = scope.ServiceProvider.GetRequiredService<IProducts>();
            try
            {
                if (await products.AutoApproveProduct(id)) approved++;
                else skipped++;
            }
            catch (Exception ex)
            {
                skipped++;
                _logger.LogError(ex, "AutoApprove: unhandled failure for {Id}", id);
            }
        }

        _logger.LogInformation("AutoApprove: {Approved} live, {Skipped} left in review", approved, skipped);
        return approved;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            this.tryEnd();
        }
    }

    public bool tryBegin()
    {
        return Interlocked.CompareExchange(ref _slot, 1, 0) == 0;
    }

    public void tryEnd()
    {
         Interlocked.CompareExchange(ref _slot, 0, 1) ;
    }
}
