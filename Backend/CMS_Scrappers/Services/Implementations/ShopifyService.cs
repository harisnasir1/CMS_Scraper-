using System.Net;
using System.Text.Json;
using System.Text;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Utils;
using  CMS_Scrappers.Data.DTO;
using CMS_Scrappers.Repositories.Interfaces;
using System.Net.Http.Headers;
using CMS_Scrappers.Models;

namespace CMS_Scrappers.Services.Implementations
{
    public class ShopifyService:IShopifyService
    {
        private readonly ShopifySettings _shopifySettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ShopifyService> _logger;
        private readonly IProductStoreMappingRepository _ProductMappingRepository;
        private IFileReadWrite _readWrite;
        private string _locationId;
        
        public ShopifyService(ShopifySettings shopifysettings,ILogger<ShopifyService> logger, IProductStoreMappingRepository ProductMappingRepository,IFileReadWrite readWrite) {
            
            _ProductMappingRepository = ProductMappingRepository;
            _shopifySettings = shopifysettings;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", shopifysettings.SHOPIFY_ACCESS_TOKEN);
            _logger = logger;
            _locationId = "";
            _readWrite = readWrite;
        }
        
        public async Task<int> Total_variant_per_store()
        {
            try
            {
                var query = @"
                 query ProductVariantsCount {
                   productVariantsCount {
                     count
                   }
                 }";;
                var payload = new { query = query };
                var data = await ExecuteGraphQLAsync(payload);
                var count =  data.GetProperty("productVariantsCount").GetProperty("count").GetInt32();
                return count;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching total variants exists on store:{_shopifySettings.SHOPIFY_STORE_NAME} ",ex);
                return -1;
            }
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
                    template_suffix = sdata.ProductType== "Accessories" ? "single-size":"Default product",
                    tags = $" ALL PRODUCTS,{sdata.Brand}, {sdata.Gender},{sdata.ProductType},{sdata.Category},{sdata.Condition},Not in HQ, RRSYNC ,{(sdata.Condition == "Pre-Owned" ? "PRELOVED":"")}",
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
            if(_shopifySettings.SHOPIFY_STORE_NAME=="Morely Trends UK")
            {
                await pushmetafields(sdata, shopifyProductId.ToString());
            }
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
        
        public async Task UpdateProduct(List<ShopifyFlatProduct> existingproduct, Dictionary<string, Sdata> sdata)
        {
            var locationId = await GetFirstLocationIdAsync();
           
            if (string.IsNullOrEmpty(locationId))
            {                
                _logger.LogError("Could not find a Shopify location to update inventory.");
                return;
            }
            decimal Batchsizes = 250;
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
                    if (sdata.TryGetValue(incomingProduct.ProductUrl, out var dbProduct))                           // the thing happing here is when we scrape dta from the endpoint we don't have all the ids and stuff as it is not from db.//and to mentain the flow we get the live data from db and match them on product url which is gonna be unique dah !
                    {
                        string gid = $"gid://shopify/Product/{dbProduct.ProductStoreMapping.First().ExternalProductId}";       //we have migrated to different table so we need to get this from different table.
                        //as db constraints one sdata can point to multiple product mapping but we one productmap is against one store
                        // so we get one product for one sotre so in return we always get 1 mapping that is why we can use [0]
                                                                                                                                                     
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
           
        public async Task<bool> Bulk_mutation_shopify_product_creation(List<Sdata> data,string name)
        {
            var jonldata = await PrepareProductInputForGraphQL(data,name);
            var lmap = jonldata.Item2;
            string path = GetJsonlPath(name);
            try
            {
                var key=await Initial_prep_for_Bulk(name,jonldata.Item1);
                if (string.IsNullOrEmpty(key)) return false;
                var bulkOp =  await StartBulkProductCreateAsync(key);
                var shopifyCmsIds=   await Insert_Get_BulkInsert_Data(lmap);
                if (shopifyCmsIds.Count==0)
                {
                    _logger.LogError("No Shopify IDs were returned from the pulling function. Stopping bulk insert.");   
                    return false;
                }

                await  PublishPtoductsToChannelsBulk(shopifyCmsIds,name);
                
                _readWrite.Delete_file(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
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
            double pound = (int)Math.Round(ourprice * 0.8);
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

        private async Task<string> GetShopifyId(Guid sid)
        {
            try
            {
                var id = this._shopifySettings.SHOPIFY_STORE_ID;
                return await _ProductMappingRepository.GetSyncIdBySidAndStoreId(sid,id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<JsonElement> Stage_uploads_Create(string name)
        {
            var query =  @"
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
                    } ";;
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
            return url;
        }
        
        private async Task <Boolean> Stage_upload_file(JsonElement stageRes, string path)
    {
                  var uploadUrl = stageRes.GetProperty("url").GetString();
                  var parameters = stageRes.GetProperty("parameters");
              
                  
                  using var form = new MultipartFormDataContent("UploadBoundary" + DateTime.Now.Ticks.ToString("x"));
              
               
                  foreach (var p in parameters.EnumerateArray())
                  {
                      var name = p.GetProperty("name").GetString();
                      var value = p.GetProperty("value").GetString();
                      
                      var content = new StringContent(value);
                      // Remove default headers that can confuse Google on simple string parts
                      content.Headers.Remove("Content-Type");
                      form.Add(content, name);
                  }
                  
                  byte[] fileBytes = await File.ReadAllBytesAsync(path);
                  var fileContent = new ByteArrayContent(fileBytes);
                  fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
              
                  form.Add(fileContent, "file", "bulk_upload.jsonl");
              
              
                  foreach (var part in form)
                  {
                      if (part.Headers.ContentDisposition != null)
                      {
                       
                          part.Headers.ContentDisposition.FileNameStar = null;
                          
                       
                          part.Headers.ContentDisposition.Name = $"\"{part.Headers.ContentDisposition.Name.Trim('"')}\"";
                          if (part.Headers.ContentDisposition.FileName != null)
                          {
                              part.Headers.ContentDisposition.FileName = $"\"{part.Headers.ContentDisposition.FileName.Trim('"')}\"";
                          }
                      }
                  }
              
              
                  _httpClient.DefaultRequestHeaders.TransferEncodingChunked = false;
              
                  var response = await _httpClient.PostAsync(uploadUrl, form);
                   
                  if (!response.IsSuccessStatusCode)
                  {
                      var errorBody = await response.Content.ReadAsStringAsync();
                      // This will print the XML error from Google if it still fails
                      Console.WriteLine($"Google Response: {errorBody}");
                      response.EnsureSuccessStatusCode();
                      return false;
                  }

                  return true;
    }
        
        private async Task<BulkOperationStartResult> StartBulkProductCreateAsync(string stagedUploadPath)
        {
            var payload = new
            {
                query = @"
                mutation bulkProductCreate($path: String!) {
                  bulkOperationRunMutation(
                    mutation: ""
                      mutation productCreate($input: ProductSetInput!) {
                        productSet(input: $input) {
                          product { id }
                          userErrors {
                            field
                            message
                          }
                        }
                      }
                    ""
                    stagedUploadPath: $path
                  ) {
                    bulkOperation {
                      id
                      status
                    }
                    userErrors {
                      field
                      message
                    }
                  }
                }",
                variables = new
                {
                    path = stagedUploadPath
                }
            };

            var data = await ExecuteGraphQLAsync(payload);

            var root = data.GetProperty("bulkOperationRunMutation");

            
            if (root.TryGetProperty("userErrors", out var errors) &&
                errors.GetArrayLength() > 0)
            {
                throw new Exception($"Bulk start error: {errors}");
            }

            var bulk = root.GetProperty("bulkOperation");

            return new BulkOperationStartResult
            {
                Id = bulk.GetProperty("id").GetString(),
                Status = bulk.GetProperty("status").GetString()
            };
        }

        private async Task<BulkOperationStartResult> StartBulkProductPublishAsync(string stagedUploadPath)
        {
            var payload = new
            {
                query = @"
                    mutation bulkPublish($path: String!) {
                      bulkOperationRunMutation(
                        mutation: ""
                          mutation publishProduct($id: ID!, $input: [PublicationInput!]!) {
                            publishablePublish(id: $id, input: $input) {
                              publishable {
                                ... on Product { id }
                              }
                              userErrors {
                                field
                                message
                              }
                            }
                          }
                        ""
                        stagedUploadPath: $path
                      ) {
                        bulkOperation {
                          id
                          status
                        }
                        userErrors {
                          field
                          message
                        }
                      }
                    }",
                variables = new
                {
                    path = stagedUploadPath
                }
            };

            var data = await ExecuteGraphQLAsync(payload);
            var root = data.GetProperty("bulkOperationRunMutation");

            if (root.TryGetProperty("userErrors", out var errors) && errors.GetArrayLength() > 0)
            {
                // Log specifically for debugging
                _logger.LogError($"Bulk Publication Start Error: {errors}");
                throw new Exception($"Bulk Publication start error: {errors}");
            }

            var bulk = root.GetProperty("bulkOperation");

            return new BulkOperationStartResult
            {
                Id = bulk.GetProperty("id").GetString(),
                Status = bulk.GetProperty("status").GetString()
            };
        }
        
        private async Task<string> Pull_Bulk_results()
        {
             try
             {
                 var payload = new
                 {
                     query = @"{
                         currentBulkOperation(type: MUTATION) 
                         {
                             id
                             status
                             errorCode
                             objectCount
                             fileSize
                             url
                             partialDataUrl
                         }}"
                 };
                 _logger.LogInformation("Starting polling for Bulk Operation completion...");
                 var bres = await ExecuteGraphQLAsync(payload);
                 var status = bres.GetProperty("currentBulkOperation").GetProperty("status").ToString();
        
                 while (status != "COMPLETED")
                 {
                     if (status == "FAILED" || status == "CANCELED")
                     {
                         var error = bres.GetProperty("currentBulkOperation").GetProperty("errorCode").GetString();
                         _logger.LogError($"Bulk operation stopped. Status: {status}, Error: {error}");
                         return "";
                     }
        
                     
                     await Task.Delay(22000); 
                     bres = await ExecuteGraphQLAsync(payload);
                     status = bres.GetProperty("currentBulkOperation").GetProperty("status").ToString();
                     _logger.LogInformation($"Current Status: {status}");
                 }
                 
                 _logger.LogInformation("Operation COMPLETED. Waiting 20s for file stabilization...");
                 await Task.Delay(2000);
        
                 var url = bres.GetProperty("currentBulkOperation").GetProperty("url").ToString();
                 if (string.IsNullOrEmpty(url))
                 {
                     _logger.LogError("Bulk operation completed but URL is null.");
                     return "";
                 }

                 return url;

             }
             catch (Exception e)
             {
                 _logger.LogCritical($"Critical failure in Pull_Bulk_results: {e.Message}");
                 return "";
             }
             
        }

        private async Task<Dictionary<Guid, string>> Insert_Get_BulkInsert_Data(Dictionary<long, Guid> lmap)
        {
            try
            {
                Dictionary<Guid, string> Shopify_cmsids = new Dictionary<Guid, string>();
                var url =await Pull_Bulk_results();
                if (string.IsNullOrEmpty(url))
                {
                    return new Dictionary<Guid, string>();
                }
                 using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                 
                 if (response.StatusCode == HttpStatusCode.NotFound)
                 {
                     _logger.LogError("Shopify result file not found (404).");
                     return new Dictionary<Guid, string>();
                 }
        
                 response.EnsureSuccessStatusCode();
        
                 using (var sTdata = await response.Content.ReadAsStreamAsync())
                 using (var reader = new StreamReader(sTdata, Encoding.UTF8))
                 {
                     string? line;
                     int successCount = 0;
                     int errorCount = 0;
        
                     _logger.LogInformation("Starting to parse results...");
        
                     while ((line = await reader.ReadLineAsync()) != null)
                     {
                         try
                         {
                             using var doc = JsonDocument.Parse(line);
                             var productSet = doc.RootElement.GetProperty("data").GetProperty("productSet");
                             
                             if (productSet.TryGetProperty("product", out var product) && product.ValueKind != JsonValueKind.Null)
                             {
                                 var shopifyId = product.GetProperty("id").GetString();
                                 int lineNumber = doc.RootElement.GetProperty("__lineNumber").GetInt32();
        
                                 
                                 if (lmap.TryGetValue((long)lineNumber, out Guid productid))
                                 {
                                     string externalid = shopifyId.Split('/').Last(); 
        
                                     var mapping = new ProductStoreMapping
                                     {
                                         Id = Guid.NewGuid(),
                                         ProductId = productid,
                                         ShopifyStoreId = _shopifySettings.SHOPIFY_STORE_ID,
                                         ExternalProductId = externalid,
                                         SyncStatus = "Live",
                                         LastSyncedAt = DateTime.UtcNow,
                                         CreatedAt = DateTime.UtcNow,
                                         UpdatedAt = DateTime.UtcNow
                                     };
                                     await _ProductMappingRepository.InsertProductmapping(mapping);
                                     Shopify_cmsids.Add(productid, shopifyId);
                                     successCount++;
                                 }
                                 else
                                 {
                                     _logger.LogWarning($"Line {lineNumber} in Shopify file had no matching entry in lmap.");
                                     errorCount++;
                                 }
                             }
                             else
                             {
                                 _logger.LogWarning($"Shopify skipped line creation. Raw: {line}");
                                 errorCount++;
                             }
                         }
                         catch (Exception ex)
                         {
                             _logger.LogError($"Error processing a single result line: {ex.Message}");
                             errorCount++;
                         }
                     }
        
                     _logger.LogInformation($"Bulk Processing Finished. Success: {successCount}, Errors/Skipped: {errorCount}");
                 }
        
                 return Shopify_cmsids;
            }
            catch (Exception e)
            {
                _logger.LogCritical($"Critical failure in Pull_Bulk_results: {e.Message}");
                return new Dictionary<Guid, string>();
            }
         
        }
        
        private string? Getkeystageparm(JsonElement t)
        {
            var k=t.GetProperty("parameters");
            foreach (var parms in k.EnumerateArray())
            {
                var name = parms.GetProperty("name").GetString();
                var value = parms.GetProperty("value").GetString();
                if (name == "key" && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            return null;
        }

        private async Task<(List<object>,Dictionary<long,Guid>)> PrepareProductInputForGraphQL(List<Sdata> data, string name)
        {
            var lineIndex = new Dictionary<long, Guid>();
            int linenumber = 0;
            var shopifydata = new List<object>();
            var locid = await GetFirstLocationIdAsync();
            string batchTimestamp = $"Batch-{DateTime.Now:dd-MM-yy-HHmm}";
            foreach (var sdata in data)
            {
                var metafields = await BuildMetafields(sdata, name);
         
                var tags = new List<string>
                {
                    "ALL PRODUCTS",
                    sdata.Brand,
                    sdata.Gender,
                    sdata.ProductType,
                    sdata.Category,
                    sdata.Condition,
                    "Not in HQ",
                    "RRSync",
                    "RRSyncBulk",
                    "test1"
                };

                if (sdata.Condition == "Pre-Owned")
                {
                    tags.Add("PRELOVED");
                }

                shopifydata.Add(new
                {
                    input = new
                    {
                        title = sdata.Title,
                        descriptionHtml = sdata.Description,
                        vendor = sdata.Brand,
                        productType = sdata.ProductType,
                        tags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
                        status = "ACTIVE",
                        metafields = metafields,
                        productOptions = new[]
                        {
                            new
                            {
                                name = "Size",
                                values = sdata.Variants
                                    .Select(v => v.Size)
                                    .Distinct()
                                    .Select(size => new { name = size })
                                    .ToArray()
                            }
                        },
                        variants = sdata.Variants.Select(v => new
                        {
                            price = Addmarkup(v.Price).ToString("F2"),
                            sku = v.SKU!=""?v.SKU:sdata.Sku+"-"+v.Size,
                            optionValues = new[]
                            {
                                new { optionName = "Size", name = v.Size }
                            },
                            inventoryQuantities = new[]
                            {
                                new
                                {
                                    locationId = locid,
                                    name= "available",
                                    quantity = v.InStock ? 1 : 0
                                }
                            }
                        }).ToArray(),
                        files = sdata.Image
                            .OrderBy(i => i.Priority)
                            .Select(img => new
                            {
                                originalSource = img.Url,
                                alt= "Product image",  
                                filename=sdata.Title+"-"+img.Priority+".png",
                                contentType = "IMAGE"
                                
                            })
                            .ToArray()
                    }
                });
                
                lineIndex[linenumber]=sdata.Id;
                linenumber++;
            }

            return (shopifydata, lineIndex);
        }

        private async Task<Boolean> PublishPtoductsToChannelsBulk(Dictionary<Guid, string> cmsShopifyIds,string name)
        {
            try
            {
                var jsonldata = await prepareproductpublicationGraphql(cmsShopifyIds);
                name = name + "_publish";
                string path = GetJsonlPath(name);
                var key=  await Initial_prep_for_Bulk(name, jsonldata);
                var pstatus=await StartBulkProductPublishAsync(key);
                if (string.IsNullOrEmpty(pstatus.Id) || pstatus.Status == "FAILED")
                {
                    _logger.LogError($"Bulk Publication failed to queue. Status: {pstatus.Status}");
                    return false; 
                }
                var url =await Pull_Bulk_results();
              
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError($"Issue with pull Bulk operation : {url}");
                    return  false;
                }
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                 
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError("Shopify result file not found (404).");
                    return false;
                }
        
                response.EnsureSuccessStatusCode();
                using (var sTdata = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(sTdata, Encoding.UTF8))
                {
                    string? line;
                    int successCount = 0;
                    int errorCount = 0;

                    _logger.LogInformation("Starting to parse results...");

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        using var doc = JsonDocument.Parse(line);
                        var pubResult = doc.RootElement.GetProperty("data").GetProperty("publishablePublish");
    
                        if (pubResult.TryGetProperty("userErrors", out var errors) && errors.GetArrayLength() > 0)
                        {
                            _logger.LogError($"Line {doc.RootElement.GetProperty("__lineNumber")}: {errors.GetRawText()}");
                        }
                        else
                        {
                            successCount++;
                        }
                    }
                    _logger.LogInformation($"Publishing finished. Success: {successCount}");
                }
                _readWrite.Delete_file(path);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError("eror on building publication mutation data.");
                throw;
            }
        }

        private async Task<string> Initial_prep_for_Bulk(string name,List<object> jsonldata)
        {
            try
            { 
                string path = GetJsonlPath(name);
                await _readWrite.Wrtie_data(jsonldata, path);
                var stageres= await Stage_uploads_Create(name);
                var key = Getkeystageparm(stageres);
                if (key == null || string.IsNullOrEmpty(key)) return "";
                var flag= await Stage_upload_file(stageres, path);
                if (!flag) return "";
                return key;
            }
            catch (Exception e)
            {
                _logger.LogError("eror on building publication mutation data.");
                throw;
            }
        }

        private async Task<List<object>> prepareproductpublicationGraphql(Dictionary<Guid, string> cmsShopifyIds)
        {
            try
            {
                List<object> publications = new List<object>();
                List<PublicationInfo>  fillterchannels=await fillteredpublications();

                var publicationInputs = fillterchannels.Select(ch => new 
                { 
                    publicationId = ch.Id 
                }).ToList();
                foreach (KeyValuePair<Guid, string> entry in cmsShopifyIds)
                {    
                    publications.Add( new
                    {
                       
                        id =entry.Value,
                        input=publicationInputs,
 
                    }  );
                    
                }

                return publications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error on building publication mutation data.");
                return new List<object>();
            }
            
        }

        private  async Task<List<object>> BuildMetafields(Sdata sdata, string store)
           {
               var metafields = new List<object>();
               await EnsureSyncIdMetafieldDefinitionExistsAsync();
               if (IsValidMetafieldValue(sdata.Id.ToString()))
               {
                   metafields.Add(new
                   {
                       key = "syncid",
                       @namespace = "custom",
                       value = sdata.Id.ToString().Trim(),
                       type = "single_line_text_field"
                   });
               }
               if (!string.Equals(store, "Morely Trends UK", StringComparison.OrdinalIgnoreCase))
               {
                   return metafields;
               }
           
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
                   var conditionGrade = Map_Condition_Grade(sdata.ConditionGrade.Trim());
                   if (!string.IsNullOrWhiteSpace(conditionGrade))
                   {
                       metafields.Add(new
                       {
                           key = "product_condition_grade_preloved",
                           @namespace = "custom",
                           value = conditionGrade.Trim(),
                           type = "single_line_text_field"
                       });
                   }
               }
           
               if (IsValidMetafieldValue(sdata.Condition))
               {
                   var condition = sdata.Condition == "Pre-Owned" ? "Preloved" : "New";
           
                   metafields.Add(new
                   {
                       key = "product_condition",
                       @namespace = "custom",
                       value = condition,
                       type = "single_line_text_field"
                   });
               }
           
               // Always add age_group (only for this store)
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
           
               return metafields;
           }
        
        private async Task EnsureSyncIdMetafieldDefinitionExistsAsync()
            {
                // 1️⃣ Check existing definitions
                var checkPayload = new
                {
                    query = @"
                        query {
                          metafieldDefinitions(
                            first: 100,
                            namespace: ""custom"",
                            ownerType: PRODUCT
                          ) {
                            edges {
                              node {
                                key
                              }
                            }
                          }
                        }
                    "
                };
            
                var data = await ExecuteGraphQLAsync(checkPayload);
            
                var definitions = data
                    .GetProperty("metafieldDefinitions")
                    .GetProperty("edges");
            
                var exists = definitions
                    .EnumerateArray()
                    .Any(d => d.GetProperty("node").GetProperty("key").GetString() == "syncid");
            
            
                if (exists)
                    return;
            
                // 3️⃣ Create definition
                var createPayload = new
                {
                    query = @"
                        mutation CreateMetafieldDefinition($definition: MetafieldDefinitionInput!) {
                          metafieldDefinitionCreate(definition: $definition) {
                            userErrors {
                              field
                              message
                            }
                          }
                        }
                    ",
                    variables = new
                    {
                        definition = new
                        {
                            name = "Sync ID",
                            @namespace = "custom",
                            key = "syncid",
                            type = "single_line_text_field",
                            ownerType = "PRODUCT",
                            description = "External system sync identifier",
                            pin = true
                        }
                    }
                };
            
                var createData = await ExecuteGraphQLAsync(createPayload);
            
                var userErrors = createData
                    .GetProperty("metafieldDefinitionCreate")
                    .GetProperty("userErrors");
            
                if (userErrors.GetArrayLength() > 0)
                {
                    var message = userErrors[0].GetProperty("message").GetString();
                    throw new Exception($"Metafield definition error: {message}");
                }
            }

        private string GetJsonlPath(string name)
        {
            string baseDir = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null 
                ? "/tmp/jsonl_files"  // Railway/production
                : "/home/haris/Projects/office/CMS/Backend/CMS_Scrappers/JSONL_files"; // Local
    
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, $"{name}.jsonl");
        }

        private async Task<List<PublicationInfo>> fillteredpublications()
        {
            try
            {
                var allpublications = await GetStorePublications();
                var importantChannels = new[]
                {
                    "Online Store",
                    "Facebook",
                    "Instagram", 
                    "TikTok",
                    "Google",
                };
                var filltered = allpublications.Where(p => importantChannels.Any(c =>
                    p.Name.Contains(c, StringComparison.OrdinalIgnoreCase)
                )).ToList();
                _logger.LogInformation($"Publishing to {filltered.Count} channels:");
                return filltered;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return new List<PublicationInfo>();
            }
        }

        private async Task<List<PublicationInfo>> GetStorePublications()
        {
            var query = @"
            query{
            publications(first:20){
              edges{
            node{
             id
             name
             supportsFuturePublishing
            }}}}";
            var payload = new { query };
            var data = await ExecuteGraphQLAsync(payload);
            var publications = new List<PublicationInfo>();
            var edges = data.GetProperty("publications").GetProperty("edges");

            foreach (var edge in edges.EnumerateArray())
            {
                var node = edge.GetProperty("node");
                var id = node.GetProperty("id").GetString();
                var name = node.GetProperty("name").GetString();
                if (String.IsNullOrEmpty(id) || String.IsNullOrEmpty(name))
                {
                    _logger.LogError("Publications is returing null node", node);
                    continue;
                }
                publications.Add(new PublicationInfo
                {
                    Id = id,
                    Name = name
                });
            }

            return publications;
        }
    }
}