
    using CMS_Scrappers.Data.DTO;

    public interface ISdataRepository
    {
        Task Add(List<ShopifyFlatProduct> data, Guid _scraperId);
        Task Update(List<ShopifyFlatProduct> data);
        Task<Dictionary<string, Sdata>> Giveliveproduct(List<ShopifyFlatProduct> existingProducts);
        
        Task<Dictionary<string, Sdata>> Giveliveproductperstore(List<ShopifyFlatProduct> existingProducts , Guid storeid);

        Task<List<Sdata>> GiveBulkliveproductperstore(Guid storeid);
        Task<int> GiveBulkliveproductperstoreCount(Guid storeid);
        /// <summary>Live products of one supplier that are new to RRSync (above the first-push price) or changed since last sync.</summary>
        Task<Dictionary<string,Sdata>> GiveLiveDataToSync(Guid scraperId, decimal minFirstPushPrice);
        /// <summary>Variants of ONE supplier on a store that the scrape has not seen since the threshold.</summary>
        Task<List<StaleVariantInfo>> GiveStaleVariants(Guid shopifyStoreId, DateTime threshold, Guid scraperId);
        Task<List<StaleVariantInfo>> GetSourceDeletedVariantsForStore(Guid shopifyStoreId);
        Task  DelunseenData(Guid scraperId,DateTime threshold);
        /// <summary>How many of this supplier's products we hold that are not SourceDeleted. Used to spot a partial fetch.</summary>
        Task<int> CountActiveForScraper(Guid scraperId);
        
            
    }

