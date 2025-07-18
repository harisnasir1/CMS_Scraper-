using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;
[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
       
        return Ok("Scraper is ready");
    }
}