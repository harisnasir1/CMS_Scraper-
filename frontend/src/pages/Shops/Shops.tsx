import { useEffect } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Store, RefreshCw } from 'lucide-react';
import { Button } from "@/components/ui/button";
import { useShop } from '@/contexts/Shop-context';
import { shops } from "@/types/Shoptypes";

const Shops = () => {
  const { Shops, getShops, isLoading, SyncShop } = useShop();

  useEffect(() => {
    getShops();
  }, []);

  const handleSync = async (storeId: string) => {
    await SyncShop(storeId);
    // Refresh the shops list after sync
    await getShops();
  };

  return (
    <div className="flex-1 space-y-4 p-4 sm:p-6 lg:p-8 pt-1">
      <h2 className="text-2xl sm:text-3xl font-bold tracking-tight flex gap-2">
        <div className="flex items-center">
          <Store />
        </div>
        <div>Synced Stores</div>
      </h2>

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 sm:gap-0">
        <div className="w-full h-full max-w-40 min-w-18 max-h-28 min-h-20 text-md sm:text-lg font-semibold bg-gray-200 rounded-md flex flex-col items-center justify-center">
          <div className="flex-1 flex items-center">Total Stores</div>
          <div className="flex-1 flex items-center text-2xl">{Shops?.length || 0}</div>
        </div>
      </div>

      <div className="rounded-md h-[60vh] overflow-auto border bg-card">
        {isLoading ? (
          <div className="flex items-center justify-center h-full">
            <RefreshCw className="animate-spin h-8 w-8 text-gray-400" />
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Store Name</TableHead>
                <TableHead className="hidden sm:table-cell">Store ID</TableHead>
                <TableHead>Products Out of Sync</TableHead>
                <TableHead className="text-right">Action</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {Shops && Shops.length > 0 ? (
                Shops.map((shop: shops) => (
                  <TableRow key={shop.store_id}>
                    <TableCell className="font-medium">
                      {shop.store_name}
                    </TableCell>
                    <TableCell className="hidden sm:table-cell text-gray-500">
                      {shop.store_id}
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <span
                          className={`px-3 py-1 rounded-full text-sm font-semibold ${
                            shop.store_out_of_sync > 0
                              ? "bg-red-100 text-red-700"
                              : "bg-green-100 text-green-700"
                          }`}
                        >
                          {shop.store_out_of_sync}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      {shop.store_out_of_sync > 0 ? (
                        <Button
                          onClick={() => handleSync(shop.store_id)}
                          className="bg-[#1D7DBD] hover:bg-blue-500"
                        >
                          <RefreshCw className="mr-2 h-4 w-4" />
                          Sync
                        </Button>
                      ) : (
                        <Button
                          disabled
                          variant="outline"
                          className="cursor-not-allowed"
                        >
                          In Sync
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={4} className="text-center text-gray-500 py-8">
                    No stores found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        )}
      </div>
    </div>
  );
};

export default Shops;