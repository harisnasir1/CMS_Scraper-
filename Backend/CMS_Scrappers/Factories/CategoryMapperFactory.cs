public class CategoryMapperFactory:ICategoryMapperFact{

        private readonly IServiceProvider _serviceProvider;
       private readonly ILogger<CategoryMapperFactory> _logger;

      public CategoryMapperFactory(IServiceProvider serviceProvider,ILogger<CategoryMapperFactory> logger)
      {
         
         _serviceProvider=serviceProvider;
         _logger=logger;
      }
      public CategoryMapper GetCategoryMapper(string StoreName)
      {
           ICategoryMappingStrategy categoryStrategy;

          switch(StoreName.ToLowerInvariant()){
            case "savonches":
              categoryStrategy=_serviceProvider.GetRequiredService<SavonchesCategoryMapper>();
              break;
            default:
                throw new NotSupportedException($"Store '{StoreName}' is not supported.");
          }
        return new CategoryMapper(_logger,categoryStrategy);    
      }
}