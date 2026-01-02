import { createContext, useContext, useState } from "react"

import {Shopapis } from '@/api/ShopApis'
import { shops } from "@/types/Shoptypes"

interface ShopContextType {
  Shops: shops[] | null
  isLoading:boolean
  getShops: () => Promise<void>
  SyncShop:(storeid:string)=>Promise<void|null>
}


const ShopContext = createContext<ShopContextType | undefined>(undefined)

export function ShopProvider({ children }: { children: React.ReactNode }) 
{
 const [Shops,setshops]=useState<shops[]|null>(null);
const [isLoading, setIsLoading] = useState<boolean>(true)
const shopapi=new Shopapis()
   const getShops = async () => {
    try {
      setIsLoading(true)
      const result = await shopapi.GetallStores();
      setshops(result)
    } catch (e) {
      console.error("Error getting scrappers", e)
    } finally {
      setIsLoading(false)
    }
  }

  const SyncShop=async(storeid:string)=>{
  
    try{
        if(!storeid) return null
         const result= await  shopapi.SyncStroe(storeid);
          if(!result) return null
    }
    catch (e) {
      console.error("Error getting scrappers", e)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <ShopContext.Provider value={{ Shops, getShops,isLoading,SyncShop }}>
      {children}
    </ShopContext.Provider>
  )

}
export function useShop() {
  const context = useContext(ShopContext)
  if (context === undefined) {
    throw new Error("useScrapper must be used within a ScrapperProvider")
  }
  return context
}
