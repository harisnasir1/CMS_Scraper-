
import { createContext, useContext, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { productsapis } from "@/api/ProductApis"
import { Sdata } from "@/types/Sdata"
import { Scraper } from "@/types/Scrappertype"
import { selectedimages } from "@/types/Simagestypes"

interface ProductContextType {
  products: Sdata[] | null
  ReviewProducts:Sdata[] | null
  isLoading: boolean
  SelectedScraper:Scraper|null,
  Selectedproduct:Sdata|null
  totalproducts:number|null
  currentPage:number|null
  similarimages:string[]|null,
  getScraperProducts: (scr:Scraper,PageNumber:number,PageSize:number) => Promise<void>
  normalizedate:(d:string)=>string
  Normalizetime:(runtime:string)=>string
  normalizeDateTime:(dateString:string)=>string
  getReviewProducts : (PageNumber:number,PageSize:number)=>Promise<void>
  Addselectedproduct:(data:Sdata)=>void
  Setcurrentpage:(page:number)=>void
  GetSimilarImg:(id:string)=>void
  GetMoreSimilarImg:(id:string,PageSize:number)=>void
  Submit:(id:string)=>void
  GetAiDescription:(id:string)=>Promise<string>
  UpdateProductDetails:(productid:string,sku:string,price:number,title:string,description:string)=>void
}

const ProductContext = createContext<ProductContextType | undefined>(undefined)

export function ProductProvider({ children }: { children: React.ReactNode }) {
  const [products, setProducts] = useState<Sdata[] | null>(null);
  const [ReviewProducts,setReviewProducts]=useState<Sdata[]|null>(null);
  const [SelectedScraper,setSelectedScraper]=useState<Scraper|null>(null);
  const [Selectedproduct,setSelectedproduct]=useState<Sdata|null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [totalproducts,settotalproducts]=useState<number>(0);
  const [currentPage,setcurrentpage]=useState(1);
  const [similarimages,setsimilarimages]=useState<string[]|null>(null)
  const navigate = useNavigate()
  const api = new productsapis()


  const getScraperProducts = async (scr: Scraper,PageNumber:number=1,PageSize:number=10) => {
    try {
      if(scr==null){
        return navigate('/Scrapers')
      }
      setIsLoading(true)
      setSelectedScraper(scr);
      const data = await api.getScraperProducts(scr.id,PageNumber,PageSize)
      setProducts(data)
      console.log(data,"for page =",PageNumber)
      settotalproducts(data.length)
      
    } catch (e) {
      console.error("Failed to fetch scraper products", e)
    } finally {
      setIsLoading(false)
    }
  }


  const getReviewProducts = async (PageNumber:number=1,PageSize:number=10) => {
    try {
      setIsLoading(true)
     
      const data = await api.getpendingreviewproducts(PageNumber,PageSize)
      setReviewProducts(data);
      console.log(data)
      settotalproducts(data.length)
      
    } catch (e) {
      console.error("Failed to fetch scraper products", e)
    } finally {
      setIsLoading(false)
    }
  }


  const Addselectedproduct=(data:Sdata)=>
  {
    setsimilarimages([""])
       setSelectedproduct(data);
  }

  const Setcurrentpage=(page:number)=>
  {
       setcurrentpage(page)
  }

  const GetSimilarImg = async (id: string) => {
    try {
      const r: string[] = await api.getsimilarimages(id,1);
      if (r && Array.isArray(r)) {
        setsimilarimages(r);
      }
    } catch {
      console.log("error getting similar images");
    }
  };

  const GetMoreSimilarImg=async (id:string,PageSize:number)=>{
    try {
      const r: string[] = await api.getsimilarimages(id,PageSize);
      if (r && Array.isArray(r)) {
        setsimilarimages(prev => {
          if (!prev) {

            return r;
          }

          return [...prev, ...r];
        });
      }
    } catch {
      console.log("error getting similar images");
    }
  }

   const Submit=async(id:string)=>{
      try{
        const re=await api.PushShopify(id)
        console.log(re)
        if(re)
        {
          navigate('/')
        }
      }
      catch{
        console.log("error getting similar images");
      }
   }

   const GetAiDescription=async(id:string)=>
   {
    try{
     const re=await api.AiDescription(id);
     return re;
    }
    catch{
      console.log("error getting ai description");
      return "";
    }
   }

   const UpdateProductDetails=async(productid:string,sku:string,price:number,title:string,description:string)=>
   {
    try{
      const re=await api.SumitDetails(productid,sku,price,title,description);
      return re;
     }
     catch{
       console.log("error getting ai description");
       return "";
     }
   }

   

 




  function normalizedate(d:string)
  {
    const re=d.split("T")[0];
    return re;
  }

  function Normalizetime(runtime:string)
  {
    const [hours, minutes, secondsWithMs] = runtime.split(":");
    const seconds = parseFloat(secondsWithMs);


 function pad(n:number) { return n.toString().padStart(2, "0"); }

   const h = pad(Number(hours));
   const m = pad(Number(minutes));
   const s = pad(Math.floor(seconds));
   return `${h}:${m}:${s}`

  }
  function normalizeDateTime(dateString:string) {
    const date = new Date(dateString);
  
   
    if (isNaN(date.getTime())) return "Invalid date";
  
    
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
  
    return `${year}-${month}-${day} ${hours}:${minutes}`;
  }




  return (
    <ProductContext.Provider value={{ products, isLoading, getScraperProducts,SelectedScraper,totalproducts,normalizedate,
     Normalizetime,normalizeDateTime,getReviewProducts,ReviewProducts,Addselectedproduct,Selectedproduct,
     currentPage,Setcurrentpage , similarimages ,GetSimilarImg,GetMoreSimilarImg,Submit,GetAiDescription,UpdateProductDetails
     }}>
      {children}
    </ProductContext.Provider>
  )
}










export function useProduct() {
  const context = useContext(ProductContext)
  if (context === undefined) {
    throw new Error("useProduct must be used within a ProductProvider")
  }
  return context
}
