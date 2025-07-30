import React from 'react'
import {useProduct} from '@/contexts/products-context'


const Product = () => {
  const{Selectedproduct}=useProduct()
  return (
<div className="w-full min-h-screen flex flex-col">

  <div className="w-full text-md items-center align-middle gap-28 font-semibold h-[5vh] sm:h-[7vh] bg-[#E2E2E2] flex">
      <div className=" ml-14">Product</div>
       <div className="">{Selectedproduct?.title}</div>
  </div>


  <div className="flex flex-col sm:flex-row flex-1 gap-1 py-3">

    <div className="bg-[#daf0f8] flex-1 sm:flex-[0.5] min-h-[30vh]"></div>

   
    <div className="bg-white flex-1 sm:flex-[1.3] min-h-[40vh]"></div>
  </div>
  <div className="w-full h-[5vh] sm:h-[7vh] bg-black p-5 mb-2"></div>

</div>


  )
}

export default Product