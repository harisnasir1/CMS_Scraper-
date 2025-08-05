// src/types/SdataType.ts

export interface ProductImageRecord {
    url: string
    Priority:number
    altText?: string
  }
  
  export interface ProductVariantRecord {
    size: string
    price: number
    inStock: boolean
  }
  
  export interface Sdata {
    id: string
    uid: string
    sid: string
    title: string
    brand: string
    image: ProductImageRecord[]
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