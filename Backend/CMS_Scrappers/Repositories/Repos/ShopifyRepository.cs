using CMS_Scrappers.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Repositories.Repos;

public class ShopifyRepository:IShopifyRepository
{
    private readonly AppDbContext _context;

    public ShopifyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Shopify>> GiveallStoresToSync()
    {
        try
        {
            return await _context.Shopify.ToListAsync();
        }
        catch (Exception ex)
        {
            return new List<Shopify>();
        }
    }

    public async Task<Shopify?> GiveStoreById(Guid Storeid)
    {
        try
        {
            var shop = await _context.Shopify.Where(s => s.Id == Storeid).FirstOrDefaultAsync();
        
            if (shop == null) return null;

            var todayUtc = DateTime.UtcNow.Date;

            if (shop.LastVariantResetDate.Date < todayUtc)
            {
                shop.VariantsCreatedToday = 0;
                shop.LastVariantResetDate = todayUtc;
                await _context.SaveChangesAsync();
            }

            return shop;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting shopify store using id {ex}");
            return null;
        }
        
        
    }

    public async  Task<bool> LastSynced(Guid storeId, int variantadded )
    {
        try
        {
            var store= await _context.Shopify.Where(s => s.Id == storeId).FirstOrDefaultAsync();
            if (store == null) return false;
            store.VariantsCreatedToday += variantadded;
            store.LastSyncedOn=DateTime.Now.ToUniversalTime();
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting shopify store using id {ex}");
            return false;
        }
    }

}