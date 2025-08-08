import React, { useState, useEffect } from "react";
import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from "@/components/ui/accordion"; // adjust import path to your accordion setup
import { Button } from "@/components/ui/button"; // Assuming you have a button component
import { Search, Check } from 'lucide-react'; // Import icons from lucide-react
import { useProduct } from "@/contexts/products-context";

const IMAGES_PER_PAGE = 10;
const RENDER_DELAY_MS = 140;

export const ImageGallery = ({
  title,
  images,
}: {
  title: string;
  images: string[];
}) => {
  const [currentPage, setCurrentPage] = useState(1);
  const [accordionValue, setAccordionValue] = useState("gallery");
  const [isContentVisible, setIsContentVisible] = useState(true);
  // const {Selectedproduct,GetMoreSimilarImg}=useProduct()
  const [sp,setsp]=useState<number>(2);
  console.log(images)
  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;

    if (accordionValue) {
      timer = setTimeout(() => {
        setIsContentVisible(true);
      }, RENDER_DELAY_MS);
    } else {
      setIsContentVisible(false);
    }

    return () => clearTimeout(timer);
  }, [accordionValue]);

  if (!images || images.length === 0) {
    return null;
  }

  const totalPages = Math.ceil(images.length / IMAGES_PER_PAGE);
  const lastImageIndex = currentPage * IMAGES_PER_PAGE;
  const firstImageIndex = lastImageIndex - IMAGES_PER_PAGE;
  const currentImages = images.slice(firstImageIndex, lastImageIndex);

  const goToNextPage = () => {
    setCurrentPage((page) => Math.min(page + 1, totalPages));
  };

  const goToPreviousPage = () => {
    setCurrentPage((page) => Math.max(page - 1, 1));
  };

  
  const handleSearchClick = (imageUrl: string) => {
    // if(Selectedproduct)
    // {  GetMoreSimilarImg(Selectedproduct?.id,sp)
    //    setsp(s=>s+1)
    // }
  };

  const handleSelectClick = (imageUrl: string) => {
    console.log("Select icon clicked for:", imageUrl);
    
  };

  return (
    <Accordion
      type="single"
      collapsible
      className="w-full"
      value={accordionValue}
      onValueChange={setAccordionValue}
    >
      <AccordionItem value="gallery">
        <AccordionTrigger>{title}</AccordionTrigger>
        <AccordionContent>
          {isContentVisible && (
            <>
              <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-3">
                {currentImages.map((img, index) => (
                  img!=""&&
                  <div
                    key={`${currentPage}-${index}`}
                    className="group relative aspect-square bg-gray-100 rounded-md overflow-hidden shadow-sm"
                  >
                    <img
                      src={img}
                      alt={`${title} ${firstImageIndex + index + 1}`}
                      loading="lazy"
                      className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-110"
                      onError={(e) => { e.currentTarget.src = 'https://placehold.co/300x300/CCCCCC/FFFFFF?text=Error'; }}
                    />
                    {/* Hover overlay with icons */}
                    <div className="absolute inset-0 bg-black bg-opacity-0 group-hover:bg-opacity-50 transition-all duration-300 flex items-center justify-center gap-4 opacity-0 group-hover:opacity-100">
                      <button
                        onClick={() => handleSearchClick(img)}
                        className="p-2 bg-white/80 rounded-full text-gray-800 hover:bg-white hover:scale-110 transition-transform"
                        aria-label="Search image"
                      >
                        <Search size={20} />
                      </button>
                      <button
                        onClick={() => handleSelectClick(img)}
                        className="p-2 bg-white/80 rounded-full text-gray-800 hover:bg-white hover:scale-110 transition-transform"
                        aria-label="Select image"
                      >
                        <Check size={20} />
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {totalPages > 1 && (
                <div className="flex items-center justify-center gap-4 mt-4">
                  <Button onClick={goToPreviousPage} variant="outline" disabled={currentPage === 1}>
                    Previous
                  </Button>
                  <span className="text-sm text-gray-600">
                    Page {currentPage} of {totalPages}
                  </span>
                  <Button onClick={goToNextPage} variant="outline" disabled={currentPage === totalPages}>
                    Next
                  </Button>
                </div>
              )}
            </>
          )}
        </AccordionContent>
      </AccordionItem>
    </Accordion>
  );
};
