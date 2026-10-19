using CMS_Scrappers.BackgroundJobs.Interfaces;
using CMS_Scrappers.Scraping;
using CMS_Scrappers.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses;
[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly ILowPriorityTaskQueue _taskQueue;
    
    private readonly IShopifyScrapperFact _shopifyscrapperfactory;
    
    private readonly IScrapperRepository _repository;
    private readonly IHighPriorityTaskQueue _scrapeQueue;
    private readonly IScrapeOrchestrator _orchestrator;
    private readonly ISupplierProfileProvider _profiles;

    public ScraperController(ILowPriorityTaskQueue taskQueue,IShopifyScrapperFact shopifyscrapperfactory,IScrapperRepository repository,
        IHighPriorityTaskQueue scrapeQueue, IScrapeOrchestrator orchestrator, ISupplierProfileProvider profiles)
    {
        _repository = repository;
        _shopifyscrapperfactory=shopifyscrapperfactory;
        _taskQueue=taskQueue;
        _scrapeQueue = scrapeQueue;
        _orchestrator = orchestrator;
        _profiles = profiles;
    }

    /// <summary>
    /// Runs one supplier's scrape now, through the same orchestrator the schedule uses.
    /// 404 unknown supplier, 409 already queued or running, 202 queued.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromQuery] string scraper)
    {
        if (string.IsNullOrWhiteSpace(scraper)) return BadRequest("scraper (Scrappers.Name) is required");

        var profile = await _profiles.GetByNameAsync(scraper);
        if (profile == null) return NotFound(new { message = $"No supplier named '{scraper}'" });

        if (!_orchestrator.TryReserve(profile.Name))
            return Conflict(new { message = $"{profile.Name} is already queued or running" });

        _scrapeQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>
        {
            await serviceProvider.GetRequiredService<IScrapeOrchestrator>().RunOneAsync(profile.Name, token);
        });

        return Accepted(new { message = "Scrape queued", scraper = profile.Name, enabled = profile.Enabled });
    }

    [HttpGet("running")]
    public IActionResult Running() => Ok(_orchestrator.Running);

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok("Scraper is ready");
    }

    //[HttpGet("test-scrape")]
    //public async Task<IActionResult> TestScrape()
    //{
    //    try
    //    {
    //         _taskQueue.QueueBackgroundWorkItem(async (serviceProvider, token) =>{
    //            using var scope = serviceProvider.CreateScope();
    //            var Shopifyscrfactory=scope.ServiceProvider.GetRequiredService<IShopifyScrapperFact>();
    //            var scraper=Shopifyscrfactory.CreateScraper("savonches");
    //            await scraper.ScrapeAsync();
    //         });
//
    //        return Ok("Scraping started - check console for results");
    //    }
    //    catch (Exception ex)
    //    {
    //        return StatusCode(500, new { error = "Scraping failed", message = ex.Message });
    //    }
    //}

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