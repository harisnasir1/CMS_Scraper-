using Microsoft.AspNetCore.Mvc;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IBackgroundTaskQueue _taskQueue;
    
    private readonly IShopifyScrapperFact _shopifyscrapperfactory;

  
    

    public ScraperController(IBackgroundTaskQueue taskQueue,IShopifyScrapperFact shopifyscrapperfactory)
    {
  
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
}