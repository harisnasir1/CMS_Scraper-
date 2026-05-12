namespace CMS_Scrappers.Services.Interfaces;

public interface ISavonchesSyncSize
{
    string GetMappedSizes(string category, string productType, string rawSizeValue,string gender);
}