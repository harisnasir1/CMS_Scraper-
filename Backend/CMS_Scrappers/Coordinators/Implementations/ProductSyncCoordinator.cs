using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Implementations;
using CMS_Scrappers.Utils;
using CMS_Scrappers.Models;

namespace CMS_Scrappers.Coordinators.Implementations;

public class ProductSyncCoordinator:IProductSyncCoordinator
{
    private readonly IProductStoreMappingRepository _productStoreMappingRepository;
    private readonly IShopifyRepository _shopifyRepository;
    private readonly ILogger<ShopifyService> _logger;


    public ProductSyncCoordinator(
        IProductStoreMappingRepository storemaprepository,
        IShopifyRepository shopifyrepository,
        ILogger<ShopifyService> logger
        )
    {
        _logger=logger;
        _shopifyRepository = shopifyrepository;
        _productStoreMappingRepository = storemaprepository;
    }

    public async Task<bool> pushProductslive(Sdata data)
    {
        var stores = await this.GetallStoresconfigs();
        foreach (var store in stores)
        {
            var _shopifyservice = this.GetShopifyService(store);

           var res=await _shopifyservice.PushProductAsync(data);
           if(res == null) return false;
           var mapping = new ProductStoreMapping
           {
               Id = Guid.NewGuid(),
               ProductId = data.Id,
               ShopifyStoreId = store.Id,
               ExternalProductId = res,  //shopify id comming from shopify after insertion.
               SyncStatus = "Live",
               LastSyncedAt = data.UpdatedAt,
               CreatedAt = DateTime.UtcNow,
               UpdatedAt = DateTime.UtcNow
           };
           try
           {
               await _productStoreMappingRepository.InsertProductmapping(mapping);
           }
           catch (Exception e)
           {
               return false;
           }
        }

        return false;
    }

    private ShopifyService GetShopifyService(Shopify store)
    {
        var storesettings = this.Configtosettings(store);
        var shopifyservice=new ShopifyService(storesettings,_logger);
        return shopifyservice;
    }

    private ShopifySettings Configtosettings(Shopify store)
    {
        return new ShopifySettings
        {
            SHOPIFY_STORE_NAME = store.ShopName,
            SHOPIFY_ACCESS_TOKEN = store.AdminApiAccessToken,
            SHOPIFY_API_KEY = store.ApiKey,
            SHOPIFY_API_SECRET = store.ApiSecretKey,
            SHOPIFY_STORE_DOMAIN = store.HostName
        };
    }

    private async Task <List<Shopify>>GetallStoresconfigs()
    {
        try
        {
            return await _shopifyRepository.GiveallStoresToSync();
        }
        catch (Exception e)
        {
            Console.WriteLine("error getting all stores to sync",e);
            throw;
        }
    }
    
}