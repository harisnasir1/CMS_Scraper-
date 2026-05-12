using CMS_Scrappers.Data.DTO.RR_Sync_DTO;
using CMS_Scrappers.Data.Requests;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses.RRSyncResopnse;


namespace CMS_Scrappers.Services.Interfaces;

public interface IRRSyncService
{
    Task<RRApiResponse<Dictionary<string, string>>> PushNewProductBatchAsync(BulkCreateRRSyncProductRequest batch);
    Task<RRApiResponse<Dictionary<string, string>>>  PushNewVariantBatchAsync(string Syncproductid, List<CreateRRSyncVariantRequest> variantRequest );
    Task<RRApiResponse<List<RRSyncVarinatDTO>>> GetAllSyncVarinats(string syncproductid);
    Task<RRApiResponse<object>> UpdateVariantBatchAsync(List<UpdateRRSyncVariantRequest> variantRequest,string Syncproductid);
    Task<RRApiResponse<object>> DeleteVariantAsync(string variantId);
    Task<RRApiResponse<RRSyncSourceResponse>>GetSourceByname(string scrappername);
    decimal Addmarkup(decimal price);
    decimal ToGbp(decimal usd);
}