namespace CMS_Scrappers.Repositories.Interfaces;

public interface IRRSyncProductMapRepository
{
    Task<Guid> Insertmap(RRSyncProductMap data);
    Task<List<RRSyncProductMap>> GetAll();
    Task<RRSyncProductMap> Get(Guid sid);
    /// <summary>Sets SyncStatus on the map of a product (looked up by SdataId).</summary>
    Task<bool> Update(Guid sid,string status);
    /// <summary>Sets SyncStatus on one map row by its own Id.</summary>
    Task<bool> UpdateStatusById(Guid mapId, string status);
    Task<bool> Delete(Guid sid);
    Task<List<Guid>> GiveLiveProductsForRRSync(List<Guid> sdataIds);
}
