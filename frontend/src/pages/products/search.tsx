import { useState } from "react"
import { usePageTitle } from "@/hooks/use-page-title"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Card,
  CardContent,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Slider } from "@/components/ui/slider"
import { Search, SlidersHorizontal } from "lucide-react"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"

interface Product {
  id: string
  name: string
  type: string
  brand: string
  status: 'draft' | 'published'
  price: number
  sellPrice: number
  stock: number
  image: string
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
]

const categories = ["All", "Shoes", "Clothing", "Accessories"]
const brands = ["All", "Urban Style", "Street Wear", "Luxe Collection", "Chic Fashion", "Time Pieces"]
const sortOptions = [
  { value: "price-asc", label: "Price: Low to High" },
  { value: "price-desc", label: "Price: High to Low" },
  { value: "name-asc", label: "Name: A to Z" },
  { value: "name-desc", label: "Name: Z to A" },
  { value: "margin-desc", label: "Highest Margin" },
]

export default function ProductSearchPage() {
  usePageTitle("Search Products")
  const [searchQuery, setSearchQuery] = useState("")
  const [selectedCategory, setSelectedCategory] = useState("All")
  const [selectedBrand, setSelectedBrand] = useState("All")
  const [selectedSort, setSelectedSort] = useState("price-asc")
  const [priceRange, setPriceRange] = useState([0, 500])

  // Filter and sort products
  const filteredProducts = products
    .filter(product => {
      const matchesSearch = product.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        product.brand.toLowerCase().includes(searchQuery.toLowerCase())
      const matchesCategory = selectedCategory === "All" || product.type === selectedCategory
      const matchesBrand = selectedBrand === "All" || product.brand === selectedBrand
      const matchesPrice = product.price >= priceRange[0] && product.price <= priceRange[1]
      return matchesSearch && matchesCategory && matchesBrand && matchesPrice
    })
    .sort((a, b) => {
      switch (selectedSort) {
        case "price-asc":
          return a.price - b.price
        case "price-desc":
          return b.price - a.price
        case "name-asc":
          return a.name.localeCompare(b.name)
        case "name-desc":
          return b.name.localeCompare(a.name)
        case "margin-desc":
          const marginA = ((a.sellPrice - a.price) / a.price) * 100
          const marginB = ((b.sellPrice - b.price) / b.price) * 100
          return marginB - marginA
        default:
          return 0
      }
    })

  return (
    <div className="flex-1 space-y-4 p-4 sm:p-8 pt-6">
      <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
        <h2 className="text-2xl sm:text-3xl font-bold tracking-tight">Search Products</h2>
        <div className="flex items-center gap-2 w-full sm:w-auto">
          <div className="relative flex-1 sm:flex-initial">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              type="search"
              placeholder="Search products..."
              className="pl-8 w-full sm:w-[300px]"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>
          <Sheet>
            <SheetTrigger asChild>
              <Button variant="outline" size="icon" className="shrink-0">
                <SlidersHorizontal className="h-4 w-4" />
              </Button>
            </SheetTrigger>
            <SheetContent>
              <SheetHeader>
                <SheetTitle>Filters</SheetTitle>
              </SheetHeader>
              <div className="space-y-4 py-4">
                <Accordion type="single" collapsible className="w-full">
                  <AccordionItem value="category">
                    <AccordionTrigger>Category</AccordionTrigger>
                    <AccordionContent>
                      <div className="space-y-2">
                        {categories.map((category) => (
                          <Button
                            key={category}
                            variant={selectedCategory === category ? "default" : "outline"}
                            className="w-full justify-start"
                            onClick={() => setSelectedCategory(category)}
                          >
                            {category}
                          </Button>
                        ))}
                      </div>
                    </AccordionContent>
                  </AccordionItem>
                  <AccordionItem value="brand">
                    <AccordionTrigger>Brand</AccordionTrigger>
                    <AccordionContent>
                      <div className="space-y-2">
                        {brands.map((brand) => (
                          <Button
                            key={brand}
                            variant={selectedBrand === brand ? "default" : "outline"}
                            className="w-full justify-start"
                            onClick={() => setSelectedBrand(brand)}
                          >
                            {brand}
                          </Button>
                        ))}
                      </div>
                    </AccordionContent>
                  </AccordionItem>
                  <AccordionItem value="price">
                    <AccordionTrigger>Price Range</AccordionTrigger>
                    <AccordionContent>
                      <div className="space-y-4">
                        <Slider
                          min={0}
                          max={500}
                          step={10}
                          value={priceRange}
                          onValueChange={setPriceRange}
                        />
                        <div className="flex items-center justify-between">
                          <span>£{priceRange[0]}</span>
                          <span>£{priceRange[1]}</span>
                        </div>
                      </div>
                    </AccordionContent>
                  </AccordionItem>
                </Accordion>
              </div>
            </SheetContent>
          </Sheet>
          <Select value={selectedSort} onValueChange={setSelectedSort}>
            <SelectTrigger className="w-[160px] hidden sm:flex">
              <SelectValue placeholder="Sort by" />
            </SelectTrigger>
            <SelectContent>
              {sortOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {filteredProducts.map((product) => (
          <Card key={product.id} className="overflow-hidden">
            <CardHeader className="p-0">
              <div className="aspect-square overflow-hidden">
                <img
                  src={product.image}
                  alt={product.name}
                  className="h-full w-full object-cover transition-transform hover:scale-105"
                />
              </div>
            </CardHeader>
            <CardContent className="space-y-2 p-4">
              <div className="flex items-start justify-between">
                <CardTitle className="line-clamp-2 text-base">{product.name}</CardTitle>
                <Badge variant={product.status === 'published' ? 'default' : 'secondary'} className="ml-2 shrink-0">
                  {product.status}
                </Badge>
              </div>
              <div className="text-sm text-muted-foreground">{product.brand}</div>
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-lg font-bold">£{product.sellPrice.toFixed(2)}</div>
                  <div className="text-sm text-muted-foreground line-through">£{product.price.toFixed(2)}</div>
                </div>
                <div className="text-sm text-green-600 font-medium">
                  {((product.sellPrice - product.price) / product.price * 100).toFixed(1)}% margin
                </div>
              </div>
            </CardContent>
            <CardFooter className="p-4 pt-0">
              <div className="text-sm text-muted-foreground">{product.stock} in stock</div>
            </CardFooter>
          </Card>
        ))}
      </div>

      {filteredProducts.length === 0 && (
        <div className="text-center py-12">
          <h3 className="text-lg font-semibold">No products found</h3>
          <p className="text-muted-foreground">Try adjusting your search or filters</p>
        </div>
      )}
    </div>
  )
}
