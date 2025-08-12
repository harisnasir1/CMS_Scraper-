// src/types/SdataType.ts

export interface ProductImageRecord {
    id:string,
    url: string
    Priority:number
    altText?: string
  }
  
  export interface ProductVariantRecord {
    size: string
    price: number
    inStock: boolean
    sku:string
  }
  
  export interface Sdata {
    id: string
    uid: string
    sid: string
    title: string
    brand: string
    image: ProductImageRecord[],
    sku:string,
    description: string
    productUrl: string
    price: number
    category: string
    productType: string
    gender: string
    scraperName: string
    status?: string
    statusDulicateId?: string
    duplicateSource?: string
    condition: string
    enriched: boolean
    variants: ProductVariantRecord[]
    hashimg?: string
    createdAt: string
    updatedAt: string
  }
  export interface ISelectedImgs{
    Id:string,
    Url:string,
    Priority:number,
    Bgremove:boolean,
  }