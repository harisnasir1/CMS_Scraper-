using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Repositories.Repos;

public class RRSyncProductMapRepository:IRRSyncProductMapRepository
{
    private static readonly Guid _userId = new Guid("0b651c37-c448-42cd-a06e-e01144285502");
    private readonly AppDbContext _context;
    private readonly ILogger<RRSyncProductMapRepository> _logger;
   
    public RRSyncProductMapRepository(AppDbContext context, ILogger<RRSyncProductMapRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> Insertmap(RRSyncProductMap data)
    {
        try
        {
            if(data==null) return Guid.Empty;
            await _context.RRSyncProductMap.AddAsync(data);
            await _context.SaveChangesAsync();
            return data.Id;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error inserting Product Mapping{ex}");
            throw;
        }
        
    }

    public async Task<List<RRSyncProductMap>> GetAll()
    {
        try
        {
            var result = await _context.RRSyncProductMap.ToListAsync();
            if (result == null) 
                return new List<RRSyncProductMap>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error Getting all RRsync Mapping{ex}");
            throw ;
        }
    }
    public async Task<RRSyncProductMap> Get(Guid sid)
    {
        try
        {
            var result = await _context.RRSyncProductMap.Include(p=>p.VariantMaps).FirstOrDefaultAsync(rs => rs.SyncStatus == "Active" && rs.SdataId==sid);
            if (result == null) return null;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error getting signle Mapping{ex}");
            throw ;
        }
    }

    public async Task<bool> Update(Guid sid, string status)
    {
        if(status==null || string.IsNullOrEmpty(status)) return false;
        var map= await _context.RRSyncProductMap.FirstOrDefaultAsync(rs => rs.SdataId==sid);
        if (map == null) return false;
        map.SyncStatus=status;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Delete(Guid sid)
    {
        var map= await _context.RRSyncProductMap.FirstOrDefaultAsync(rs => rs.SdataId==sid);
        if (map == null) return false;
        _context.RRSyncProductMap.Remove(map);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<List<Guid>> GiveLiveProductsForRRSync(List<Guid> sdataIds)
    {
        return await _context.RRSyncProductMap
            .AsNoTracking()
            .Where(m => m.SyncStatus == "Active" && sdataIds.Contains(m.SdataId)&& m.Sdata.Status!="Live")
            .Select(m => m.SdataId)
            .ToListAsync();
    }
}