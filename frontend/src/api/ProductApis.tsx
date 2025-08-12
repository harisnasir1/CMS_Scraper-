import { api } from "@/lib/api";
import { ISelectedImgs, Sdata } from "@/types/Sdata";

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
    async getlivefeedproducts(PageNumber:Number,PageSize:Number)
    {
        const response=await api.post<Sdata[]>("Product/Livefeed",{PageNumber,PageSize});
        console.log(response.data)
        return response.data;
    }
    async getsimilarimages( productid:string,PageSize:number)
    {
      const response=await api.post<string[]>("Product/Similarimages",{productid:productid,page:PageSize});
      return response.data
    }
    async PushShopify(productid:string,images:ISelectedImgs[])
    {
  
        const response=await api.post<string[]>("Product/Push",{id:productid,productimage:images});
        console.log(response)
        return response.data
    }
    async AiDescription(productid:string)
    {
        const response=await api.post<string>("Product/AiDescription",{productid:productid});
        return response.data
    }
    async SumitDetails(productid:string,sku:string,price:number,title:string,description:string)
    {
        const response=await api.post<string>("Product/SaveDetails",{productid:productid,sku,description,title,price});
        return response.data
    }
    async GetProductCount(status:string)
    {
        const response=await api.post<number>("Product/GetCount",{status:status});
        return response.data
    }
}
