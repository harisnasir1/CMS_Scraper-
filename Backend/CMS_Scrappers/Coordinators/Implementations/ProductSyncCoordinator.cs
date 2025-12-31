using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Implementations;
using CMS_Scrappers.Utils;
using CMS_Scrappers.Models;
using CMS_Scrappers.Services.Interfaces;

namespace CMS_Scrappers.Coordinators.Implementations;

public class ProductSyncCoordinator:IProductSyncCoordinator
{
    private readonly IProductStoreMappingRepository _productStoreMappingRepository;
    private readonly IShopifyRepository _shopifyRepository;
    private readonly ILogger<ShopifyService> _logger;
    private readonly ISdataRepository _sdataRepository;
    private readonly IFileReadWrite _readWrite;
    public ProductSyncCoordinator(
        IProductStoreMappingRepository storemaprepository,
        IShopifyRepository shopifyrepository,
        ILogger<ShopifyService> logger,
        ISdataRepository sdataRepository,
        IFileReadWrite readWrite
        )
    {
        _sdataRepository = sdataRepository;
        _logger=logger;
        _shopifyRepository = shopifyrepository;
        _productStoreMappingRepository = storemaprepository;
        _readWrite = readWrite;
    }

    public async Task<bool> pushProductslive(Sdata data)
    {
        var stores = await this.GetallStoresconfigs();
        foreach (var store in stores)
        {
            
            var _shopifyservice = this.GetShopifyService(store);
            var valid=await Isthisproductlive(data.Id,store.Id);
            if (!valid) return false;  //product is already live wtf its doing in review stage
            
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

    public async Task UpdateProduct_Coordinator(List<ShopifyFlatProduct> existingproduct)
    {
        try
        {
            
            var stores = await this.GetallStoresconfigs();
            foreach (var store in stores)
            {
                try
                {
                    var sdata = await _sdataRepository.Giveliveproductperstore(existingproduct, store.Id);
                    var _shopifyservice = this.GetShopifyService(store);
                    //we need to get live product per store not all the stores have all the same 
                
                    await _shopifyservice.UpdateProduct(existingproduct, sdata);
                    //for now but it's good to have this as this var is reusing there should be no problem regarding memory issue.
                
                    _logger.LogInformation($"Shopify update completed successfully for {store.ShopName}" ); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"✗ Update FAILED for {store.ShopName}: {ex.Message}");
                }
              
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> BulkSyncLiveProduct(Guid store)
    {
        try
        {
            var shop = await _shopifyRepository.GiveStoreById(store);
            if(shop == null) return false;
            var data = await _sdataRepository.GiveBulkliveproductperstore(shop.Id);
            var _shopifyservice = GetShopifyService(shop);
            var lookup_id_line_map=
            await _shopifyservice.Bulk_mutation_shopify_product_creation(data, shop.ShopName);
            _logger.LogInformation($"Synced {data.Count} to {shop.ShopName}");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("error getting all stores to sync",e);
            return false;
        }
    }

   
    
    

    private ShopifyService GetShopifyService(Shopify store)
    {
        var storesettings = this.Configtosettings(store);
        var shopifyservice=new ShopifyService(storesettings,_logger,_productStoreMappingRepository,_readWrite);
        return shopifyservice;
    }

    private ShopifySettings Configtosettings(Shopify store)
    {
        return new ShopifySettings
        {
            SHOPIFY_STORE_ID = store.Id,
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
    
    private async Task<bool> Isthisproductlive(Guid id, Guid storeid)
    {
       var k= await _productStoreMappingRepository.GetSyncIdBySidAndStoreId(id, storeid);

       return String.IsNullOrEmpty(k);
    }
}