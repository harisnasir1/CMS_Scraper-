import { useState, useEffect } from 'react';
import { MagnifyingGlassIcon } from '@radix-ui/react-icons';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { usePageTitle } from "@/hooks/use-page-title";
import {
  Carousel,
  CarouselContent,
  CarouselItem,
  CarouselNext,
  CarouselPrevious,
} from "@/components/ui/carousel";


// Sample data for best-selling products
const bestSellingProducts = [
  {
    id: 1,
    name: "Air Jordan 4 Retro SE GS \"Paris\"",
    sku: 'HM8965001',
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fcdn.kickgame.com%2F9e3a561e-507b-48fd-ace2-ad35bf78fbcb%2F-%2Fpreview%2F200x200%2FHM8965001.png&w=96&q=75',
  },
  {
    id: 2,
    name: 'adidas Yeezy Slide \"Onyx\"',
    sku: 'DN2023001',
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fucarecdn.com%2F0fdea69e-8dcc-4a0e-a570-b7d198b84a60%2F-%2Fpreview%2F200x200%2FadidasYeezySlidesOnyx.png&w=128&q=75',
  },
  {
    id: 3,
    name: 'Supreme x Nike Air Force 1 Low \'Box Logo - Black\'',
    sku: 'US2023002',
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fcdn.kickgame.com%2F97ca9f98-b9cc-4f60-b3a7-a0eb570de420%2F-%2Fpreview%2F200x200%2Fcu9225001.png&w=128&q=75',
  },
  {
    id: 4,
    name: 'Nike Dunk High GS \'Black White\'',
    sku: 'SE2023003',
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fcdn.kickgame.com%2F16f58148-44b3-4bbe-9c9f-c8edc1fe3496%2F-%2Fpreview%2F200x200%2FDB2179103.png&w=128&q=75',
  },
  {
    id: 5,
    name: 'Fear of God Essentials Hoodie \'Light Oatmeal\' (SS22)',
    brand: 'Elegance',
    price: 129.99,
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fucarecdn.com%2F484751c5-d310-45f2-b79d-2052f06fa835%2F-%2Fpreview%2F200x200%2Ffearofgodessentialshoodiess22lightoatmeal_1.png&w=128&q=75',
    sold: 892,
  },
  {
    id: 6,
    name: 'KAWS x Warhol UNIQLO UT Graphic T-Shirt \'White Blue\'',
    brand: 'Elegance',
    price: 129.99,
    image: 'https://sell.kickgame.com/_next/image?url=https%3A%2F%2Fcdn.kickgame.com%2Fc5c3c0b4-d089-438d-a39c-069789f62af1%2F-%2Fpreview%2F200x200%2FKAWSUNIQLO471321.png&w=128&q=75',
    sold: 892,
  }
];

export default function SellPage() {
  usePageTitle('Sell');
  const [search, setSearch] = useState('');
  const [scrollProgress, setScrollProgress] = useState(0);
  const [api, setApi] = useState<any>(null);

  useEffect(() => {
    if (!api) return;

    const onScroll = () => {
      if (!api) return;

      const container = api.rootNode();
      const scrollContainer = api.containerNode();
      const containerWidth = container.clientWidth;
      const scrollWidth = scrollContainer.scrollWidth;
      const scrollLeft = Math.abs(api.scrollOffset());
      const maxScroll = scrollWidth - containerWidth;
      
      // Calculate progress and scale to available space (75%)
      const rawProgress = maxScroll > 0 ? scrollLeft / maxScroll : 0;
      const position = rawProgress * 75; // 75 is the available space (100% - 25% thumb width)
      
      console.log({
        containerWidth,
        scrollWidth,
        scrollLeft,
        maxScroll,
        rawProgress,
        position
      });
      
      setScrollProgress(position);
    };

    api.on('scroll', onScroll);
    api.on('reInit', onScroll);

    return () => {
      api.off('scroll', onScroll);
      api.off('reInit', onScroll);
    };
  }, [api]);


  return (
    <div className="flex-1 min-h-full flex flex-col">
      <div className="container mx-auto space-y-8 sm:space-y-12 pt-8 sm:pt-12">
        {/* Search Section */}
        <div className="px-4 sm:px-6 max-w-4xl mx-auto space-y-6 sm:space-y-8">
          <div className="space-y-2 sm:space-y-3">
            <h1 className="text-2xl sm:text-3xl font-semibold">Sell items</h1>
            <p className="text-base sm:text-lg text-muted-foreground">What are you selling today?</p>
          </div>
          <div className="relative">
            <MagnifyingGlassIcon className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-500 h-5 w-5" />
            <Input
              type="text"
              placeholder="Tell us what you'd like to sell..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-11 h-12 sm:h-14 text-base w-full"
            />
          </div>
        </div>

        {/* Best Sellers Carousel */}
        <div className="space-y-4 sm:space-y-6 max-w-4xl mx-auto px-4 sm:px-6">
          <h2 className="text-lg sm:text-xl font-semibold">Best Sellers</h2>
          <div className="relative">
            <div className="absolute -bottom-6 left-0 right-0 h-3 bg-secondary/10 rounded-full overflow-hidden shadow-sm">
              <div 
                className="absolute top-0 left-0 h-full bg-secondary/90 rounded-full transition-transform duration-150 ease-out shadow-sm"
                style={{ 
                  width: '25%',
                  transform: `translateX(${scrollProgress}%)`
                }}
              />
            </div>
            <Carousel 
              className="w-full max-w-5xl mx-auto" 
              setApi={setApi}
              opts={{
                align: 'start',
                dragFree: true,
                containScroll: 'trimSnaps',
                skipSnaps: true
              }}
            >
              <CarouselContent className="-ml-2 sm:-ml-3">
                {bestSellingProducts.map((product) => (
                  <CarouselItem key={product.id} className="pl-2 sm:pl-3 basis-[140px] sm:basis-[160px] md:basis-[180px]"
                  >
                    <div className="rounded-lg overflow-hidden border bg-card h-full shadow-sm hover:shadow-md transition-shadow duration-200">
                      <img
                        src={product.image}
                        alt={product.name}
                        className="w-16 h-16 sm:w-20 sm:h-20 object-cover mx-auto my-2"
                      />
                      <div className="p-2 sm:p-3 space-y-1 sm:space-y-1.5">
                        <h3 className="font-medium text-sm sm:text-base">{product.name}</h3>
                        <p className="text-xs text-muted-foreground">SKU: {product.sku}</p>
                      </div>
                    </div>
                  </CarouselItem>
                ))}
              </CarouselContent>
              <CarouselPrevious className="-left-4 sm:-left-5" />
              <CarouselNext className="-right-4 sm:-right-5" />
            </Carousel>
          </div>
        </div>
      </div>
    </div>
  );
}
