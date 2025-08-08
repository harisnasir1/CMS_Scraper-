namespace CMS_Scrappers.Services.Interfaces
{
    public interface S3Interface
    {
        Task<string> Uploadimage (Stream images); 
    }
}
