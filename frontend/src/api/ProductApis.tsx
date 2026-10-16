import { api } from "@/lib/api";
import { DeleteProductResult, ISelectedImgs, Sdata } from "@/types/Sdata";

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
    async runAutoApprove(scraperId:string,limit:number=100)
    {
        const response=await api.post<{message:string;scraperId:string;limit:number}>("Product/Sync_non_live_inventory",{ScraperId:scraperId,PageNumber:0,PageSize:limit});
        return response.data
    }
    // non-2xx is thrown; the caller reads the body from err.response.data
    async deleteLiveProduct(productid:string)
    {
        const response=await api.delete<DeleteProductResult>("Product/"+productid);
        return response.data
    }
}
