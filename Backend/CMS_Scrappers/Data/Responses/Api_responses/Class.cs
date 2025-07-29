namespace CMS_Scrappers.Data.Responses.Api_responses
{
    public class ReviewProductRequest
    {
        public string ScraperId { get; set; } = "";
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
