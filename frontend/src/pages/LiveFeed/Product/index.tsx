import { useEffect } from 'react';
import { useProduct } from '@/contexts/products-context';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import {ImageGallery} from '@/components/ui/ImageGallery'
import {ArrowBigLeft,ExternalLink} from "lucide-react"
import { ISelectedImgs } from '@/types/Sdata';

const LiveProduct = () => {
  const navigate = useNavigate();
  const { Selectedproduct, normalizeDateTime } = useProduct();

  useEffect(() => {
    if (!Selectedproduct) {
      navigate("/LiveFeed");
    }
  }, [Selectedproduct, navigate]);

  if (!Selectedproduct) {
    return (
      <div className="w-full min-h-screen flex items-center justify-center">
        <p>No product selected. Redirecting...</p>
      </div>
    );
  }

  const images:ISelectedImgs[]=Selectedproduct.image.map((imgs,i)=>({
    Id:imgs.id,
    Url:imgs.url,
    Priority:i,
    Bgremove:false
  }))

  return (
    <div className="w-full min-h-screen flex flex-col bg-gray-50">

      <div className="w-full text-md sm:text-lg items-center align-middle  font-semibold h-[5vh] sm:h-[7vh] bg-[#E2E2E2] flex sticky top-0 z-10 shrink-0">
        <Button className="  flex items-center cursor-pointer bg-transparent text-black border-none shadow-none hover:bg-transparent"
        onClick={()=>{
          navigate("/LiveFeed")
        }}
        > <ArrowBigLeft/> back</Button>
        <div className="mx-5  sm:mx-14">Live Product</div>
        <div className="text-xs sm:text-lg font-normal truncate pr-4">{Selectedproduct.title}</div>
      </div>


      <div className="flex flex-col lg:flex-row flex-1 gap-4 p-4">

        <div className="bg-[#e5f6fc] lg:flex-[0.5] p-4 rounded-lg shadow-sm h-fit lg:h-auto">
          <h2 className="text-lg sm:text-xl font-bold mb-4">Details</h2>
          <div className="space-y-6">
            <div>
              <div className='text-sm sm:text-md font-semibold mb-2'>Product Name</div>
              <div className="text-sm sm:text-md">{Selectedproduct.title}</div>
            </div>
            <div>
              <div className='text-sm sm:text-md font-semibold mb-2'>Brand</div>
              <div className="text-sm sm:text-md">{Selectedproduct.brand}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Description</div>
              <div className="text-sm sm:text-md whitespace-pre-wrap break-words">{Selectedproduct.description}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Sizes</div>
              <div className="flex gap-4 flex-wrap">
                {Selectedproduct.variants?.map((s, i) => (
                  <div key={i} className={`px-2 py-1 rounded-md text-sm ${!s.inStock ? "line-through text-gray-500 bg-gray-200" : "bg-blue-100 text-blue-800"}`}>
                    {s.size}
                  </div>
                ))}
              </div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Condition</div>
              <div className="text-sm sm:text-md">{Selectedproduct.condition}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Retail Price:</div>
              <div className="text-sm sm:text-md">{Selectedproduct.price}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">SKU:</div>
              <div className="text-sm sm:text-md">{Selectedproduct.sku}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Scraper</div>
              <div className="text-sm sm:text-md">{Selectedproduct.scraperName}</div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Source</div>
              <a href={Selectedproduct.productUrl} target="_blank" rel="noreferrer" className="text-sm sm:text-md text-[#1D7DBD] flex items-center gap-1 break-all">
                <ExternalLink size={16} className="shrink-0" /> {Selectedproduct.productUrl}
              </a>
            </div>
            <div className="grid grid-cols-2 gap-4 text-xs sm:text-sm pt-4 border-t">
              <div>
                <div className="font-semibold">Created At:</div>
                <div>{Selectedproduct.createdAt && normalizeDateTime(Selectedproduct.createdAt)}</div>
              </div>
              <div>
                <div className="font-semibold">Updated At:</div>
                <div>{Selectedproduct.updatedAt && normalizeDateTime(Selectedproduct.updatedAt)}</div>
              </div>
            </div>
          </div>
        </div>


        <div className="bg-white lg:flex-[1.3] rounded-lg shadow-sm p-4 space-y-8">
            <ImageGallery title="Current Images" images={images} Selectedflag={false} />
        </div>
      </div>
    </div>
  );
};

export default LiveProduct;
