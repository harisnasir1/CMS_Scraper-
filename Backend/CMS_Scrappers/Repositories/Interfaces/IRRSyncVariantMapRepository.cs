using CMS_Scrappers.Data.Requests;

namespace CMS_Scrappers.Repositories.Interfaces;

public interface IRRSyncVariantMapRepository
{
    Task<Guid> insert(RRSyncVariantMap data);
    Task<Dictionary<string,RRSyncVariantMap>> GetAll(Guid sid);
    Task<RRSyncVariantMap> Get(Guid sid);
    Task<bool> UpdateStatus(long vid,string status);
    Task TouchVariantMapsAfterSync(List<string> rrsyncVariantIds);
    /// <summary>Active maps of ONE supplier's Live products whose variant has not been seen since the threshold.</summary>
    Task<List<RRSyncVariantMap>> GetStaleVariantMaps(DateTime threshold, Guid scraperId);
    Task<List<RRSyncVariantMap>> GetActiveByProductMapId(Guid productMapId);
    Task MarkAsDeleted(List<Guid> ids);
    Task<bool> Delete(Guid sid);
}