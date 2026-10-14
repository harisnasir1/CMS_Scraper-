namespace CMS_Scrappers.Services.Interfaces;

public interface IAutoApproveRunner
{
    /// <summary>Auto-approves eligible products of a scraper. Returns how many went Live.</summary>
    Task<int> RunAsync(Guid scraperId, int? limit, CancellationToken token = default);
}
