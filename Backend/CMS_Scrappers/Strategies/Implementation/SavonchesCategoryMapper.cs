using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Routing.Tree;
public  class SavonchesCategoryMapper:ICategoryMappingStrategy{
    
   private static readonly Dictionary<string,(string TrendCategory,string Trendproduct)> _categoryMap;

    static SavonchesCategoryMapper()
    {
        _categoryMap=new (StringComparer.OrdinalIgnoreCase)
        {
            //Clothing
            { "Coats & Jackets", ("Clothing", "Coats & Jackets") },
            { "Light Jackets", ("Clothing", "Coats & Jackets") },
            { "T-Shirts", ("Clothing", "T-Shirts & Tops") },
            { "Shirts", ("Clothing", "Shirts & Polo Shirts") },
            { "Jeans/Pante", ("Clothing", "Jeans & Bottoms") },
            { "Sweatshirts & Hoodies", ("Clothing", "Hoodies & Sweatshirts") },
            { "Sherts", ("Clothing", "Shorts") }, 
            { "Sweaters & Knitwear", ("Clothing", "Knitwear") },

            // Accessories
            { "Bumbags", ("Accessories", "Bags") },
            { "Handbags", ("Accessories", "Bags") },
            { "Shoulder Bags", ("Accessories", "Bags") },
            { "Mini Bags/Messenger Bags/Sling Bags", ("Accessories", "Bags") },
            { "Crossbody Bags", ("Accessories", "Bags") },
            { "Backpacks", ("Accessories", "Bags") },
            { "Duffel Bags", ("Accessories", "Bags") },
            { "Luggages", ("Accessories", "Bags") },
            { "Toiletry Pouches", ("Accessories", "Bags") },
            { "Belts", ("Accessories", "Belts") },
            { "Cardholders & Wallets", ("Accessories", "Wallets & Cardholders") },
            { "Eyewear", ("Accessories", "Sunglasses") },
            { "Hats & Scarves", ("Accessories", "Winter Accessories") },
            { "Jewelry", ("Accessories", "Jewellery") },

            // Footwear
            { "High Top Sneakers", ("Footwear", "Trainers") },
            { "Low-Top Sneakers", ("Footwear", "Trainers") },
            { "Beets", ("Footwear", "Boots") }, 
            { "Formal Shoes", ("Footwear", "Flats") },
            { "Sandals & Slides", ("Footwear", "Sandals") }
        };

    }
    public  (string TrendsCategory,string TrendsProductType)  GetCategory(string savonchesProductType){

     if(string.IsNullOrWhiteSpace(savonchesProductType))
     {
        return (string.Empty,string.Empty);
     }

     if(_categoryMap.TryGetValue(savonchesProductType,out var trendData))
     {
        return trendData;
     }
     return (string.Empty,string.Empty);   
    }
 
}