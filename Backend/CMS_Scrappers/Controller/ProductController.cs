using CMS_Scrappers.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMS_Scrappers.Data.Responses.Api_responses;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CMS_Scrappers.BackgroundJobs.Interfaces;
namespace CMS_Scrappers.Controller
{   [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IHighPriorityTaskQueue _taskQueue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductController> _logger;
        private readonly IProducts _ProductSerivce;

        public ProductController(IHighPriorityTaskQueue taskQueue,IProducts service,IServiceProvider serviceProvider, ILogger<ProductController> logger)
        {
            _ProductSerivce=service;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _taskQueue = taskQueue;
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
            _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
            {
                _logger.LogInformation("shopify product pushing in process");
                using var scope = serviceProvider.CreateScope();
                var pservice = scope.ServiceProvider.GetService<IProducts>();
                await pservice.RemovingBackgroundimages(guid,Images);
                await pservice.PushProductShopify(guid);
                await pservice.UpdateStatus(guid, "Live");
                _logger.LogInformation("shopify product pushing ended");
            });
            return Ok(true);
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

    }

}
