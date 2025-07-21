using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IScrappers _scraper;
    private readonly IBackgroundTaskQueue _taskQueue;

    public ScraperController(IScrappers scraper,IBackgroundTaskQueue taskQueue)
    {
        _scraper = scraper;
        
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
                var scrapp=scope.ServiceProvider.GetRequiredService<IScrappers>();
                await scrapp.ScrapeAsync(); 
             });

            return Ok("Scraping started - check console for results");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Scraping failed", message = ex.Message });
        }
    }
}