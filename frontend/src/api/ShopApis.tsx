import { api } from "@/lib/api"
import {shops} from "@/types/Shoptypes"
export class Shopapis{
    
   async GetallStores()
   {
     const response=await api.get<[shops]>("/Shop/Get_Stores");
     return response.data;
   }
   async SyncStroe(storeid:string)
   {
    const response=await api.post<[string]>("/Product/Sync_inventory",{storeid:storeid})
    return response.data;
   }
}