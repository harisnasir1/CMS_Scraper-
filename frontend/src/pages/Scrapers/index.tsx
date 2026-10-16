import  { useEffect, useState } from 'react'
import axios from 'axios';
import toast from 'react-hot-toast';
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
  import { useNavigate } from 'react-router-dom';
  import { productsapis } from '@/api/ProductApis';
  import { Scraper } from '@/types/Scrappertype';

const api = new productsapis()

const Scraperspage = () => {
  const [runningId,setRunningId]=useState<string|null>(null);
  const [queuedId,setQueuedId]=useState<string|null>(null);

  const RunAutoApprove=async(scrap:Scraper)=>{
    if(!window.confirm(`Auto-approve up to 100 products for ${scrap.name} now?`)) return;
    setRunningId(scrap.id)
    try{
      await api.runAutoApprove(scrap.id)
      toast.success("Auto-approve queued — progress is in the server logs.")
      setQueuedId(scrap.id)
      setTimeout(()=>setQueuedId(q=>q===scrap.id?null:q),3000)
    }
    catch(e){
      const body=axios.isAxiosError(e)?e.response?.data:undefined;
      toast.error(typeof body==="string"&&body?body:"Failed to start auto-approve, please try again.")
    }
    finally{
      setRunningId(null)
    }
  }

  usePageTitle('Scrapers');
  const navigate=useNavigate();
  const {getScrappers}=useScrapper();
  const {scrappers}=useScrapper();
  const {getScraperProducts,normalizeDateTime,Normalizetime}=useProduct();
  console.log(scrappers)
  useEffect(()=>{
    const Getall=async()=>{
      await getScrappers();
    }
    Getall()
  },[])

 

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
                         <TableCell>{normalizeDateTime(scrap.lastrun.toString())}</TableCell>
                         <TableCell>{Normalizetime(scrap.runtime)}</TableCell>
                         <TableCell className=' text-right'>
                          <div className='flex justify-end gap-2'>
                         <Button
                         variant="secondary"
                         disabled={scrap.status!=="active"||runningId===scrap.id||queuedId===scrap.id}
                         onClick={()=>RunAutoApprove(scrap)}
                         >{runningId===scrap.id?"Running…":queuedId===scrap.id?"Queued…":"Run auto-approve"}</Button>
                         <Button
                         onClick={()=>{
                          getScraperProducts(scrap,1,10)
                          navigate(`/Scrapers/products/`)
                        }
                         }
                         className='bg-blue-700 hover:bg-blue-500'>View products </Button>
                          </div>
                         </TableCell>
                    </TableRow>
                ))
            }
          </TableBody>
        </Table>
        </div>
  )
}

export default Scraperspage