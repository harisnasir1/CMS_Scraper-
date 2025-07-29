import { api } from "@/lib/api"
import {Scraper} from "@/types/Scrappertype"
export class Scraperapis{
    
   async GetallScrappers()
   {
     const response=await api.get<[Scraper]>("/scraper/Getallscrapers");
     return response.data;
   }
}