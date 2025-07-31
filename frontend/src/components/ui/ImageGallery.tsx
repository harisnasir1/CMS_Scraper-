import React, { useState, useEffect } from "react";
import {
  Accordion,
  AccordionItem,
  AccordionTrigger,
  AccordionContent,
} from "@/components/ui/accordion"; // adjust import path to your accordion setup
import { Button } from "@/components/ui/button"; // Assuming you have a button component
import { ProductImageRecord } from "@/types/Sdata";


const IMAGES_PER_PAGE = 10;

const RENDER_DELAY_MS = 140;

export const ImageGallery = ({
  title,
  images,
}: {
  title: string;
  images: ProductImageRecord[];
}) => {
  const [currentPage, setCurrentPage] = useState(1);

  const [accordionValue, setAccordionValue] = useState("gallery");

  const [isContentVisible, setIsContentVisible] = useState(true);

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
          {/* Render content only when isContentVisible is true */}
          {isContentVisible && (
            <>
              <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-3">
                {currentImages.map((img, index) => (
                  <div
                    key={`${currentPage}-${index}`}
                    className="aspect-square bg-gray-100 rounded-md overflow-hidden shadow-sm"
                  >
                    <img
                      src={img.url}
                      alt={`${title} ${firstImageIndex + index + 1}`}
                      loading="lazy"
                      className="w-full h-full object-cover"
                      onError={(e) => { e.currentTarget.src = 'https://placehold.co/300x300/CCCCCC/FFFFFF?text=Error'; }}
                    />
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
