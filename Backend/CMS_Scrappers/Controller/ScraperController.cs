using Microsoft.AspNetCore.Mvc;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IBackgroundTaskQueue _taskQueue;
    
    private readonly IShopifyScrapperFact _shopifyscrapperfactory;
    
    private readonly IScrapperRepository _repository;
  
    

    public ScraperController(IBackgroundTaskQueue taskQueue,IShopifyScrapperFact shopifyscrapperfactory,IScrapperRepository repository)
    {
        _repository = repository;
        _shopifyscrapperfactory=shopifyscrapperfactory;
        _taskQueue=taskQueue;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("Scraper is ready");
    }

    [HttpGet("test-scrape")]
    public async Task<IActionResult> TestScrape()
    {
        try
        {
             _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>{
                using var scope = serviceProvider.CreateScope();
                var Shopifyscrfactory=scope.ServiceProvider.GetRequiredService<IShopifyScrapperFact>();
                var scraper=Shopifyscrfactory.CreateScraper("savonches");
                await scraper.ScrapeAsync();
             });

            return Ok("Scraping started - check console for results");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Scraping failed", message = ex.Message });
        }
    }

    [HttpGet("Getallscrapers")]

    public async Task <IActionResult> GetScrapers()
    {
        try
        {
            var result=await _repository.Getallscrapers();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get Scrapers", message = ex.Message });
        }
    }

}