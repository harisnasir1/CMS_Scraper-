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
        
            return await _context.Shopify.Where(s=>s.Id==Storeid).FirstOrDefaultAsync();
        

    }

}