using System.Text.Json;
using System.Text;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using System.Xml.Linq;
using Amazon.S3.Model;

namespace CMS_Scrappers.Services.Implementations
{
    public class ShopifyService:IShopifyService
    {
        private readonly ShopifySettings _shopifySettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShopifyService> _logger;
        private string _locationId;
        public ShopifyService(ShopifySettings shopifysettings,ILogger<ShopifyService> logger) {
        
            _shopifySettings = shopifysettings;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", shopifysettings.SHOPIFY_ACCESS_TOKEN);
            _logger = logger;
            _locationId = "";
        }
        public async Task<string> PushProductAsync(Sdata sdata)
        {
            if (!string.IsNullOrEmpty(sdata.Shopifyid))
            {
                return null;
            }
            var product = new
            {
                product = new
                {
                    title = sdata.Title,
                    body_html = sdata.Description,
                    vendor = sdata.Brand,
                    product_type = sdata.ProductType,
                    template_suffix = sdata.ProductType== "Accessories" ? "single-size":"Default product",
                    tags = $" ALL PRODUCTS,{sdata.Brand}, {sdata.Gender},{sdata.ProductType},{sdata.Category},{sdata.Condition},Not in HQ",
                    options = new[]
                    {
                        new { name = "Size" }
                    },
                    variants = sdata.Variants.Select(v => new
                    {
                        option1 = v.Size,
                        price = Addmarkup(v.Price).ToString("F2"),
                        sku = v.SKU,
                        inventory_management = "shopify",
                        inventory_quantity = 1,
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
            _logger.LogInformation($"Starting metafield update for product {productId}");
         
            _logger.LogDebug($"Product data for {productId}: ScraperName='{sdata.ScraperName}', Condition='{sdata.Condition}', Category='{sdata.Category}', ProductType='{sdata.ProductType}', Gender='{sdata.Gender}'");
            
            var result = await RetryWithBackoff(async () => await PushMetafieldsInternal(sdata, productId), 
                maxRetries: 3, 
                baseDelay: 2000);
                
            if (result)
            {
                _logger.LogInformation($"Metafield update completed successfully for product {productId}");
            }
            else
            {
                _logger.LogError($"Metafield update failed for product {productId} after all retry attempts");
            }
            
            return result;
        }

        private async Task<bool> PushMetafieldsInternal(Sdata sdata, string productId)
        {
            try
            {
                var ownerId = $"gid://shopify/Product/{productId}";
                var metafieldInputs = new List<object>();

                
                if (IsValidMetafieldValue(sdata.ScraperName))
                {
                    metafieldInputs.Add(new { ownerId = ownerId, key = "scraper_origin", @namespace = "custom", value = sdata.ScraperName.Trim(), type = "single_line_text_field" });
                }

                if (IsValidMetafieldValue(sdata.Condition))
                {
                    metafieldInputs.Add(new { ownerId = ownerId, key = "product_condition", @namespace = "custom", value = sdata.Condition.Trim(), type = "single_line_text_field" });
                }

            
                metafieldInputs.Add(new { ownerId = ownerId, key = "age_group", @namespace = "custom", value = "Adult", type = "single_line_text_field" });

                if (IsValidMetafieldValue(sdata.Category))
                {
                    metafieldInputs.Add(new { ownerId = ownerId, key = "category", @namespace = "custom", value = sdata.Category.Trim(), type = "single_line_text_field" });
                }

                if (IsValidMetafieldValue(sdata.ProductType))
                {
                    metafieldInputs.Add(new { ownerId = ownerId, key = "product_type", @namespace = "custom", value = sdata.ProductType.Trim(), type = "single_line_text_field" });
                }

                var gender = GetGender(sdata.Gender);
                if (IsValidMetafieldValue(gender))
                {
                    metafieldInputs.Add(new { ownerId = ownerId, key = "gender", @namespace = "custom", value = gender, type = "single_line_text_field" });
                }

               
                if (!metafieldInputs.Any())
                {
                    _logger.LogWarning($"No valid metafields found for product {productId}");
                    return true;
                }

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

                _logger.LogInformation($"Setting {metafieldInputs.Count} metafields for product {productId}");

                var response = await _httpClient.PostAsync(
                    $"{_shopifySettings.SHOPIFY_STORE_DOMAIN}/admin/api/2025-07/graphql.json", 
                    content
                ).ConfigureAwait(false);

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

             
                if (doc.RootElement.TryGetProperty("data", out var data) && 
                    data.TryGetProperty("metafieldsSet", out var metafieldsSet) &&
                    metafieldsSet.TryGetProperty("userErrors", out var userErrors))
                {
                    var userErrorsArray = userErrors.EnumerateArray().ToList();
                    if (userErrorsArray.Any())
                    {
                        foreach (var error in userErrorsArray)
                        {
                            _logger.LogError($"Metafield user error: {error.ToString()}");
                        }
                        return false;
                    }
                }

                _logger.LogInformation($"Successfully set {metafieldInputs.Count} metafields for product {productId} via GraphQL.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while setting metafields for product {productId}");
                return false;
            }
        }

        private bool IsValidMetafieldValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogDebug($"Metafield value rejected: null or whitespace");
                return false;
            }
       
            var trimmedValue = value.Trim();
            if (trimmedValue.Length < 2)
            {
                _logger.LogDebug($"Metafield value rejected: too short (length: {trimmedValue.Length})");
                return false;
            }
                
          
            var lowerValue = trimmedValue.ToLowerInvariant();
            var emptyValues = new[] { "n/a", "na", "none", "null", "undefined", "unknown", "-", "--", "..." };
            
            if (emptyValues.Contains(lowerValue))
            {
                _logger.LogDebug($"Metafield value rejected: common empty value '{value}'");
                return false;
            }
                
            _logger.LogDebug($"Metafield value accepted: '{value}'");
            return true;
        }

        private async Task<bool> RetryWithBackoff(Func<Task<bool>> operation, int maxRetries = 3, int baseDelay = 1000)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await operation();
                    if (result)
                    {
                        return true;
                    }
                    
             
                    if (attempt == 1)
                    {
                        _logger.LogWarning("Metafield operation failed, not retrying as it's not a transient error");
                    }
                    return false;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    var delay = baseDelay * (int)Math.Pow(2, attempt - 1); 
                    _logger.LogWarning(ex, $"Metafield operation attempt {attempt} failed, retrying in {delay}ms");
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Metafield operation failed after {maxRetries} attempts");
                    return false;
                }
            }
            
