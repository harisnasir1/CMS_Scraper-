namespace CMS_Scrappers.Repositories.Interfaces;

public interface IRRSyncProductMapRepository
{
    Task<Guid> Insertmap(RRSyncProductMap data);
    Task<List<RRSyncProductMap>> GetAll();
    Task<RRSyncProductMap> Get(Guid sid);
    Task<bool> Update(Guid sid,string status);
    Task<bool> Delete(Guid sid);
    Task<List<Guid>> GiveLiveProductsForRRSync(List<Guid> sdataIds);
}