public interface IScrappers{
    Task ScrapeAsync();
    Task MarkUnseenProductsAsSourceDeleted();
}