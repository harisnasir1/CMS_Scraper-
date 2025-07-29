using Microsoft.EntityFrameworkCore;

public class ScrapperRepository : IScrapperRepository
{
    private readonly AppDbContext _context;

    public ScrapperRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Startrun(string name)
    {
        try
        {
            var src = await _context.Scrappers
                .FirstOrDefaultAsync(u => u.Name == name);
            Console.WriteLine(name);
            if (src == null) return false;
            Console.WriteLine("getting the scrrapper data from db");
            src.Status = "Running";
            src.Lastrun = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Startrun: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> Stoprun(string timetake, string name)
    {
        try
        {
            var src = await _context.Scrappers
                .FirstOrDefaultAsync(u => u.Name == name);
                
            if (src == null) return false;
            
            src.Status = "active";
            src.Runtime = timetake;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Stoprun: {ex.Message}");
            return false;
        }
    }

    public async Task Storerrors(string name, string error)
    {
        try
        {
            var src = await _context.Scrappers
                .FirstOrDefaultAsync(u => u.Name == name);
                
            if (src != null)
            {
                src.Status = "error";
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Storerrors: {ex.Message}");
        }
    }

    public async Task <Guid> Giveidbyname(string name)
    {
        try
        {
            var src = await _context.Scrappers
                .FirstOrDefaultAsync(u => u.Name == name);

            if (src != null)
            {
                return src.ID;
            }

            return Guid.Empty ;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Storerrors: {ex.Message}");
            return Guid.Empty; 
        }
    }
}