import React, { useState } from "react";
import { cn } from "@/lib/utils";
import { useProduct } from "@/contexts/products-context";
type PaginationProps = {
  totalItems: number;
  itemsPerPage: number;

  onPageChange: (page: number) => void;
  className?: string;
};

const Pagination: React.FC<PaginationProps> = ({
  totalItems,
  itemsPerPage,

  onPageChange,
  className,
}) => {
  

  const {currentPage,Setcurrentpage}=useProduct();
  const totalPages = Math.ceil(totalItems / itemsPerPage);
  if (totalPages <= 1) return null;

  const handleClick = (page: number) => {
    if (page >= 1 && page <= totalPages){ 
       onPageChange(page)
      Setcurrentpage(page)
    };
  };

  const getPages = () => {
    const pages: (number | string)[] = [];
    if(currentPage==null)return pages
    if (totalPages <= 5) {
      for (let i = 1; i <= totalPages; i++) pages.push(i);
    } else {
      pages.push(1);
      if (currentPage > 3) pages.push("...");
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);

      for (let i = start; i <= end; i++) pages.push(i);

      if (currentPage < totalPages - 2) pages.push("...");
      pages.push(totalPages);
    }

    return pages;
  };

  return (
    <div className={cn("flex items-center gap-2", className)}>
      <button
        onClick={() => handleClick(currentPage?currentPage - 1:1)}
        disabled={currentPage === 1}
        className="px-3 py-1 text-sm rounded-md border disabled:opacity-40"
      >
        Prev
      </button>
      { getPages().map((p, i) =>
        typeof p === "number" ? (
          <button
            key={i}
            onClick={() => handleClick(p)}
            className={cn(
              "px-3 py-1 text-sm rounded-md border",
              p === currentPage ? "bg-primary text-white" : ""
            )}
          >
            {p}
          </button>
        ) : (
          <span key={i} className="px-2 text-muted-foreground text-sm">
            ...
          </span>
        )
      )}
      <button
        onClick={() => handleClick(currentPage?currentPage + 1:1)}
        disabled={currentPage === totalPages}
        className="px-3 py-1 text-sm rounded-md border disabled:opacity-40"
      >
        Next
      </button>
    </div>
  );
};

export default Pagination;
