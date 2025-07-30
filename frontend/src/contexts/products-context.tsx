
import { createContext, useContext, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { productsapis } from "@/api/ProductApis"
import { Sdata } from "@/types/Sdata"
import { Scraper } from "@/types/Scrappertype"

interface ProductContextType {
  products: Sdata[] | null
  ReviewProducts:Sdata[] | null
  isLoading: boolean
  SelectedScraper:Scraper|null,
  Selectedproduct:Sdata|null
  totalproducts:number|null
  getScraperProducts: (scr:Scraper,PageNumber:number,PageSize:number) => Promise<void>
  normalizedate:(d:string)=>string
  Normalizetime:(runtime:string)=>string
  normalizeDateTime:(dateString:string)=>string
  getReviewProducts : (PageNumber:number,PageSize:number)=>Promise<void>
  Addselectedproduct:(data:Sdata)=>void
}

const ProductContext = createContext<ProductContextType | undefined>(undefined)

export function ProductProvider({ children }: { children: React.ReactNode }) {
  const [products, setProducts] = useState<Sdata[] | null>(null)
  const [ReviewProducts,setReviewProducts]=useState<Sdata[]|null>(null);
  const [SelectedScraper,setSelectedScraper]=useState<Scraper|null>(null);
  const [Selectedproduct,setSelectedproduct]=useState<Sdata|null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [totalproducts,settotalproducts]=useState<number>(0);
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
      settotalproducts(data.length)
      
    } catch (e) {
      console.error("Failed to fetch scraper products", e)
    } finally {
      setIsLoading(false)
    }
  }


  const Addselectedproduct=(data:Sdata)=>
  {
       setSelectedproduct(data);
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
    <ProductContext.Provider value={{ products, isLoading, getScraperProducts,SelectedScraper,totalproducts,normalizedate, Normalizetime,normalizeDateTime,getReviewProducts,ReviewProducts,Addselectedproduct,Selectedproduct}}>
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
