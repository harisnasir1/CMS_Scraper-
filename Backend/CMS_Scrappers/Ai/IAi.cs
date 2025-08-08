namespace CMS_Scrappers.Ai
{
    public interface IAi
    {
        Task <string> GenerateDescription(Guid id);
    }
}
