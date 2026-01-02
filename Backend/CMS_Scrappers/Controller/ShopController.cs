using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Data.DTO;
namespace CMS_Scrappers.Controller
{
   // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController: ControllerBase
    {
        public readonly IProductSyncCoordinator _productSyncCoordinator;
        public readonly IShopifyRepository  _shopifyRepository;
        public readonly ISdataRepository _sdataRepository;
        public ShopController(IProductSyncCoordinator productSyncCoordinator,IShopifyRepository shopifyRepository,ISdataRepository sdataRepository)
        {
            _productSyncCoordinator=productSyncCoordinator;
            _shopifyRepository=shopifyRepository;
            _sdataRepository=sdataRepository;
        }

        [HttpGet("Get_Stores")]
        public async Task<IActionResult> getlivestoredata()
        {
            try
            {
               var stores= await _shopifyRepository.GiveallStoresToSync();
               var stores_details = new List<Get_all_store_response>();
               foreach (var store in stores)
               {
                   var c = await _sdataRepository.GiveBulkliveproductperstoreCount(store.Id);
                   stores_details.Add(
                 new Get_all_store_response  {
                       store_id=store.Id.ToString(),
                       store_name = store.ShopName,
                       store_out_of_sync=c
                   });
               }
                return Ok(stores_details);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Scraping failed", message = ex.Message });
            }
        }
    }

}