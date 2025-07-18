import { usePageTitle } from "@/hooks/use-page-title";
import { Button } from "@/components/ui/button";
import { useNavigate } from "react-router-dom";
import { Edit, Plus, Search } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";

interface Product {
  id: string;
  name: string;
  type: string;
  brand: string;
  status: 'draft' | 'published';
  price: number;
  sellPrice: number;
  stock: number;
  image: string;
}

// Sample data - replace with actual API call
const products: Product[] = [
  {
    id: '1',
    name: 'Classic White Sneakers',
    type: 'Shoes',
    brand: 'Urban Style',
    status: 'published',
    price: 89.99,
    sellPrice: 129.99,
    stock: 45,
    image: 'https://images.unsplash.com/photo-1600269452121-4f2416e55c28?w=800&dpr=2&q=80'
  },
  {
    id: '2',
    name: 'Denim Jacket',
    type: 'Clothing',
    brand: 'Street Wear',
    status: 'published',
    price: 129.99,
    sellPrice: 179.99,
    stock: 30,
    image: 'https://images.unsplash.com/photo-1576995853123-5a10305d93c0?w=800&dpr=2&q=80'
  },
  {
    id: '3',
    name: 'Leather Crossbody Bag',
    type: 'Accessories',
    brand: 'Luxe Collection',
    status: 'draft',
    price: 199.99,
    sellPrice: 299.99,
    stock: 15,
    image: 'https://images.unsplash.com/photo-1548036328-c9fa89d128fa?w=800&dpr=2&q=80'
  },
  {
    id: '4',
    name: 'Summer Floral Dress',
    type: 'Clothing',
    brand: 'Chic Fashion',
    status: 'published',
    price: 79.99,
    sellPrice: 129.99,
    stock: 25,
    image: 'https://images.unsplash.com/photo-1515372039744-b8f02a3ae446?w=800&dpr=2&q=80'
  },
  {
    id: '5',
    name: 'Vintage Watch',
    type: 'Accessories',
    brand: 'Time Pieces',
    status: 'draft',
    price: 299.99,
    sellPrice: 449.99,
    stock: 5,
    image: 'https://images.unsplash.com/photo-1524592094714-0f0654e20314?w=800&dpr=2&q=80'
  }
];

export default function ProductsPage() {
  usePageTitle('Products');
  const navigate = useNavigate();

  return (
    <div className="flex-1 space-y-4 p-4 sm:p-8 pt-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <h2 className="text-2xl sm:text-3xl font-bold tracking-tight">Products</h2>
        <div className="flex gap-2 w-full sm:w-auto">
          <Button
            variant="outline"
            onClick={() => navigate('/products/search')}
            className="w-full sm:w-auto justify-center"
          >
            <Search className="mr-2 h-4 w-4" />
            Search
          </Button>
          <Button
            onClick={() => navigate('/products/create')}
            className="w-full sm:w-auto justify-center"
          >
            <Plus className="mr-2 h-4 w-4" />
            Add Product
          </Button>
        </div>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Image</TableHead>
              <TableHead>Name</TableHead>
              <TableHead className="hidden sm:table-cell">Type</TableHead>
              <TableHead className="hidden sm:table-cell">Brand</TableHead>
              <TableHead className="hidden sm:table-cell">Status</TableHead>
              <TableHead>RRP</TableHead>
              <TableHead>Sell Price</TableHead>
              <TableHead className="hidden sm:table-cell">Stock</TableHead>
              <TableHead>Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {products.map((product) => (
              <TableRow key={product.id}>
                <TableCell>
                  <div className="h-14 w-14 sm:h-12 sm:w-12 overflow-hidden rounded-md">
                    <img
                      src={product.image}
                      alt={product.name}
                      className="h-full w-full object-cover"
                      loading="lazy"
                    />
                  </div>
                </TableCell>
                <TableCell>
                  <div>
                    <div className="font-medium">{product.name}</div>
                    <div className="text-sm text-muted-foreground hidden sm:block">{product.brand}</div>
                  </div>
                </TableCell>
                <TableCell className="hidden sm:table-cell">{product.type}</TableCell>
                <TableCell className="hidden sm:table-cell">{product.brand}</TableCell>
                <TableCell className="hidden sm:table-cell">
                  <Badge variant={product.status === 'published' ? 'default' : 'secondary'}>
                    {product.status}
                  </Badge>
                </TableCell>
                <TableCell>
                  <div className="font-medium">£{product.price.toFixed(2)}</div>
                  <div className="text-sm text-muted-foreground sm:hidden">{product.stock} in stock</div>
                </TableCell>
                <TableCell>
                  <div className="font-medium text-green-600">£{product.sellPrice.toFixed(2)}</div>
                  <div className="text-xs text-muted-foreground">
                    {((product.sellPrice - product.price) / product.price * 100).toFixed(1)}% margin
                  </div>
                </TableCell>
                <TableCell className="hidden sm:table-cell">{product.stock}</TableCell>
                <TableCell>
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => navigate(`/products/edit/${product.id}`)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
