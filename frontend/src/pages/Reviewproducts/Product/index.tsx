import  { useState, useEffect } from 'react';
import { useProduct } from '@/contexts/products-context';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import {ImageGallery} from '@/components/ui/ImageGallery'
import {ArrowBigLeft} from "lucide-react"
const Product = () => {
  const navigate = useNavigate();
  const { Selectedproduct, normalizeDateTime } = useProduct();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [sizes, setSizes] = useState<{size:string,instock:boolean}[]>();
  const [price, setPrice] = useState(0);
  const [condition, setCondition] = useState('');

  useEffect(() => {
    if (Selectedproduct) {
      setTitle(Selectedproduct.title || '');
      setDescription(Selectedproduct.description || '');
      setPrice(Selectedproduct.price || 0);
      // Ensure variants exist before mapping
      setSizes(Selectedproduct.variants ? Selectedproduct.variants.map(v => ({ size: v.size, instock: v.inStock })) : []);
      setCondition(Selectedproduct.condition || '');
    } else {
      navigate("/Reviewproducts");
    }
  }, [Selectedproduct, navigate]);

  if (!Selectedproduct) {
    return (
      <div className="w-full min-h-screen flex items-center justify-center">
        <p>No product selected. Redirecting...</p>
      </div>
    );
  }

  return (
    <div className="w-full min-h-screen flex flex-col bg-gray-50">
     
      <div className="w-full text-md sm:text-lg items-center align-middle  font-semibold h-[5vh] sm:h-[7vh] bg-[#E2E2E2] flex sticky top-0 z-10 shrink-0">
        <Button className="  flex items-center cursor-pointer bg-transparent text-black border-none shadow-none hover:bg-transparent"
        onClick={()=>{
          navigate("/Reviewproducts")
        }}
        > <ArrowBigLeft/> back</Button>
        <div className="mx-5  sm:mx-14">Product</div>
        <div className="text-xs sm:text-lg font-normal truncate pr-4">{Selectedproduct?.title}</div>
      </div>

  
      <div className="flex flex-col lg:flex-row flex-1 gap-4 p-4">

        <div className="bg-[#e5f6fc] lg:flex-[0.5] p-4 rounded-lg shadow-sm h-fit lg:h-auto">
          <h2 className="text-lg sm:text-xl font-bold mb-4">Details</h2>
          <div className="space-y-6">
            <div>
              <label htmlFor="productName" className='text-sm sm:text-md font-semibold mb-2 block'>Product Name</label>
              <input
                id="productName"
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="w-full p-2 border border-gray-300 rounded-md"
              />
            </div>
            <div>
              <label htmlFor="productDescription" className="text-sm sm:text-md font-semibold mb-2 block">Description</label>
              <textarea
                id="productDescription"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                className="w-full p-2 border border-gray-300 rounded-md"
              ></textarea>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Sizes</div>
              <div className="flex gap-4 flex-wrap">
                {sizes?.map((s, i) => (
                  <div key={i} className={`px-2 py-1 rounded-md text-sm ${!s.instock ? "line-through text-gray-500 bg-gray-200" : "bg-blue-100 text-blue-800"}`}>
                    {s.size}
                  </div>
                ))}
              </div>
            </div>
            <div>
              <div className="text-sm sm:text-md font-semibold mb-2">Condition</div>
              <div className="text-sm sm:text-md">{condition}</div>
            </div>
            <div>
              <label htmlFor="retailPrice" className="text-sm sm:text-md font-semibold mb-2 block">Retail Price:</label>
              <input
                id="retailPrice"
                type="number"
                value={price}
                onChange={(e) => setPrice(Number(e.target.value))}
                className="w-full p-2 border border-gray-300 rounded-md"
              />
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
            <div className='flex justify-end'>
              <Button className='bg-black text-white hover:bg-gray-800 px-4 py-2 rounded-md'>Save Changes</Button>
            </div>
          </div>
        </div>

      
        <div className="bg-white lg:flex-[1.3] rounded-lg shadow-sm p-4 space-y-8">
            <ImageGallery title="Current Images" images={Selectedproduct.image} />
            <ImageGallery title="Suggested Images" images={Selectedproduct.image} />
        </div>
      </div>

      {/* Footer */}
      <div className="w-full h-[5vh] sm:h-[7vh] bg-black p-5 shrink-0"></div>
    </div>
  );
};

export default Product;