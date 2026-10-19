using System.Globalization;
using System.Text.RegularExpressions;
using CMS_Scrappers.Strategies.Interface;

namespace CMS_Scrappers.Suppliers.VNDSWORLD;

/// <summary>
/// VNDSWORLD option1 -> RRSync size. The store already uses UK footwear sizes and letter clothing sizes,
/// so most values pass through after normalisation. Built from the catalog-summary dump of 2026-10-18.
/// </summary>
public class VndsWorldSizeMapper : ISizeMappingStrategy
{
    /// <summary>Suffix the store appends to its Dubai-location duplicates. Those variants are skipped.</summary>
    public const string DubaiSuffix = " DXB";

    private static readonly HashSet<string> _footwear = new(StringComparer.OrdinalIgnoreCase)
    {
        "UK 3", "UK 3.5", "UK 4", "UK 4.5", "UK 5", "UK 5.5",
        "UK 6 (EU 39)", "UK 6 (EU 40)", "UK 6.5",
        "UK 7", "UK 7.5", "UK 8", "UK 8.5", "UK 9", "UK 9.5",
        "UK 10", "UK 10.5", "UK 11", "UK 11.5", "UK 12", "UK 12.5",
        "UK 13", "UK 13.5", "UK 14", "UK 14.5", "UK 15", "UK 16", "UK 17"
    };

    private static readonly HashSet<string> _clothing = new(StringComparer.OrdinalIgnoreCase)
    {
        "XS", "S", "M", "L", "XL", "XXL", "XXXL"
    };

    private static readonly HashSet<string> _waist = new(StringComparer.OrdinalIgnoreCase)
    {
        "W 22", "W 23", "W 24", "W 25", "W 26", "W 27", "W 28", "W 29", "W 30",
        "W 31", "W 32", "W 33", "W 34", "W 35", "W 36", "W 37", "W 38", "W 39", "W 40"
    };

    // Values that mean "one size" on this store: OS, fitted-cap fractions, jewellery and bag dimensions
    private static readonly HashSet<string> _oneSize = new(StringComparer.OrdinalIgnoreCase)
    {
        "OS", "O/S", "One Size", "7 3/8", "7 1/2", "7 1/4", "7 5/8", "45cm", "11inch", "5.5inch", "8x5inch", "MM"
    };

    private static readonly Dictionary<string, string> _letters = new(StringComparer.OrdinalIgnoreCase)
    {
        { "XS", "XS" }, { "Small", "S" }, { "S", "S" }, { "Medium", "M" }, { "M", "M" },
        { "Large", "L" }, { "L", "L" }, { "XL", "XL" }, { "XXL", "XXL" }, { "2XL", "XXL" },
        { "XXXL", "XXXL" }, { "3XL", "XXXL" },
        { "S/M", "S" }, { "M/L", "M" },           // round down, same rule as Savonches
        { "IT44", "XS" }, { "IT46", "S" }, { "IT48", "M" }, { "IT50", "L" }, { "IT52", "XL" }, { "IT54", "XXL" }, { "IT56", "XXXL" },
    };

    // EU -> UK men's footwear. Only the EU values seen in the dump plus their neighbours.
    private static readonly Dictionary<string, string> _euToUk = new(StringComparer.OrdinalIgnoreCase)
    {
        { "EU40", "UK 6 (EU 40)" }, { "EU40.5", "UK 6.5" }, { "EU41", "UK 7" }, { "EU41.5", "UK 7.5" },
        { "EU42", "UK 8" }, { "EU42.5", "UK 8.5" }, { "EU43", "UK 9" }, { "EU43.5", "UK 9.5" },
        { "EU44", "UK 9.5" }, { "EU44.5", "UK 10" }, { "EU45", "UK 10.5" }, { "EU45.5", "UK 11" },
        { "EU46", "UK 11" }, { "EU47", "UK 12" },
        // women's, marked "(w)" by the store
        { "EU36 (w)", "UK 3" }, { "EU37 (w)", "UK 4" }, { "EU38 (w)", "UK 5" }, { "EU39 (w)", "UK 6 (EU 39)" }, { "EU40 (w)", "UK 6 (EU 40)" },
    };

    public string? GetMappedSizes(string category, string productType, string rawSize, string gender)
    {
        if (string.IsNullOrWhiteSpace(rawSize)) return null;
        var input = rawSize.Trim();

        // Dubai-location duplicates are not synced (decision 2026-10-18)
        if (input.EndsWith(DubaiSuffix, StringComparison.OrdinalIgnoreCase)) return null;

        // Accessories collapse to one size, same as Savonches
        if (string.Equals(category, "Accessories", StringComparison.OrdinalIgnoreCase)) return "O/S";

        if (_oneSize.Contains(input)) return "O/S";

        return category?.Trim() switch
        {
            "Footwear" => MapFootwear(input),
            "Clothing" => MapClothing(input),
            _ => null
        };
    }

    private static string? MapFootwear(string input)
    {
        // "UK10", "UK 10", "UK10.5"
        var uk = Regex.Match(input, @"^UK\s*(\d+(?:\.5)?)$", RegexOptions.IgnoreCase);
        if (uk.Success)
        {
            var n = uk.Groups[1].Value;
            if (n == "6") return "UK 6 (EU 40)";          // the store does not say which EU 6; default like Savonches
            var candidate = $"UK {n}";
            return _footwear.Contains(candidate) ? candidate : null;
        }

        var key = Regex.Replace(input, @"\s+", " ");
        return _euToUk.TryGetValue(key, out var mapped) ? mapped : null;
    }

    private static string? MapClothing(string input)
    {
        // Waist sizes on jeans/joggers: "W34", "W 34"
        var waist = Regex.Match(input, @"^W\s*(\d{2})$", RegexOptions.IgnoreCase);
        if (waist.Success)
        {
            var candidate = $"W {waist.Groups[1].Value}";
            return _waist.Contains(candidate) ? candidate : null;
        }

        if (_letters.TryGetValue(input, out var letter)) return letter;

        return _clothing.Contains(input) ? input.ToUpperInvariant() : null;
    }
}
