using CMS_Scrappers.Coordinators.Interfaces;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS_Scrappers.Controller
{
    // Development-only helpers for stepping through sync code against a real environment
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _context;
        private readonly IProductSyncCoordinator _productSyncCoordinator;
        private readonly ILogger<DebugController> _logger;
        private readonly ICatalogDiscoveryService _catalogDiscovery;

        public DebugController(IWebHostEnvironment env, AppDbContext context, IProductSyncCoordinator productSyncCoordinator, ILogger<DebugController> logger, ICatalogDiscoveryService catalogDiscovery)
        {
            _env = env;
            _context = context;
            _productSyncCoordinator = productSyncCoordinator;
            _logger = logger;
            _catalogDiscovery = catalogDiscovery;
        }

        // Dumps what a supplier's products.json contains (product types, tags, option values, prices)
        // so the category and size mapping can be built before the scraper is enabled. Read-only.
        // Example: GET api/Debug/catalog-summary?scraper=Savonches
        [HttpGet("catalog-summary")]
        public async Task<IActionResult> CatalogSummary([FromQuery] string scraper, [FromQuery] int? maxPages = null)
        {
            if (!_env.IsDevelopment()) return NotFound();
            if (string.IsNullOrWhiteSpace(scraper)) return BadRequest("scraper (Scrappers.Name) is required");

            try
            {
                var summary = await _catalogDiscovery.SummarizeAsync(scraper, maxPages);
                return Ok(summary);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class SyncStockRequest
        {
            public List<Guid> SdataIds { get; set; } = new();
        }

        // Pushes the DB's stock state for the given products through the normal UpdateProduct path (all mapped stores)
        [HttpPost("sync-stock")]
        public async Task<IActionResult> SyncStock([FromBody] SyncStockRequest request)
        {
            if (!_env.IsDevelopment()) return NotFound();
            if (request.SdataIds.Count == 0 || request.SdataIds.Count > 20)
                return BadRequest("Send between 1 and 20 SdataIds");

            var products = await _context.Sdata
                .AsNoTracking()
                .Include(s => s.Variants)
                .Where(s => request.SdataIds.Contains(s.Id))
                .ToListAsync();

            var flat = products.Select(p => new ShopifyFlatProduct
            {
                Title = p.Title,
                ProductUrl = p.ProductUrl,
                Status = p.Status,
                New = false,
                Variants = p.Variants.Select(v => new ProductVariantRecordDTO
                {
                    Id = v.Id,
                    Size = v.Size,
                    SKU = v.SKU,
                    Price = v.Price,
                    Available = v.InStock ? 1 : 0
                }).ToList()
            }).ToList();

            // UpdateProduct_Coordinator only touches products with Status == "Live" and a store mapping
            var summary = products.Select(p => new
            {
                p.Id,
                p.Title,
                p.Status,
                WillSync = p.Status == "Live",
                Variants = p.Variants.Select(v => new { v.Size, v.InStock })
            }).ToList();

            _logger.LogInformation($"Debug sync-stock for {flat.Count} products");
            await _productSyncCoordinator.UpdateProduct_Coordinator(flat);

            return Ok(new
            {
                found = products.Count,
                missing = request.SdataIds.Except(products.Select(p => p.Id)),
                products = summary
            });
        }

        // Compares Shopify's real stock with the supplier stock and pushes the difference.
        // dryRun defaults to true so a stray call only reports; pass dryRun=false to apply,
        // and force=true to bypass the mass-zero-out guard on the first repair run.
        [HttpPost("reconcile-stock")]
        public async Task<IActionResult> ReconcileStock(
            [FromServices] IStockReconciler reconciler,
            [FromServices] IShopifyRepository shopifyRepository,
            [FromQuery] Guid? storeId,
            [FromQuery] bool dryRun = true,
            [FromQuery] bool force = false)
        {
            if (!_env.IsDevelopment()) return NotFound();

            if (storeId == null)
            {
                return Ok(await reconciler.ReconcileAllStoresAsync(dryRun, force));
            }

            var store = (await shopifyRepository.GiveallStoresToSync()).FirstOrDefault(s => s.Id == storeId);
            if (store == null) return NotFound($"No store with id {storeId}");
            return Ok(await reconciler.ReconcileStoreAsync(store, dryRun, force));
        }
    }
}
