// src/context/ScrapperContext.tsx
import { createContext, useContext, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { Scraperapis } from '@/api/Scrapperapis'
import { Scraper } from "@/types/Scrappertype"

interface ScrapperContextType {
  scrappers: Scraper[] | null
  isLoading: boolean
  getScrappers: () => Promise<void>
}

const ScrapperContext = createContext<ScrapperContextType | undefined>(undefined)

export function ScrapperProvider({ children }: { children: React.ReactNode }) 
{
  const [scrappers, setScrappers] = useState<Scraper[] | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const navigate = useNavigate()
  const scrapis = new Scraperapis()

  const getScrappers = async () => {
    try {
      setIsLoading(true)
      const result = await scrapis.GetallScrappers()
      setScrappers(result)
    } catch (e) {
      console.error("Error getting scrappers", e)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    getScrappers()
  }, [])

  return (
    <ScrapperContext.Provider value={{ scrappers, isLoading, getScrappers }}>
      {children}
    </ScrapperContext.Provider>
  )
}

export function useScrapper() {
  const context = useContext(ScrapperContext)
  if (context === undefined) {
    throw new Error("useScrapper must be used within a ScrapperProvider")
  }
  return context
}
