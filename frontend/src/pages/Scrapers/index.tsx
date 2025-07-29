import React, { useEffect } from 'react'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
  } from "@/components/ui/table";
  import { Button } from "@/components/ui/button";
  import { Badge } from "@/components/ui/badge";
  import { usePageTitle } from "@/hooks/use-page-title";
  import {useScrapper} from "@/contexts/Scrapper-context"
  import { useProduct } from '@/contexts/products-context';

  const Scrapers=[{
    Name:"Savonches",
    Status:"Active",
    Lastrun: Date.now(),
    runtime:"100min"
  }]

const Scraperspage = () => {
  usePageTitle('Scrapers');
  const {getScrappers}=useScrapper();
  const {scrappers}=useScrapper();
  const {getScraperProducts}=useProduct();
  
  useEffect(()=>{
    const Getall=async()=>{
      await getScrappers();
    }
    Getall()
  },[])

  function normalizedate(d:string)
  {
    const re=d.split("T")[0];
    return re;
  }

  function Normalizetime(runtime:string)
  {
    const [hours, minutes, secondsWithMs] = runtime.split(":");
const seconds = parseFloat(secondsWithMs);


 function pad(n:number) { return n.toString().padStart(2, "0"); }

const h = pad(Number(hours));
const m = pad(Number(minutes));
const s = pad(Math.floor(seconds));
return `${h}:${m}:${s}`

  }

  return (
    <div className="flex-1 space-y-4  p-4 sm:p-6 lg:p-8 pt-6">
             <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-0">
        <h2 className="text-2xl sm:text-3xl font-bold tracking-tight">Scrapers</h2>
      </div>
      <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Lastrun</TableHead>
              <TableHead> run time</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {
             scrappers&&   scrappers.map((scrap,index)=>(
                    <TableRow key={index}>
                        <TableCell>{scrap.name}</TableCell>
                        <TableCell>
                             <Badge
                               variant={scrap.status === 'active' ? 'Idel' : 'Running'}
                             >
                               {scrap.status}
                             </Badge>
                         </TableCell>
                         <TableCell>{normalizedate(scrap.lastrun.toString())}</TableCell>
                         <TableCell>{Normalizetime(scrap.runtime)}</TableCell>
                         <TableCell className=' text-right'>{<Button 
                         onClick={()=>getScraperProducts(scrap)}
                         className='bg-blue-700 hover:bg-blue-500'>View products </Button>}</TableCell>
                    </TableRow>
                ))
            }
          </TableBody>
        </Table>
        </div>
  )
}

export default Scraperspage