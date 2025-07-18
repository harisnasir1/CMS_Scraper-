import { useState } from 'react';
import useEmblaCarousel from 'embla-carousel-react';
import { ChevronLeftIcon, ChevronRightIcon, MagnifyingGlassIcon } from '@radix-ui/react-icons';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { usePageTitle } from "@/hooks/use-page-title";


// Sample data for best-selling products
const bestSellingProducts = [
  {
    id: 1,
    name: "Air Jordan 4 Retro SE GS \"Paris\"",
    brand: 'Fashion Brand',
    price: 89.99,
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fcdn.kickgame.com%2F9e3a561e-507b-48fd-ace2-ad35bf78fbcb%2F-%2Fpreview%2F200x200%2FHM8965001.png&w=96&q=75',
    sold: 1234,
  },
  {
    id: 2,
    name: 'Premium Jeans',
    brand: 'Denim Co',
    price: 199.99,
    image: 'https://placehold.co/300x400',
    sold: 982,
  },
  {
    id: 3,
    name: 'Leather Jacket',
    brand: 'Urban Style',
    price: 299.99,
    image: 'https://placehold.co/300x400',
    sold: 756,
  },
  {
    id: 4,
    name: 'Running Shoes',
    brand: 'Sport Elite',
    price: 129.99,
    image: 'https://placehold.co/300x400',
    sold: 1542,
  },
  {
    id: 5,
    name: 'Summer Dress',
    brand: 'Elegance',
    price: 159.99,
    image: 'https://placehold.co/300x400',
    sold: 892,
  },
];

export default function SellPage() {
  usePageTitle('Sell');

  const [search, setSearch] = useState('');
  const [emblaRef, emblaApi] = useEmblaCarousel({
    align: 'start',
    loop: true,
  });

  const scrollPrev = () => {
    if (emblaApi) emblaApi.scrollPrev();
  };

  const scrollNext = () => {
    if (emblaApi) emblaApi.scrollNext();
  };

  return (
    <div className="space-y-6">
      {/* Search Section */}
      <div className="flex gap-4 items-center">
        <div className="relative flex-1">
          <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500 h-4 w-4" />
          <Input
            type="text"
            placeholder="Search for fashion products..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
        <Button>Search</Button>
      </div>

      {/* Best Sellers Carousel */}
      <div className="space-y-4">
        <h2 className="text-xl font-semibold">Best Sellers</h2>
        <div className="relative">
          <div className="overflow-hidden" ref={emblaRef}>
            <div className="flex gap-6">
              {bestSellingProducts.map((product) => (
                <div
                  key={product.id}
                  className="flex-[0_0_250px] min-w-0"
                >
                  <div className="rounded-lg overflow-hidden border bg-card">
                    <img
                      src={product.image}
                      alt={product.name}
                      className="w-full h-48 object-cover"
                    />
                    <div className="p-4 space-y-2">
                      <h3 className="font-medium line-clamp-1">{product.name}</h3>
                      <p className="text-sm text-muted-foreground">{product.brand}</p>
                      <div className="flex justify-between items-center">
                        <span className="font-semibold">${product.price}</span>
                        <span className="text-sm text-muted-foreground">{product.sold} sold</span>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
          <Button
            variant="outline"
            size="icon"
            className="absolute left-4 top-1/2 -translate-y-1/2 rounded-full"
            onClick={scrollPrev}
          >
            <ChevronLeftIcon className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="icon"
            className="absolute right-4 top-1/2 -translate-y-1/2 rounded-full"
            onClick={scrollNext}
          >
            <ChevronRightIcon className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
