
using CMS_Scrappers.BackgroundJobs.Interfaces;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using CMS_Scrappers.Coordinators.Interfaces;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
public class Shopify_Scrapper_factory:IShopifyScrapperFact{

        

       private ShoipfyScrapper _shopifyclient;

        private readonly ILogger<ShopifyStoreScraper> _looger;

        private readonly IScrapperRepository _scrapperRepository;

        private readonly IServiceProvider _serviceProvider;
        
        private readonly ISdataRepository _sdataRepository;

        private readonly IUpdateShopifyTaskQueue _updateShopifyTaskQueue;
        private readonly IProductSyncCoordinator _productSyncCoordinator;

        public Shopify_Scrapper_factory (
           
            ShoipfyScrapper shoipfyScrapper
            ,ILogger<ShopifyStoreScraper> logger
            ,IScrapperRepository scrapperRepository,
            IServiceProvider serviceProvider,
           ISdataRepository sdataRepository,
            IUpdateShopifyTaskQueue updateShopifyTaskQueue,
            IProductSyncCoordinator productSyncCoordinator
        )
        {
         
            _shopifyclient=shoipfyScrapper;
            _looger=logger;
            _scrapperRepository=scrapperRepository;
            _serviceProvider=serviceProvider;
            _sdataRepository=sdataRepository;
             _updateShopifyTaskQueue=updateShopifyTaskQueue;
             _productSyncCoordinator = productSyncCoordinator;
        }

        public ShopifyStoreScraper CreateScraper(string StoreName){
           IShopifyParsingStrategy strategy;
         
           string baseurl;

           switch(StoreName.ToLowerInvariant())
           {
            case "savonches":
                 strategy= _serviceProvider.GetRequiredService<SavonchesStrategy>();
                 baseurl = "https://savonches.com/";
                 break;
             default:
                throw new NotSupportedException($"Store '{StoreName}' is not supported.");
           }
            return new ShopifyStoreScraper(StoreName, baseurl, _looger, _shopifyclient, strategy, _scrapperRepository,_serviceProvider,_sdataRepository,_updateShopifyTaskQueue,_productSyncCoordinator);
        }
}