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
        private IFileReadWrite _readWrite;
        public ShopifyService(ShopifySettings shopifysettings,ILogger<ShopifyService> logger,IFileReadWrite readWrite) {
            _shopifySettings = shopifysettings;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", shopifysettings.SHOPIFY_ACCESS_TOKEN);
            _logger = logger;
            _locationId = "";
            _readWrite = readWrite;
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
                    tags = $" ALL PRODUCTS,{sdata.Brand}, {sdata.Gender},{sdata.ProductType},{sdata.Category},{sdata.Condition},Not in HQ,{(sdata.Condition == "Pre-Owned" ? "PRELOVED":"")}",
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
                        inventory_quantity = v.InStock?1:0,
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
                if (IsValidMetafieldValue(sdata?.ConditionGrade))
                {
                    var condi_grade=Map_Condition_Grade(sdata.ConditionGrade.Trim());
                    if(condi_grade!="")
                    {
                        metafieldInputs.Add(new
                        {
                            ownerId = ownerId, key = "product_condition_grade_preloved", @namespace = "custom",
                            value = condi_grade.Trim(), type = "single_line_text_field"
                        });
                    }
                }

                if (IsValidMetafieldValue(sdata.Condition))
                {
                    string condition = "";
                    if (sdata.Condition == "Pre-Owned")
                    {
                        condition = "Preloved";
                    }
                    else
                    {
                        condition = "New";
                    }
                    metafieldInputs.Add(new { ownerId = ownerId, key = "product_condition", @namespace = "custom", value = condition.Trim(), type = "single_line_text_field" });
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
                var priceUpdate = new List<object>();
                int startIndex = i * (int)Batchsizes;
                int endIndex = Math.Min(startIndex + (int)Batchsizes, existingproduct.Count);
                var currentBatch = existingproduct.GetRange(startIndex, endIndex - startIndex);
                foreach (var incomingProduct in currentBatch)
                {
                    if (sdata.TryGetValue(incomingProduct.ProductUrl, out var dbProduct))
                    {
                        string gid = $"gid://shopify/Product/{dbProduct.Shopifyid}";
                        Dictionary<string , (string variantId, string inventoryId) > variantInventoryMap = await GetVariantInventoryIdsAsync(gid);
                        List<ProductVariantRecord> db_current_variant=dbProduct.Variants;
                        foreach(var incomingvariant in incomingProduct.Variants)
                        {
                            if (variantInventoryMap.TryGetValue(incomingvariant.Size, out var details))
                            {
                                int newQuantity = incomingvariant.Available == 1 ? 1 : 0;
                                inventoryQuantities.Add(new
                                {
                                    inventoryItemId = details.inventoryId,
                                    locationId = locationId,
                                    quantity = newQuantity
                                });
                                //get the db current variant first.
                                 var db_c_variant=Get_Current_db_variant(db_current_variant,incomingvariant.Size);
                                //check if db price is changed from comming variant price then update the price.
                                //well we can not check if price get change because we updated price beofre this step.
                                //at it is very long to go back so just check fi they are in stock then update the price.
                                if (db_c_variant!=null&&incomingvariant.Price > 0&&db_c_variant.InStock)
                                {
                                    priceUpdate.Add(new
                                    {
                                        productId = gid,
                                        id = details.variantId,
                                        price =  Addmarkup( incomingvariant.Price).ToString("F2"),
                                    });
                                }
                            }
                        }
                    }
                }
                if(inventoryQuantities.Count > 0)
                {
                   await Update_Variant_Quantites_batch(inventoryQuantities, i);
                }
                if(priceUpdate.Count > 0)
                {
                    await UpdatePricesBatch(priceUpdate, i);
                }

            }
        }
        
        
        private async Task<Dictionary<string, (string variantId, string inventoryId)>> GetVariantInventoryIdsAsync(string productGid)
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

            var variants = new Dictionary<string, (string,string)>();

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
                var variantId = node.GetProperty("id").GetString();
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

                if (!string.IsNullOrEmpty(size) && !string.IsNullOrEmpty(inventoryItemId)&&!string.IsNullOrEmpty(variantId))
                {
                    variants[size] = (variantId,inventoryItemId);
                }
            }

            return variants;
        }

        
        private async Task<string> GetFirstLocationIdAsync()
        {
            var query = "query { locations(first: 1) { edges { node { id } } } }";

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

        private async Task UpdatePricesBatch(List<object> priceUpdates, int batchIndex)
        {
            // Group by productId
            var groupedByProduct = priceUpdates
                .Cast<dynamic>()
                .GroupBy(v => (string)v.productId)
                .ToList();
    
            foreach (var productGroup in groupedByProduct)
            {
                var mutation = @"
            mutation productVariantsBulkUpdate($productId: ID!, $variants: [ProductVariantsBulkInput!]!) {
                productVariantsBulkUpdate(productId: $productId, variants: $variants) {
                    product {
                        id
                    }
                    productVariants {
                        id
                        price
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

                // Transform to only include id and price (remove productId)
                var variantsForMutation = productGroup.Select(v => new 
                {
                    id = v.id,
                    price = v.price
                }).ToList();

                var variables = new
                {
                    productId = productGroup.Key,
                    variants = variantsForMutation
                };

                var payload = new { query = mutation, variables };

                try
                {
                  
                    var response = await ExecuteGraphQLAsync(payload);

                    // handle errors
                    if (response.TryGetProperty("productVariantsBulkUpdate", out var result) &&
                        result.TryGetProperty("userErrors", out var errors))
                    {
                        foreach (var e in errors.EnumerateArray())
                        {
                            _logger.LogError($"Bulk error: {e.GetProperty("message").GetString()}");
                        }
                    }

                    _logger.LogInformation($"Updated {variables.variants.Count} variants for product {variables.productId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error updating prices for product {productGroup.Key}: {ex.Message}");
                }
            }
            
        }
        
        
        private async Task  Update_Variant_Quantites_batch(List<object>inventoryQuantities,int i)
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
                Console.WriteLine($"Batch {i + 1} updated Quantity successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating inventory batch {i + 1}: {ex.Message}");
                throw; 
            }
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

        private ProductVariantRecord? Get_Current_db_variant(List<ProductVariantRecord>? dbVariants, string? size)
        {
            
            if (string.IsNullOrEmpty(size))
                return null;

            
            if (dbVariants == null || dbVariants.Count == 0)
                return null;

            
            return dbVariants.Find(v => v.Size == size);
        }


        private string Map_Condition_Grade(string condition)
        {
            var gradeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Used Condition.", "worn condition" },
                { "Good Condition.", "good condition" },
                { "Great Condition.", "excellent condition" },
                { "Like New Condition.", "like new" }
            };

            if (string.IsNullOrWhiteSpace(condition))
                return string.Empty;

            return gradeMap.TryGetValue(condition.Trim(), out var mappedValue) 
                ? mappedValue 
                : ""; 
        }
        
        ///for bulk operations
        ///
        /// we are gonna take from below onwards
        
        public async Task<bool> Bulk_mutation_shopify_product_creation(List<Sdata> data,string name)
        {
            //var jonldata = PrepareProductInputForGraphQL(data);
            //await this._readWrite.Wrtie_data(jonldata, name);
           
            try
            {
               var stagedurl= await Stage_uploads_Create(name);
                //string path = $"/home/haris/Projects/office/CMS/Backend/CMS_Scrappers/JSONL_files/{name}";
                //var mutation = Prepare_Bulk_Creation_Mutation(path);
                //var payload = new { query=mutation };
                //var re=await ExecuteGraphQLAsync(payload);
                //var url = await bulk_query_pulling();
                //var subdata = await Retrieve_bulkResults_data(url);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
          
        }
        
        private List<object> PrepareProductInputForGraphQL(List<Sdata> data)
        {
            List<object> shopifydata = new List<object>();
            foreach (var sdata in data)
            {
                // Prepare metafields
                var metafields = new List<object>();
                if (IsValidMetafieldValue(sdata.Id.ToString()))
                {
                    metafields.Add(new
                    {
                        key = "crm_id",
                        @namespace = "custom",
                        value = sdata.Id.ToString().Trim(),
                        type = "single_line_text_field"
                    });
                }
              

                if (IsValidMetafieldValue(sdata.ScraperName))
                {
                    metafields.Add(new
                    {
                        key = "scraper_origin",
                        @namespace = "custom",
                        value = sdata.ScraperName.Trim(),
                        type = "single_line_text_field"
                    });
                }

                if (IsValidMetafieldValue(sdata?.ConditionGrade))
                {
                    var condiGrade = Map_Condition_Grade(sdata.ConditionGrade.Trim());
                    if (!string.IsNullOrEmpty(condiGrade))
                    {
                        metafields.Add(new
                        {
                            key = "product_condition_grade_preloved",
                            @namespace = "custom",
                            value = condiGrade.Trim(),
                            type = "single_line_text_field"
                        });
                    }
                }

                if (IsValidMetafieldValue(sdata.Condition))
                {
                    string condition = sdata.Condition == "Pre-Owned" ? "Preloved" : "New";
                    metafields.Add(new
                    {
                        key = "product_condition",
                        @namespace = "custom",
                        value = condition.Trim(),
                        type = "single_line_text_field"
                    });
                }

                metafields.Add(new
                {
                    key = "age_group",
                    @namespace = "custom",
                    value = "Adult",
                    type = "single_line_text_field"
                });

                if (IsValidMetafieldValue(sdata.Category))
                {
                    metafields.Add(new
                    {
                        key = "category",
                        @namespace = "custom",
                        value = sdata.Category.Trim(),
                        type = "single_line_text_field"
                    });
                }

                if (IsValidMetafieldValue(sdata.ProductType))
                {
                    metafields.Add(new
                    {
                        key = "product_type",
                        @namespace = "custom",
                        value = sdata.ProductType.Trim(),
                        type = "single_line_text_field"
                    });
                }

                var gender = GetGender(sdata.Gender);
                if (IsValidMetafieldValue(gender))
                {
                    metafields.Add(new
                    {
                        key = "gender",
                        @namespace = "custom",
                        value = gender,
                        type = "single_line_text_field"
                    });
                }

                // Build tags
                var tags = new List<string>
                {
                    "ALL PRODUCTS",
                    sdata.Brand,
                    sdata.Gender,
                    sdata.ProductType,
                    sdata.Category,
                    sdata.Condition,
                    "Not in HQ"
                };

                if (sdata.Condition == "Pre-Owned")
                {
                    tags.Add("PRELOVED");
                }
                shopifydata.Add(
                        new
                        {
                           input = new {
                               title = sdata.Title,
                            descriptionHtml = sdata.Description,
                            vendor = sdata.Brand,
                            category = sdata.Category,
                            productType = sdata.ProductType,
                            tags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                            status = "ACTIVE",
                            metafields = metafields,
                            variants = sdata.Variants.Select(v => new
                            {
                                price = Addmarkup(v.Price).ToString("F2"),
                                sku = v.SKU,
                                inventoryQuantities = 1,
                                inventoryPolicy = "DENY",
                                requiresShipping = true,
                                options = new[] { v.Size }
                            }).ToArray(),
                            images = sdata.Image.OrderBy(i => i.Priority).Select(img => new
                            {
                                src = img.Url
                            }).ToArray()
                        }
                        }
                    );
            }

            return shopifydata;
        }

        private string Prepare_Bulk_Creation_Mutation(string path)
        {
           return $@"
        mutation {{
          bulkOperationRunMutation(
            mutation: """""" 
            mutation call($input: ProductInput!) {{
              productCreate(input: $input) {{
                product {{
                  id
                  title
                  metafields(first: 20) {{
                    edges {{
                      node {{
                        namespace
                        key
                        value
                        type
                      }}
                    }}
                  }}
                  variants(first: 10) {{
                    edges {{
                      node {{
                        id
                        title
                        inventoryQuantity
                      }}
                    }}
                  }}
                }}
                userErrors {{
                  message
                  field
                }}
              }}
            }}
            """""",
            stagedUploadPath: ""{path}""
          ) {{
            bulkOperation {{
              id
              url
              status
            }}
            userErrors {{
              message
              field
            }}
          }}
        }}
    ";
        }
        
        private async Task<string>  bulk_query_pulling()
        {
            try
            {
                var query = @"query {
             currentBulkOperation(type: MUTATION) {
                id
                status
                errorCode
                createdAt
                completedAt
                objectCount
                fileSize
                url
                partialDataUrl
             }
            }";
                var payload = new { query= query };
                var status = "";
                var url = "";
                while (status != "COMPLETED")
                {
                    var d = await this.ExecuteGraphQLAsync(payload);
                    status = d.GetProperty("currentBulkOperation").GetProperty("status").GetString();
                    if (status == "COMPLETED")
                    {
                        url=d.GetProperty("currentBulkOperation").GetProperty("url").GetString();
                        break;
                    }
                    Thread.Sleep(180000);
                }
                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
          
        }

        private async Task<object> Retrieve_bulkResults_data(string url)
        {
            if(url==null)
                return new { };
            try
            {
                var result = await _httpClient.GetAsync(url);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new { };
            }
          
        }

        private async Task<string> Upload_file_shopify(string path)
        {
            return "";
        }

        private async Task<string> Stage_uploads_Create(string name)
        {
            var query = this.Get_Staged_Uploads_Create_Mutation();
            var variables = new
            {
                input = new[]
                {
                    new
                    {
                        filename  = $"{name}.jsonl",
                        mimeType  ="text/plain",
                        httpMethod= "POST",
                        resource  = "BULK_MUTATION_VARIABLES"
                    }
                }
            };
            var payload = new { query=query, variables  };
            var k=   await this.ExecuteGraphQLAsync(payload);
            var url = k.GetProperty("stagedUploadsCreate").GetProperty("stagedTargets")[0];
            return "";
        }
        private string Get_Staged_Uploads_Create_Mutation()
        {
            return @"
        mutation stagedUploadsCreate($input: [StagedUploadInput!]!) {
          stagedUploadsCreate(input: $input) {
            stagedTargets {
              url
              resourceUrl
              parameters {
                name
                value
              }
            }
            userErrors {
              field
              message
            }
          }
        }
    ";
        }

        
    }

}