            return false;
        }
        private string GetGender(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return string.Empty;
                
            var normalizedGender = s.Trim().ToLowerInvariant();
            
            switch (normalizedGender)
            {
                case "male":
                case "men":
                case "m":
                    return "Men";
                case "female":
                case "women":
                case "f":
                case "w":
                    return "Women";
                case "unisex":
                case "u":
                    return "Unisex";
                default:
                    _logger.LogWarning($"Unknown gender value: '{s}', skipping gender metafield");
                    return string.Empty;
            }
        }
     
        public async Task UpdateProduct(List<ShopifyFlatProduct> existingproduct, Dictionary<string, Sdata> sdata)
        {
            var locationId = await GetFirstLocationIdAsync();
           
            if (string.IsNullOrEmpty(locationId))
            {                
                _logger.LogError("Could not find a Shopify location to update inventory.");
                return;
            }
            decimal Batchsizes = 500;
            decimal totalproduct=existingproduct.Count();
            decimal Batchcount = Math.Ceiling(totalproduct/Batchsizes);

            for (int i = 0; i < Batchcount; i++)
            {
                _logger.LogInformation($"Processing batch {i + 1} of {Batchcount}...");
                var inventoryQuantities = new List<object>();
                int startIndex = i * (int)Batchsizes;
                int endIndex = Math.Min(startIndex + (int)Batchsizes, existingproduct.Count);
                var currentBatch = existingproduct.GetRange(startIndex, endIndex - startIndex);
                foreach (var incomingProduct in currentBatch)
                {
                   
                    if (sdata.TryGetValue(incomingProduct.ProductUrl, out var dbProduct))
                    {
                        string gid = $"gid://shopify/Product/{dbProduct.Shopifyid}";
                        Dictionary<string , string > variantInventoryMap = await GetVariantInventoryIdsAsync(gid);

                        foreach(var incomingvariant in incomingProduct.Variants)
                        {
                            if (variantInventoryMap.TryGetValue(incomingvariant.Size, out var inventoryItemId))
                            {
                                int newQuantity = incomingvariant.Available == 1 ? 1 : 0;
                                inventoryQuantities.Add(new
                                {
                                    inventoryItemId = inventoryItemId,
                                    locationId = locationId,
                                    quantity = newQuantity
                                });
                            }
                        }
                    }
                }
                if(inventoryQuantities.Count > 0)
                {
                    var mutation = @"
                mutation inventorySetOnHandQuantities($input: InventorySetOnHandQuantitiesInput!) {
                    inventorySetOnHandQuantities(input: $input) {
                        userErrors {
                            field
                            message
                        }
                        inventoryAdjustmentGroup {
                            id
                            reason
                        }
                    }
                }";
                    var variables = new
                    {
                        input = new
                        {
                            reason = "restock", 
                            setQuantities = inventoryQuantities
                        }
                    };
                    var payload = new { query = mutation, variables };

                    try
                    {
                        var data = await ExecuteGraphQLAsync(payload);
                        Console.WriteLine($"Batch {i + 1} updated successfully.");
                       
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating batch {i + 1}: {ex.Message}");
                    }
                }

            }
        }
        public async Task<Dictionary<string, string>> GetVariantInventoryIdsAsync(string productGid)
        {
            var query = @"
        query getProductVariants($id: ID!) {
            product(id: $id) {
                variants(first: 50) {
                    edges {
                        node {
                            id
                            selectedOptions {
                                name
                                value
                            }
                            inventoryItem {
                                id
                            }
                        }
                    }
                }
            }
        }";

            var variables = new { id = productGid };
            var payload = new { query, variables };
            var data = await ExecuteGraphQLAsync(payload);

            var variants = new Dictionary<string, string>();

            var productElement = data.GetProperty("product");
            if (productElement.ValueKind == JsonValueKind.Null)
            {
               
                _logger.LogWarning($"No product found for gid {productGid}");
                return variants;
            }
            var variantEdges = data.GetProperty("product").GetProperty("variants").GetProperty("edges");

            foreach (var edge in variantEdges.EnumerateArray())
            {
                var node = edge.GetProperty("node");
                var inventoryItemId = node.GetProperty("inventoryItem").GetProperty("id").GetString();

              
                string size = null;
                foreach (var option in node.GetProperty("selectedOptions").EnumerateArray())
                {
                    if (option.GetProperty("name").GetString().Equals("Size", StringComparison.OrdinalIgnoreCase))
                    {
                        size = option.GetProperty("value").GetString();
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(size) && !string.IsNullOrEmpty(inventoryItemId))
                {
                    variants[size] = inventoryItemId;
                }
            }

            return variants;
        }

        public async Task<string> GetFirstLocationIdAsync()
        {


            string query = "query { locations(first: 1) { edges { node { id } } } }";

            var payload = new { query };
            var data = await ExecuteGraphQLAsync(payload);

            _locationId = data.GetProperty("locations").GetProperty("edges")[0].GetProperty("node").GetProperty("id").GetString();
            return _locationId;
        }
        private async Task<JsonElement> ExecuteGraphQLAsync(object payload)
        {
            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_shopifySettings.SHOPIFY_STORE_DOMAIN}/admin/api/2024-07/graphql.json");
            request.Headers.Add("X-Shopify-Access-Token", _shopifySettings.SHOPIFY_ACCESS_TOKEN);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync();
            var jsonDoc = await JsonDocument.ParseAsync(responseStream);


            if (jsonDoc.RootElement.TryGetProperty("errors", out var errors))
            {
                throw new Exception($"GraphQL Error: {errors.ToString()}");
            }
            return jsonDoc.RootElement.GetProperty("data");
        }

        private double Addmarkup(decimal price)
        {
            double p = (float)price;
            if(price<=0)return 0;
            double markup = 0.1 * p;
            double ourprice = p + markup;
            double pound = (int)Math.Round(ourprice * 0.74);
            double k = (int)pound / 5;
            double converted = k * 5;            
            return converted;
        }
    }

}
