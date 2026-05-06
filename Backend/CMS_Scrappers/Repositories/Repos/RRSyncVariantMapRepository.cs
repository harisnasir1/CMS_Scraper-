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
            
            _context.RRSyncVariantMaps.Add(data);
            await _context.SaveChangesAsync();
            return data.Id;

        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public async Task<List<RRSyncVariantMap>> GetAll(Guid sid)
    {
        try
        {
        var result=   await _context.RRSyncVariantMaps.Where(rv=>rv.RRSyncProductMapId == sid).ToListAsync();
        if(result==null) return  new List<RRSyncVariantMap>();
        return result;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    
    public async Task<RRSyncVariantMap> Get(Guid sid)
    {
       
            var result=   await _context.RRSyncVariantMaps.Include(p=>p.Variant).FirstOrDefaultAsync(rv=>rv.RRSyncProductMapId == sid);
           if(result==null) return new RRSyncVariantMap();
            return result;
    
    }

    public async Task<bool> UpdateStatus(Guid rrProductId, string status)
    {
        try
        {
            if(status==null ||rrProductId==Guid.Empty) return false;
            
            var data = await _context.RRSyncVariantMaps
                .FirstOrDefaultAsync(rv => rv.RRSyncProductMapId == rrProductId);
        
            if (data == null) return false;
        
            data.SyncStatus = status;
            data.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogCritical($"Error Updating RRsync varaint mapping{e}");
            return false;
        }
    }

    public Task<bool> Delete(Guid sid)
    {
        throw new NotImplementedException();
    }
}