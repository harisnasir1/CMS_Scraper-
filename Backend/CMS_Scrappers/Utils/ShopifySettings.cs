namespace CMS_Scrappers.Utils
{
    public class ShopifySettings
    {
        public Guid  SHOPIFY_STORE_ID{ get; set; }
        public string?  SHOPIFY_STORE_NAME { get; set; }
       public string SHOPIFY_ACCESS_TOKEN { get; set; }
       public string SHOPIFY_API_KEY {get;set;}
       public string SHOPIFY_API_SECRET  {get;set;}
       public string SHOPIFY_STORE_DOMAIN {get;set;}
    }
}
