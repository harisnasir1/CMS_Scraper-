using System.Text.Json;
using System.Text;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;

namespace CMS_Scrappers.Services.Implementations
{
    public class ShopifyService:IShopifyService
    {
        private readonly ShopifySettings _shopifySettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShopifyService> _logger;
        public ShopifyService(ShopifySettings shopifysettings,ILogger<ShopifyService> logger) {
        
            _shopifySettings = shopifysettings;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", shopifysettings.SHOPIFY_ACCESS_TOKEN);
            _logger = logger;

        }
        public async Task<string> PushProductAsync(Sdata sdata)
        {
            var product = new
            {
                product = new
                {
                    title = sdata.Title,
                    body_html = sdata.Description,
                    vendor = sdata.Brand,
                    product_type = sdata.ProductType,
                    tags = $" 'ALL PRODUCTS',{sdata.Brand}, {sdata.Gender},{sdata.ProductType},{sdata.Category},{sdata.Condition},'Not in HQ'",
                    variants = sdata.Variants.Select(v => new
                    {
                        option1 = v.Size,
                        price = v.Price.ToString("F2"),
                        sku = v.SKU,
                        inventory_management = "shopify",
                        inventory_quantity = v.InStock ? 1 : 0,
                        requires_shipping = true
                    }),
                    images = sdata.Image.Select(i => new { src = i.Url })
                }
            };
            var json = JsonSerializer.Serialize(product);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_shopifySettings.SHOPIFY_STORE_DOMAIN}/admin/api/2023-07/products.json", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Shopify error: " + error);
                return "";
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            var shopifyProductId = root.GetProperty("product").GetProperty("id").GetInt64();
            await pushmetafields(sdata, shopifyProductId.ToString());
            return shopifyProductId.ToString();
        }


        public async Task<bool> pushmetafields(Sdata sdata, string productId)
        {
           
            var ownerId = $"gid://shopify/Product/{productId}";

           
            var metafieldInputs = new[]
            {
        new { ownerId = ownerId, key = "scraper_origin", @namespace = "custom", value = sdata.ScraperName, type = "single_line_text_field" },
        new { ownerId = ownerId, key = "product_condition", @namespace = "custom", value = sdata.Condition, type = "single_line_text_field" },
        new { ownerId = ownerId, key = "age_group", @namespace = "custom", value = "Adult", type = "single_line_text_field" },
        new { ownerId = ownerId, key = "category", @namespace = "custom", value = sdata.Category, type = "single_line_text_field" },
        new { ownerId = ownerId, key = "product_type", @namespace = "custom", value = sdata.ProductType, type = "single_line_text_field" },
        new { ownerId = ownerId, key = "gender", @namespace = "custom", value =GetGender(sdata.Gender), type = "single_line_text_field" },
       
            };

            
            var mutation = @"
        mutation setMetafields($metafields: [MetafieldsSetInput!]!) {
          metafieldsSet(metafields: $metafields) {
            metafields {
              id
              key
            }
            userErrors {
              field
              message
            }
          }
        }";

          
            var payload = new
            {
                query = mutation,
                variables = new { metafields = metafieldInputs }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

          
            var response = await _httpClient.PostAsync(
                $"{_shopifySettings.SHOPIFY_STORE_DOMAIN}/admin/api/2025-07/graphql.json", 
                content
            ).ConfigureAwait(false);

            _logger.LogError(response.ToString());
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GraphQL request failed with status {response.StatusCode}. Response: {responseBody}");
                return false;
            }

          
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                _logger.LogError($"GraphQL operation failed: {errors.ToString()}");
                return false;
            }

            _logger.LogInformation($"Successfully set metafields for product {productId} via GraphQL.");
            return true;
        }
        private string GetGender(string s)
        {
            if(s == "Male")
            {
                return "Men";
            }
            else if(s=="Unisex")
            {
                return "Unisex";
            }
            else if(s=="Female")
            {
                return "Women";
            }
            else
            {
                return "";
            }
        }
    }

}
