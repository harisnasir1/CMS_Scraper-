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
            await _context.RRSyncProductMaps.AddAsync(data);
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
            var result = await _context.RRSyncProductMaps.ToListAsync();
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
            var result = await _context.RRSyncProductMaps.Include(p=>p.VariantMaps).FirstOrDefaultAsync(rs => rs.SyncStatus == "Active" && rs.SdataId==sid);
            if (result == null) 
                return new RRSyncProductMap();
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error getting signle Mapping{ex}");
            throw ;
        }
    }

    public Task<bool> Update(Guid sid, string status)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Delete(Guid sid)
    {
        throw new NotImplementedException();
    }
}