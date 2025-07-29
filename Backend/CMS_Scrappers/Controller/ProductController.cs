using CMS_Scrappers.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CMS_Scrappers.Data.Responses.Api_responses;
namespace CMS_Scrappers.Controller
{
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
        public async Task<IActionResult> Readytoreview([FromBody ] ReviewProductRequest request)
        {
            Guid id = new Guid(request.ScraperId);
            var data=await _ProductSerivce.Get_Ready_to_review_products(id,request.PageNumber,request.PageSize);
            return Ok(data);
        }
    }
}
