namespace ResellersTech.Backend.Scrapers.Shopify.Http.Responses.RRSyncResopnse;

public class RRApiResponse<T>
{
    public string? Message { get; set; }
    public string? Status { get; set; }
    public bool IsSuccess { get; set; }
    public int ErrorCode { get; set; }
    public T? Payload { get; set; }
    public int PageNumber { get; set; }
    public int RowNumber { get; set; }
    public int Total { get; set; }
    public int? NextPage { get; set; }
}