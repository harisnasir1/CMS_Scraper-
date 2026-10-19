using System.Net.Http.Headers;
using CMS_Scrappers.Data.DTO.RR_Sync_DTO;
using CMS_Scrappers.Services.Interfaces;
using CMS_Scrappers.Data.Requests;
using CMS_Scrappers.Repositories.Interfaces;
using CMS_Scrappers.Utils;
using CMS_Scrappers.Suppliers;
using ResellersTech.Backend.Scrapers.Shopify.Http.Responses.RRSyncResopnse;

namespace CMS_Scrappers.Services.Implementations;

public class RRSyncService:IRRSyncService
{
    private readonly HttpClient _http;
    private readonly ILogger<RRSyncService> _logger;
    private readonly IRRSyncAuthService _syncauthService;
    private readonly RRSyncConfig _syncconfig;
    private readonly RRSyncAccount _account;

    /// <summary>Built by IRRSyncClientFactory only; every call is made as <paramref name="account"/>.</summary>
    public RRSyncService(ILogger<RRSyncService> logger, RRSyncConfig syncconfig, IRRSyncAuthService syncauthService, HttpClient http, RRSyncAccount account)
    {
        _logger = logger;
        _syncauthService = syncauthService;
        _syncconfig = syncconfig;
        _http = http;
        _account = account;
    }
    
    public async Task<RRApiResponse<Dictionary<string, string>>> PushNewProductBatchAsync(BulkCreateRRSyncProductRequest batch)
    {
        
        var token = await _syncauthService.GetTokenAsync(_account);

        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_syncconfig.BaseURl}/api/Scrapper/CreateProduct")
        {
            Content = JsonContent.Create(batch)
        };

        msg.Headers.Add("Cookie", $"access_token={token}");

        var response = await _http.SendAsync(msg);
        var rawBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Bulk create HTTP {Status} ({Reason}). Body: {Body}",
                (int)response.StatusCode, response.ReasonPhrase, rawBody);

            return new RRApiResponse<Dictionary<string, string>>
            {
                IsSuccess = false,
                Status = "Fail",
                Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                ErrorCode = (int)response.StatusCode,
            };
        }
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<Dictionary<string, string>>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }

    public async Task<RRApiResponse<Dictionary<string, string>>>  PushNewVariantBatchAsync(string Syncproductid,
        List<CreateRRSyncVariantRequest> variantRequest)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_syncconfig.BaseURl}/api/Scrapper/AddVariants/{Syncproductid}")
        {
            Content = JsonContent.Create(variantRequest)
        };

        msg.Headers.Add("Cookie", $"access_token={token}");

        var response = await _http.SendAsync(msg);
        var rawBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Bulk create HTTP {Status} ({Reason}). Body: {Body}",
                (int)response.StatusCode, response.ReasonPhrase, rawBody);

            return new RRApiResponse<Dictionary<string, string>>
            {
                IsSuccess = false,
                Status = "Fail",
                Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                ErrorCode = (int)response.StatusCode,
            };
        }
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<Dictionary<string, string>>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }

  public  async Task<RRApiResponse<List<RRSyncVarinatDTO>>> GetAllSyncVarinats(string syncproductid)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        var url = $"{_syncconfig.BaseURl}/api/Scrapper/GetMyProductVariants?productId={Uri.EscapeDataString(syncproductid)}";
        using var msg = new HttpRequestMessage(
            HttpMethod.Get,url);
       

        msg.Headers.Add("Cookie", $"access_token={token}");

        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<List<RRSyncVarinatDTO>>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }

    public async Task<RRApiResponse<object>> UpdateVariantBatchAsync(List<UpdateRRSyncVariantRequest> variantRequest,string Syncproductid)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        using var msg = new HttpRequestMessage(
            HttpMethod.Put,
            $"{_syncconfig.BaseURl}/api/Scrapper/UpdateVariant/{Syncproductid}")
        {
            Content = JsonContent.Create(variantRequest)
        };
        msg.Headers.Add("Cookie", $"access_token={token}");
        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<object>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }

    public async Task<RRApiResponse<object>> DeleteVariantAsync(string variantId)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        using var msg = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_syncconfig.BaseURl}/api/Scrapper/DeleteVariant/{variantId}");
        msg.Headers.Add("Cookie", $"access_token={token}");
        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<object>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }

    public async Task<RRApiResponse<object>> DeleteProductAsync(string rrProductId)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        using var msg = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{_syncconfig.BaseURl}/api/Product/DeleteProduct/{Uri.EscapeDataString(rrProductId)}");
        msg.Headers.Add("Cookie", $"access_token={token}");

        var response = await _http.SendAsync(msg);
        
        var rawBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return new RRApiResponse<object> { IsSuccess = true, Status = "Success", Message = rawBody };
        }

      
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogWarning("RRSync product {Id} had nothing left to delete: {Body}", rrProductId, rawBody);
            return new RRApiResponse<object> { IsSuccess = true, Status = "AlreadyDeleted", Message = rawBody };
        }

        _logger.LogError("RRSync product delete failed for {Id}. HTTP {Status}: {Body}",
            rrProductId, (int)response.StatusCode, rawBody);
        return new RRApiResponse<object>
        {
            IsSuccess = false,
            Status = "Fail",
            Message = $"HTTP {(int)response.StatusCode}: {rawBody}",
            ErrorCode = (int)response.StatusCode,
        };
    }

   public async Task<RRApiResponse<RRSyncSourceResponse>> GetSourceByname(string scrappername)
    {
        var token = await _syncauthService.GetTokenAsync(_account);
        var url = $"{_syncconfig.BaseURl}/api/Source/GetSourceByName/{Uri.EscapeDataString(scrappername)}";
        using var msg = new HttpRequestMessage(
            HttpMethod.Get,url);
       

        msg.Headers.Add("Cookie", $"access_token={token}");

        var response = await _http.SendAsync(msg);
        var body = await response.Content.ReadFromJsonAsync<RRApiResponse<RRSyncSourceResponse>>()
                   ?? throw new InvalidOperationException("Empty bulk create response");

        if (!response.IsSuccessStatusCode || !body.IsSuccess)
        {
            _logger.LogError(
                "Bulk create failed. HTTP {Status}, IsSuccess={IsSuccess}, Message={Message}, ErrorCode={ErrorCode}",
                response.StatusCode, body.IsSuccess, body.Message, body.ErrorCode);
        }

        return body;
    }
    public static int MapConditionToRating(string? conditionGrade)
    {
        var normalized = (conditionGrade ?? "").Trim().ToLowerInvariant();
    
        return normalized switch
        {
            "new"                  => 5,
            "like new condition."  => 4,
            "great condition."     => 3,
            "good condition."      => 2,
            "used condition."      => 1,
            _                      => 5  // policy: unknown defaults to new
        };
    }
    // Pricing lives in CMS_Scrappers.Pricing (IPricingPolicyResolver); this client only transports numbers.
}