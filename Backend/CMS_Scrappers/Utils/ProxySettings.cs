namespace CMS_Scrappers.Utils;

public class ProxySettings
{
    public const string SectionName = "ProxySettings";

    public string? ApiUrl { get; set; }
    public string? FallbackList { get; set; }
    public string LocalFilePath { get; set; } = "prox.txt";
}