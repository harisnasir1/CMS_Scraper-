import { api } from "@/lib/api";
import { Sdata } from "@/types/Sdata";

export class productsapis{

    async getScraperProducts(id:string,PageNumber:number,PageSize:number)
    {
        const response=await api.post<Sdata[]>("Product/Readytoreview",{ScraperId:id,PageNumber,PageSize});
        return response.data;
    }
    async getpendingreviewproducts(PageNumber:Number,PageSize:Number)
    {
        const response=await api.post<Sdata[]>("Product/pendingreview",{PageNumber,PageSize});
        return response.data;
    }
}