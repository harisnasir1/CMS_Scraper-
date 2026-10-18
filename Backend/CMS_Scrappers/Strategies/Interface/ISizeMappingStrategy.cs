namespace CMS_Scrappers.Strategies.Interface;

/// <summary>
/// Turns a supplier's raw variant size into one of the size values RRSync accepts.
/// Returns null when the variant should not be sent (noise, unsupported size, location duplicate).
/// </summary>
public interface ISizeMappingStrategy
{
    string? GetMappedSizes(string category, string productType, string rawSize, string gender);
}
