import React, { useEffect,useState } from 'react'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {useProduct} from '@/contexts/products-context'
import { Input } from '@/components/ui/input';
import { Badge } from "@/components/ui/badge";
import Pagination from '@/components/ui/paginations'
import { useEditor } from '@tiptap/react';
import { Sdata } from '@/types/Sdata';

type Props = {}

function ScrapedProducts({ }: Props) {
  const {SelectedScraper,getScraperProducts,products,totalproducts}=useProduct()
  const [Pro,setpro]=useState<Sdata[]|null>()

  useEffect(()=>{
    if(products){
   setpro(products)
  }
  },[products])

  return (
    <div className="flex-1 space-y-4  p-4 sm:p-6 lg:p-8 pt-1">
     <h2 className="text-2xl sm:text-3xl font-bold tracking-tight">{SelectedScraper?.name}</h2>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-0">
         <div className="w-full h-full max-w-40  min-w-18 max-h-28 min-h-20 text-md sm:text-lg font-semibold   bg-gray-200 rounded-md flex flex-col items-center">
                  <div className=" flex-1 "> Total products </div>
                  <div className=" flex-1 "> {totalproducts}</div>
         </div> 
        <Input className=' w-[30vh] max-w-44 min-w-12' placeholder='Search product' />
      </div>
      <div className="rounded-md h-[60vh] overflow-auto border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Image</TableHead>
              <TableHead>Name</TableHead>
              <TableHead className="hidden sm:table-cell">Type</TableHead>
              <TableHead className="hidden sm:table-cell">Brand</TableHead>
              <TableHead className="hidden sm:table-cell">Status</TableHead>
              <TableHead>Sell Price</TableHead>
              <TableHead>Condition</TableHead>
              <TableHead className="hidden sm:table-cell">Stock</TableHead>
              {/* <TableHead>Actions</TableHead> */}
            </TableRow>
          </TableHeader>
          <TableBody>
            {products&&products.map((product) => (
              <TableRow key={product.id}>
                <TableCell>
                  <div className="h-14 w-14 sm:h-12 sm:w-12 overflow-hidden rounded-md">
                    <img
                      src={product.image[0].url}
                      alt={product.title}
                      className="h-full w-full object-cover"
                      loading="lazy"
                    />
                  </div>
                </TableCell>
                <TableCell>
                  <div>
                    <div className="font-medium">{product.title}</div>
                    <div className="text-sm text-muted-foreground hidden sm:block">{product.brand}</div>
                  </div>
                </TableCell>
                <TableCell className="hidden sm:table-cell">{product.productType}</TableCell>
                <TableCell className="hidden sm:table-cell">{product.brand}</TableCell>
                <TableCell className="hidden sm:table-cell">
                  <Badge variant={product.status === 'Categorized' ? 'default' : 'secondary'}>
                    {product.status}
                  </Badge>
                </TableCell>
                <TableCell>
                  <div className="font-medium">£{product.price.toFixed(2)}</div>
                  <div className="text-sm text-muted-foreground sm:hidden">{product.variants[0].inStock} in stock</div>
                </TableCell>
                <TableCell>
                  {product.condition}
                </TableCell>
                <TableCell className="hidden sm:table-cell">{product.variants[0].inStock==true?"in stock":"out of stock"}</TableCell>
                {/* <TableCell>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => navigate(`/products/edit/${product.id}`)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                </TableCell> */}
              </TableRow>
            ))}
          </TableBody>
        </Table>
     
      </div>
      <Pagination
          totalItems={100}
          itemsPerPage={10}
          onPageChange={(page:number)=>{
            if(SelectedScraper)
            getScraperProducts(SelectedScraper,page,10)
          }}
          className="mt-6 justify-center"
        />
    </div>
  )
}

export default ScrapedProducts