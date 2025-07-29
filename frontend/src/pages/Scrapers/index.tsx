import React from 'react'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
  } from "@/components/ui/table";
  import { Badge } from "@/components/ui/badge";
  const Scrapers=[{
    Name:"Savonches",
    Status:"Active",
    Lastrun: Date.now(),
    runtime:"100min"
  }]

const Scraperspage = () => {
  return (
    <div className="flex-1 space-y-4  p-4 sm:p-6 lg:p-8 pt-6">
             <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-0">
        <h2 className="text-2xl sm:text-3xl font-bold tracking-tight">Scrappers</h2>
      </div>
      <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Lastrun</TableHead>
              <TableHead>last run time</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {
                Scrapers.map((scrap,index)=>(
                    <TableRow key={index}>
                        <TableCell>{scrap.Name}</TableCell>
                        <TableCell>
                  <Badge
                    variant={scrap.Status === 'Active' ? 'Running' : 'destructive'}
                  >
                    {scrap.Status}
                  </Badge>
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