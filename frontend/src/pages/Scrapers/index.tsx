import  { useEffect } from 'react'
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
  

const Scraperspage = () => {
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
                         <TableCell className=' text-right'>{<Button 
                         onClick={()=>{
                          getScraperProducts(scrap,1,10)
                          navigate(`/Scrapers/products/`)
                        }
                         }
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