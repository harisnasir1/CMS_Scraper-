namespace CMS_Scrappers.Repositories.Interfaces;

public interface IRRSyncVariantMapRepository
{
    Task<Guid> insert(RRSyncVariantMap data);
    Task<List<RRSyncVariantMap>> GetAll(Guid sid);
    Task<RRSyncVariantMap> Get(Guid sid);
    Task<bool> UpdateStatus(Guid sid,string status);
    
    Task<bool> Delete(Guid sid);
}