public class CategoryMapper : ICategoryMapper
{

    private readonly ILogger<CategoryMapperFactory> _logger;

    private readonly ICategoryMappingStrategy _ICategoryMappingStrategy;
    public CategoryMapper(ILogger<CategoryMapperFactory> logger, ICategoryMappingStrategy CategoryMappingStrategy)
    {
        _logger = logger;
        _ICategoryMappingStrategy = CategoryMappingStrategy;
    }
    public List<ShopifyFlatProduct> TrendCategoryMapper(List<ShopifyFlatProduct> l)
    {
        var Data=l;
         foreach(var i in Data)
         {
             (string c,string p)=_ICategoryMappingStrategy.GetCategory(i.Category);
             i.Category=c;
             i.ProductType=p;
         }
         return Data; 
    }
}