
import { createContext, useContext, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { productsapis } from "@/api/ProductApis"
import { Sdata } from "@/types/Sdata"
import { Scraper } from "@/types/Scrappertype"

interface ProductContextType {
  products: Sdata[] | null
  isLoading: boolean
  SelectedScraper:Scraper|null,
  totalproducts:number|null
  getScraperProducts: (scr:Scraper,PageNumber:number,PageSize:number) => Promise<void>
}

const ProductContext = createContext<ProductContextType | undefined>(undefined)

export function ProductProvider({ children }: { children: React.ReactNode }) {
  const [products, setProducts] = useState<Sdata[] | null>(null)
  const [SelectedScraper,setSelectedScraper]=useState<Scraper|null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false)
  const [totalproducts,settotalproducts]=useState<number>(0);
  const navigate = useNavigate()
  const api = new productsapis()

  useEffect(()=>{
    if(SelectedScraper==null)
    navigate('/Scrapers')
  },[])

  const getScraperProducts = async (scr: Scraper,PageNumber:number=1,PageSize:number=10) => {
    try {
      setIsLoading(true)
      setSelectedScraper(scr);
      const data = await api.getScraperProducts(scr.id,PageNumber,PageSize)
      setProducts(data)
      console.log(data,"for page =",PageNumber)
      settotalproducts(data.length)
      navigate(`/Scrapers/products/`)
    } catch (e) {
      console.error("Failed to fetch scraper products", e)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <ProductContext.Provider value={{ products, isLoading, getScraperProducts,SelectedScraper,totalproducts }}>
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
