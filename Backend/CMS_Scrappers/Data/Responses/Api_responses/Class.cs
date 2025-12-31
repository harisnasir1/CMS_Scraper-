namespace CMS_Scrappers.Data.Responses.Api_responses
{
    public class ReviewProductRequest
    {
        public string ScraperId { get; set; } = "";
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class SimilarproductRequest
    {
        public string productid { get; set; }
        public int page { get; set; }

    }

    public class SubmitRequest
    {
        public string productid { get; set; }

    }

    public class StroeSync
    {
        public string storeid { get; set; }
    }

    public class PushRequest
    {
        public string id { get; set; }

        public List<Requestimages> productimage { get; set; }
    }
    public class Requestimages
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string Url { get; set; } = "";
        public bool? Bgremove { get; set; } = false;

    }

    public class CountRequest
    {
        public string status { get; set; }
    }
    public class UpdateDetails
    {
        public string productid { get; set; }
        public string sku { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public int price { get; set; }
    }
}
