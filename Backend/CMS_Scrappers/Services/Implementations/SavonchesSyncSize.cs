using System.Text.RegularExpressions;
using CMS_Scrappers.Services.Interfaces;

namespace CMS_Scrappers.Utils.variantMapperSync;

public class SavonchesSyncSize:ISavonchesSyncSize
{
    
    /// <summary>
    /// Converts US(Savonches data to ResellersSync Varaint Uk size
    ///
    /// </summary>
    
    /// <What needs to do when add more scrapper>
    ///  Make this a part of factory
    ///  Create a mapper like this for that scrapper
    ///  Let factory get size like category on runtime
    /// </What needs to do when add more scrapper>
    
   
       private static readonly HashSet<string> _footwearSystemA = new(StringComparer.OrdinalIgnoreCase)
    {
        "UK 3", "UK 3.5", "UK 4", "UK 4.5", "UK 5", "UK 5.5",
        "UK 6 (EU 39)", "UK 6 (EU 40)", "UK 6.5",
        "UK 7", "UK 7.5", "UK 8", "UK 8.5", "UK 9", "UK 9.5",
        "UK 10", "UK 10.5", "UK 11", "UK 11.5", "UK 12", "UK 12.5",
        "UK 13", "UK 13.5", "UK 14", "UK 14.5", "UK 15", "UK 16", "UK 17"
    };

    private static readonly HashSet<string> _clothingSystemA = new(StringComparer.OrdinalIgnoreCase)
    {
        "XS", "S", "M", "L", "XL", "XXL", "XXXL"
    };

    private static readonly HashSet<string> _jeansSystemA = new(StringComparer.OrdinalIgnoreCase)
    {
        "W 22", "W 23", "W 24", "W 25", "W 26", "W 27", "W 28", "W 29", "W 30",
        "W 31", "W 32", "W 33", "W 34", "W 35", "W 36", "W 37", "W 38", "W 39", "W 40"
    };

    private static readonly HashSet<string> _noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Black", "Brown", "Navy", "White", "Beige", "Green", "Orange", "Blue", "Red", "Grey",
        "Default Title", "Eastern", "Western", "PM"
    };

    public string? GetMappedSizes(string category, string productType, string rawSize,string gender)
    {
        // Accessories: every variant collapses to O/S
        if (string.Equals(category, "Accessories", StringComparison.OrdinalIgnoreCase))
            return "O/S";

        if (string.IsNullOrWhiteSpace(rawSize)) return null;

        var input = rawSize.Trim().Trim('"');

        // Drop obvious noise (colours, defaults, fragrance ml, ranges, fractions)
        if (IsNoise(input)) return null;

        return category?.Trim() switch
        {
            "Footwear" => MapFootwear(input,gender),
            "Clothing" when IsJeansType(productType) => MapJeans(input),
            "Clothing" => MapGeneralClothing(input),
            _ => null
        };
    }

    private static bool IsJeansType(string productType) =>
        string.Equals(productType, "Jeans & Bottoms", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(productType, "Shorts", StringComparison.OrdinalIgnoreCase);

    private static bool IsNoise(string input)
    {
        if (_noise.Contains(input)) return true;
        if (input.Contains("ml", StringComparison.OrdinalIgnoreCase)) return true;   // 100ml, 75ml
        if (input.Contains("/") && !input.Contains("O/S", StringComparison.OrdinalIgnoreCase)
                                && !input.Contains("S/M", StringComparison.OrdinalIgnoreCase)
                                && !input.Contains("M/L", StringComparison.OrdinalIgnoreCase))
            return true;  // 10/11, 8/9, 9-13, hat fractions like "7 1/2"
        if (Regex.IsMatch(input, @"^\d+-\d+$")) return true;  // 4-6, 8-10, 9-13 ranges
        return false;
    }

    private string? MapFootwear(string input,string gender)
    {
        // Drop US widths (10W, 5.5W, 8.5W etc) — RRSync has no width equivalent
        if (Regex.IsMatch(input, @"^\d+(\.\d+)?W$", RegexOptions.IgnoreCase))
            return null;

        if (!double.TryParse(input, out var usSize)) return null;

        var diff = 1;
        // US men's → UK men's = US - 1
        //Us women -> UK women = US - 2
        if(IsWomensGender(gender))
        {
            diff = 2;
        }
        var ukSize = usSize -diff;
        // Special case: Savonches doesn't distinguish UK 6 (EU 39) vs (EU 40); default to EU 40
        if (ukSize == 6) return "UK 6 (EU 40)";

        var ukSizeStr = ukSize % 1 == 0
            ? ukSize.ToString("0")
            : ukSize.ToString("0.#");

        var candidate = $"UK {ukSizeStr}";
        return _footwearSystemA.Contains(candidate) ? candidate : null;
    }

    private string? MapJeans(string input)
    {
        // Handle waist-x-length format (30X32) — strip length
        var waist = input.Split('X', 'x')[0].Trim();

        if (!int.TryParse(waist, out var w)) return null;

        var formatted = $"W {w}";
        return _jeansSystemA.Contains(formatted) ? formatted : null;
    }

    private string? MapGeneralClothing(string input)
    {
        var upper = input.Trim().ToUpperInvariant();

        // Common name variants
        upper = upper switch
        {
            "SMALL"   => "S",
            "LARGE"   => "L",
            "2XS"     => "XS",
            "2XL"     => "XXL",
            "3XL"     => "XXXL",
            "S/M"     => "S",   // round down per your decision
            "M/L"     => "M",   // round down
            _         => upper
        };

        // Drop 4XL+, 5XL etc — beyond RRSync's range
        if (Regex.IsMatch(upper, @"^\d+XL?$") || upper == "5L")
            return null;

        // Numeric US sizes — your data is US-based
        // Conservative mapping per women's US:
        // 0-2 → XS, 4-6 → S, 8-10 → M, 12-14 → L, 16-18 → XL, 20+ → XXL
        if (int.TryParse(upper, out var usNum))
        {
            return usNum switch
            {
                <= 2          => "XS",
                4 or 6        => "S",
                8 or 10       => "M",
                12 or 14      => "L",
                16 or 18      => "XL",
                20 or 22      => "XXL",
                _             => null
            };
        }

        return _clothingSystemA.Contains(upper) ? upper : null;
    }
    private static bool IsWomensGender(string gender) =>
        string.Equals(gender, "Women", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase);
    }
