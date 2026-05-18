using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Repositories.Repos;

public class RRSyncVariantMapRepository:IRRSyncVariantMapRepository
{
    private static readonly Guid _userId = new Guid("0b651c37-c448-42cd-a06e-e01144285502");
    private readonly AppDbContext _context;
    private readonly ILogger<RRSyncVariantMapRepository> _logger;
    public RRSyncVariantMapRepository(AppDbContext context, ILogger<RRSyncVariantMapRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> insert(RRSyncVariantMap data)
    {
        try
        {
            if(data==null) return Guid.Empty;
            
            _context.RRSyncVariantMap.Add(data);
            await _context.SaveChangesAsync();
            return data.Id;

        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<Dictionary<string,RRSyncVariantMap>> GetAll(Guid sid)
    {
        try
        {
        var result=   await _context.RRSyncVariantMap.Where(rv=>rv.RRSyncProductMapId == sid).ToDictionaryAsync(rv=>rv.RRSyncVariantId,rv=>rv);
        if(result==null) return  null;
        return result;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    
    public async Task<RRSyncVariantMap> Get(Guid sid)
    {
       
            var result=   await _context.RRSyncVariantMap.Include(p=>p.Variant).FirstOrDefaultAsync(rv=>rv.RRSyncProductMapId == sid);
           if(result==null) return new RRSyncVariantMap();
            return result;
    
    }

    public async Task<bool> UpdateStatus(long vid, string status)
    {
        try
        {
            if(status==null || string.IsNullOrEmpty(status)) return false;
            var map=await _context.RRSyncVariantMap.FirstOrDefaultAsync(v=>v.VariantId==vid);
            map.SyncStatus = status;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogCritical($"Error Updating RRsync varaint mapping{e}");
            return false;
        }
    }
    public async Task TouchVariantMapsAfterSync(List<string> updates)
    {
        
        await _context.RRSyncVariantMap
            .Where(m => updates.Contains(m.RRSyncVariantId))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
    }
    public async Task<List<RRSyncVariantMap>> GetStaleVariantMaps(DateTime threshold)
    {
        return await _context.RRSyncVariantMap
            .Where(vm => vm.SyncStatus == "Active"
                         && _context.ProductVariants
                             .Any(v => v.Id == vm.VariantId
                                       && (v.LastViewed == null || v.LastViewed < threshold)))
            .ToListAsync();
    }
    public async Task MarkAsDeleted(List<Guid> ids)
    {
        await _context.RRSyncVariantMap
            .Where(vm => ids.Contains(vm.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.SyncStatus, "Deleted")
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));
    }
    public async Task<List<RRSyncVariantMap>> GetActiveByProductMapId(Guid productMapId)
    {
        return await _context.RRSyncVariantMap
            .Where(vm => vm.RRSyncProductMapId == productMapId
                         && vm.SyncStatus == "Active")
            .ToListAsync();
    }

    public Task<bool> Delete(Guid sid)
    {
        throw new NotImplementedException();
    }
}