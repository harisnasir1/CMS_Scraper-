using CMS_Scrappers.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMS_Scrappers.Data.Responses.Api_responses;
using Microsoft.AspNetCore.Authorization;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace CMS_Scrappers.Controller
{   [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProductController> _logger;
        private readonly IProducts _ProductSerivce;
        public ProductController(IProducts service,IServiceProvider serviceProvider, ILogger<ProductController> logger)
        {
            _ProductSerivce=service;
            _serviceProvider = serviceProvider;
            _logger = logger;
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

        [HttpPost("Similarimages")]
        public async Task<IActionResult> GetSimilarImg([FromBody] SimilarproductRequest request)
        {
            Guid id = new Guid(request.productid);
            ApiResponse<object> data = await _ProductSerivce.GetSimilarimages(id,request.page);
            if (data._Success == false) return BadRequest();

            return Ok(data.Data);
             
        }

        [HttpPost("Push")]
        public async Task<IActionResult> Submit([FromBody] SubmitRequest request )
        {
            Guid guid = new Guid(request.productid);
            await _ProductSerivce.RemovingBackgroundimages(guid);
            return Ok();
        }

    }

}
