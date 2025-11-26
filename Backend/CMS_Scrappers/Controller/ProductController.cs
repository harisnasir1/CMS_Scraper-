using CMS_Scrappers.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMS_Scrappers.Data.Responses.Api_responses;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CMS_Scrappers.BackgroundJobs.Interfaces;
namespace CMS_Scrappers.Controller
{   //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IHighPriorityTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductController> _logger;
        private readonly IProducts _ProductSerivce;
        private readonly IScrapperRepository _scrapperRepository;
        public ProductController(IHighPriorityTaskQueue taskQueue,IProducts service,IServiceProvider serviceProvider,IScrapperRepository scrapperRepository, ILogger<ProductController> logger)
        {
            _ProductSerivce=service;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _taskQueue = taskQueue;
            _scrapperRepository = scrapperRepository;
        }
        
        [HttpPost("Readytoreview")]
        public async Task<IActionResult> Readytoreview([FromBody ] ReviewProductRequest request) //bad naming here it is actually for product for each scraper
        {
            Guid id = new Guid(request.ScraperId);
            var data=await _ProductSerivce.Get_Ready_to_review_products(id,request.PageNumber,request.PageSize);
            return Ok(data);
        }

        [HttpPost("pendingreview")]
        public async Task<IActionResult> PendingReview([FromBody] ReviewProductRequest request)
        {
            var data=await _ProductSerivce.pendingReviewproducts(request.PageNumber,request.PageSize);
            return Ok(data);
        }
        [HttpPost("Livefeed")]
        public async Task<IActionResult> Livefeed([FromBody] ReviewProductRequest request)
        {
            var data = await _ProductSerivce.Livefeedproducts(request.PageNumber, request.PageSize);
            return Ok(data);
        }

        [HttpPost("Similarimages")]
        public async Task<IActionResult> GetSimilarImg([FromBody] SimilarproductRequest request)
        {
            Guid id = new Guid(request.productid);
            ApiResponse<object> data = await _ProductSerivce.GetSimilarimages(id,request.page);
            if (data._Success == false) return BadRequest();
            return Ok(data.Data);   
        }

        [HttpPost("Push")]
        public async Task<IActionResult> Submit([FromBody] PushRequest request )
        {
            var guid= new Guid(request.id);
           
           
            var Images = request.productimage;
            
            
            var update=await _ProductSerivce.UpdateStatus(guid, "Shopify Queued");
            if (!update)
            {
                return BadRequest(new { message = "Failed to update product status" });
            }
            
            _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
            {
                _logger.LogInformation($"shopify product pushing in process for product {guid}");
                using var scope = serviceProvider.CreateScope();
                var pservice = scope.ServiceProvider.GetService<IProducts>();               
                try
                {
                   var k1= await pservice.UpdateStatus(guid, "Processing");
                    if(!k1) {
                        throw new Exception("Error in updating status");
                        }
                    await pservice.RemovingBackgroundimages(guid,Images);
                   var k2= await pservice.PushProductShopify(guid);
                    if (!k2)
                    {
                        throw new Exception("Error in pusing shopify order");
                    }
                  var k3=  await pservice.UpdateStatus(guid, "Live");
                    if (!k3)
                    {
                        throw new Exception("Error in updating status");
                    }
                    _logger.LogInformation($"shopify product pushing completed successfully for product {guid}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing product {guid}");
                }
            
            });
            return Ok(new { message = "Product queued for processing", productId = guid });
        }

        [HttpPost("AiDescription")]
        public async Task<IActionResult> GenerativeDescription([FromBody] SubmitRequest request)
        {
            Guid guid = new Guid(request.productid);
            string d=await _ProductSerivce.AIGeneratedDescription(guid);
            return Ok(d);
        }
        [HttpPost("SaveDetails")]
        public async Task<IActionResult> UpdateDetails([FromBody] UpdateDetails request)
        {
            Guid guid = new Guid(request.productid);
            bool k= await _ProductSerivce.UpdateProductDetails(guid,request.sku,request.title,request.description,request.price);
            if (!k) return Ok(false);
            return Ok(true);
        }
        [HttpPost("GetCount")]
        public async Task<IActionResult> GetProductCount([FromBody] CountRequest request)
        {
            int re=await _ProductSerivce.ProductCountStatus(request.status);
            return Ok(re);
        }
        [HttpPost("GetStatus")]
        public async Task <IActionResult> GetStatus([FromBody] SubmitRequest request)
        {
            
            var id = new Guid(request.productid);
            var data = await _ProductSerivce.GetProductStatus(id);
            if(data==null || data== "Unknown") return BadRequest();
            return Ok(data);
        }
        [HttpPost("Sync_inventory")]
        public async Task <IActionResult> Sync_inventory([FromBody] ReviewProductRequest request)
        {
            // Validate request object
            if (request == null || string.IsNullOrEmpty(request.ScraperId))
                return BadRequest("Invalid request body.");

            Guid id;

            // Validate GUID
            if (!Guid.TryParse(request.ScraperId, out id))
                return BadRequest("Invalid ScraperId format.");

            int? limit = request.PageSize;

            var status = await _scrapperRepository.Get_Status_by_id(id);

            // Check scraper status
            if (string.IsNullOrEmpty(status) || status != "active")
                return BadRequest("Scraper is not active.");

            await _ProductSerivce.PushAllScraperProductsLive(id, limit);

            return Ok("ok");
        }

        
    }

}
