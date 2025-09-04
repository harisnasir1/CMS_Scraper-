using CMS_Scrappers.Strategies.Interface;

namespace CMS_Scrappers.Strategies.Implementation;

public class ChromeHeartsSizesStrategy
{
   private static readonly Dictionary<string,string > _BottomSizeMap;
   private static readonly Dictionary<string,string > _FootWearSizeMap;

   static ChromeHeartsSizesStrategy()
   {
      _BottomSizeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
      {

      };
   }
}