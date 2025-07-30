import { useEffect } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { ClipboardCheck } from 'lucide-react';
import {useProduct} from '@/contexts/products-context'
import { Input } from '@/components/ui/input';
import { Button } from "@/components/ui/button";
import Pagination from '@/components/ui/paginations'
import { useNavigate } from "react-router-dom";
import { Sdata } from "@/types/Sdata";

const Reviewproducts = () => {
  const navigate=useNavigate()
  const {SelectedScraper,getReviewProducts,ReviewProducts,totalproducts,normalizeDateTime,Addselectedproduct}=useProduct()
  const getproducts=async()=>{
   await getReviewProducts(1,10);
  }
  useEffect(()=>{
  getproducts()
  },[])
  
 
  return (
    <div className="flex-1 space-y-4  p-4 sm:p-6 lg:p-8 pt-1">
     <h2 className="text-2xl sm:text-3xl font-bold tracking-tight flex gap-2">
      <div className=" flex items-center"><ClipboardCheck/></div>
      <div className="">Pending Review</div>
     </h2>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-0">
         <div className="w-full h-full max-w-40  min-w-18 max-h-28 min-h-20 text-md sm:text-lg font-semibold   bg-gray-200 rounded-md flex flex-col items-center">
                  <div className=" flex-1 "> Pending Review </div>
                  <div className=" flex-1 "> {totalproducts}</div>
         </div> 
        <Input className=' w-[30vh] max-w-44 min-w-12' placeholder='Search product' />
      </div>
      <div className="rounded-md h-[60vh] overflow-auto border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Image</TableHead>
              <TableHead>Brand Name</TableHead>
              <TableHead className="hidden sm:table-cell">Product Name</TableHead>
              <TableHead className="hidden sm:table-cell">Scraper</TableHead>
              <TableHead className="hidden sm:table-cell">Last Update</TableHead>
             
              <TableHead className="hidden sm:table-cell"></TableHead>
             
            </TableRow>
          </TableHeader>
          <TableBody>
            {ReviewProducts&&ReviewProducts.map((product:Sdata) => (
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
                {product.brand}
                </TableCell>
                <TableCell className="hidden sm:table-cell">{product.title}</TableCell>
                <TableCell className="hidden sm:table-cell">{product.scraperName}</TableCell>
                <TableCell className="hidden sm:table-cell">{normalizeDateTime(product.updatedAt)}</TableCell>
                 <TableCell ><Button 
                         onClick={()=>{
                              Addselectedproduct(product)
                              navigate("/Reviewproducts/Product")
                         }}
                         className='bg-[#1D7DBD] hover:bg-blue-500'> Review </Button> </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
     
      </div>
      <Pagination
          totalItems={100}
          itemsPerPage={10}
          onPageChange={(page:number)=>{
            
            getReviewProducts(page,10)
          }}
          className="mt-6 justify-center"
        />
    </div>
  )
}

export default Reviewproducts