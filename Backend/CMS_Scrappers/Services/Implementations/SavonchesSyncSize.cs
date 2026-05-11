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
    
   
        private readonly HashSet<string> _footwearSystemA = new() { 
            "UK 3","UK 3.5","UK 4","UK 4.5","UK 5","UK 5.5","UK 6 (EU 39)","UK 6 (EU 40)",
            "UK 6.5","UK 7","UK 7.5","UK 8","UK 8.5","UK 9","UK 9.5","UK 10","UK 10.5",
            "UK 11","UK 11.5","UK 12","UK 12.5","UK 13","UK 13.5","UK 14","UK 14.5",
            "UK 15","UK 16","UK 17"
        };

        private readonly HashSet<string> _clothingSystemA = new() { "XS", "S", "M", "L", "XL", "XXL", "XXXL" };

        private readonly HashSet<string> _jeansSystemA = new() { 
            "W 22", "W 23", "W 24", "W 25", "W 26", "W 27", "W 28", "W 29", "W 30",
            "W 31", "W 32", "W 33", "W 34", "W 35", "W 36", "W 37", "W 38", "W 39", "W 40"
        };

        /// <summary>
        /// Main method to process the "sizes" string from System B
        /// </summary>
        public string GetMappedSizes(string category, string productType, string rawSizeValue)
        {
            if (string.IsNullOrWhiteSpace(rawSizeValue) || rawSizeValue == "{}") 
                return category == "Accessories" ? "O/S" : null;

            // Split in case the variant string still contains noise like "{Black, 10}"
            var parts = rawSizeValue.Trim('{', '}').Split(',');

            foreach (var part in parts)
            {
                string input = part.Replace("\"", "").Trim();

                if (IsNoise(input)) continue;

                string mappedValue = category switch
                {
                    "Accessories" => "O/S",
                    "Footwear" => MapFootwear(input),
                    "Clothing" when (productType == "Jeans & Bottoms" || productType == "Shorts") 
                        => MapJeans(input),
                    "Clothing" => MapGeneralClothing(input),
                    _ => null
                };

                // Return the first valid size we find for this variant
                if (mappedValue != null) return mappedValue;
            }

            // Fallback for accessories or unidentified clothing
            return category == "Accessories" ? "O/S" : null;
        }

        private bool IsNoise(string input)
        {
           
            string[] colors = { "BLACK", "BROWN", "NAVY", "WHITE", "BEIGE", "GREEN", "ORANGE", "BLUE", "RED", "GREY", "DEFAULT TITLE", "EASTERN", "WESTERN" };
            if (colors.Contains(input.ToUpper())) return true;
            if (input.Contains("ml")) return true; // Filter 100ml, 75ml
            return false;
        }

        private string MapFootwear(string input)
        {
            
            var match = Regex.Match(input, @"(\d+(\.\d+)?)");
            if (match.Success && double.TryParse(match.Value, out double usSize))
            {
                double ukSize = usSize - 2; 
                string result = $"UK {ukSize}";
                
                if (ukSize == 6) return "UK 6 (EU 40)"; 

                return _footwearSystemA.Any(s => s.StartsWith(result)) ? _footwearSystemA.First(s => s.StartsWith(result)) : null;
            }
            return null;
        }

        private string MapJeans(string input)
        {
           
            var waistPart = input.Split('X')[0];
            if (int.TryParse(waistPart, out int w))
            {
                string formatted = $"W {w}";
                return _jeansSystemA.Contains(formatted) ? formatted : null;
            }
            return null;
        }

        private string MapGeneralClothing(string input)
        {
            string upper = input.ToUpper();

            if (upper == "SMALL") return "S";
            if (upper == "LARGE") return "L";
            if (upper == "2XL" || upper == "XXL" || upper == "2XL") return "XXL";
            if (upper == "3XL" || upper == "XXXL") return "XXXL";
            if (upper == "S/M") return "M";
            if (upper == "M/L") return "L";
            
            if (_clothingSystemA.Contains(upper)) return upper;

           
            if (int.TryParse(input, out int usNum))
            {
                return usNum switch
                {
                    <= 2 => "XS",
                    4 or 6 => "S",
                    8 or 10 => "M",
                    12 or 14 => "L",
                    16 or 18 => "XL",
                    20 or 22 => "XXL",
                    _ => null
                };
            }

            return null;
        }
    }
