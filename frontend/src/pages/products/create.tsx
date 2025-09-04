import { useState } from "react"
import { useNavigate } from "react-router-dom"
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core"
import {
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  rectSortingStrategy,
} from "@dnd-kit/sortable"
import { CSS } from "@dnd-kit/utilities"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { usePageTitle } from "@/hooks/use-page-title"
import { RichTextEditor } from "@/components/ui/rich-text-editor"

function SortableImage({ id, image, index, onRemove }: { id: string; image: { preview: string }; index: number; onRemove: (index: number) => void }) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    cursor: 'grab',
    touchAction: 'none',
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="relative aspect-square group"
    >
      <div className="absolute inset-0 rounded-lg border-2 border-primary/20 group-hover:border-primary/40 transition-colors">
        <div className="absolute top-2 left-2 bg-primary text-primary-foreground w-6 h-6 rounded-full flex items-center justify-center text-sm font-medium shadow-sm">
          {index + 1}
        </div>
      </div>
      <img
        src={image.preview}
        alt={`Product ${index + 1}`}
        className="h-full w-full rounded-lg object-cover"
      />
      <button
        type="button"
        onClick={() => onRemove(index)}
        className="absolute -top-2 -right-2 rounded-full bg-red-500 p-1 text-white hover:bg-red-600 shadow-sm"
      >
        <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
        </svg>
      </button>
    </div>
  );
}

interface ProductImage {
  file: File
  preview: string
}

interface ProductIdentifier {
  type: 'MPN' | 'SKU' | 'GTIN'
  value: string
}

interface ProductFormData {
  title: string
  description: string
  images: ProductImage[]
  type: string
  brand: string
  status: 'draft' | 'published'
  isLimited: boolean
  identifiers: ProductIdentifier[]
}

const PRODUCT_TYPES = [
  {
    id: 'shoes',
    name: 'Shoes',
    count: '2,120 Items',
    icon: (
      <svg
        fill="currentColor"
        className="h-8 w-8"
        version="1.1"
        id="Layer_1"
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 512.042 512.042"
      >
        <g>
          <g>
            <g>
              <path d="M509.41,285.857l-8.544-19.961c-0.062-0.145-0.148-0.274-0.217-0.415c-1.405-3.563-3.674-7.096-7-10.449
                c-13.832-13.941-43.274-21.605-92.647-21.5c-0.306,0-0.616,0.002-0.924,0.003c-1.215,0.005-2.437,0.013-3.676,0.027
                c-0.841,0.009-1.692,0.022-2.544,0.036c-0.715,0.012-1.429,0.023-2.151,0.038c-1.378,0.027-2.773,0.062-4.179,0.1
                c-0.129,0.004-0.253,0.005-0.382,0.009l-39.094-18.396l0.283-0.481c2.388-4.063,1.029-9.293-3.034-11.68
                s-9.293-1.029-11.68,3.034l-1.076,1.83l-19.089-8.982l0.745-1.268c2.388-4.063,1.029-9.293-3.034-11.68
                c-4.063-2.388-9.293-1.029-11.68,3.034l-1.538,2.618l-19.089-8.982l1.208-2.055c2.388-4.063,1.029-9.293-3.034-11.68
                c-4.063-2.388-9.293-1.029-11.68,3.034l-2.001,3.405l-19.089-8.982l1.67-2.843c2.388-4.063,1.029-9.293-3.034-11.68
                c-4.063-2.388-9.293-1.029-11.68,3.034l-2.463,4.192l-22.792-10.725c-3.898-1.834-8.553-0.475-10.851,3.169
                c-0.889,1.41-2.811,4.099-5.736,7.639c-4.949,5.989-10.852,11.994-17.669,17.592c-4.23,3.474-8.584,6.616-13.051,9.434
                c-0.322,0.146-0.643,0.305-0.957,0.496c-27.349,16.639-58.866,20.838-95.056,7.716c-0.322-0.117-0.645-0.208-0.968-0.285
                c-8.825-3.316-17.923-7.646-27.313-13.115c-1.182-0.688-2.438-1.044-3.685-1.133c-6.901-1.739-13.609-11.559-13.609-20.661
                c0-4.713-3.82-8.533-8.533-8.533s-8.533,3.821-8.533,8.533c0,14.104,8.42,28.597,20.317,34.84
                c-0.567,3.004-1.178,6.241-1.829,9.673c-2.406,12.69-4.838,25.379-7.141,37.219c-0.323,1.661-0.323,1.661-0.647,3.32
                c-5.292,27.113-9.114,45.528-10.229,48.988c-0.219,0.633-0.365,1.3-0.429,1.993c-0.012,0.127-0.014,0.252-0.02,0.379
                c-0.007,0.139-0.022,0.276-0.022,0.417v50.901c0,4.64,3.707,8.43,8.346,8.531c0.878,0.019,0.878,0.019,5.054,0.112
                c5.714,0.127,8.902,0.198,14.287,0.319c15.386,0.346,32.531,0.738,50.995,1.167c52.753,1.226,105.506,2.508,154.74,3.781
                c25.951,0.671,50.263,1.324,72.639,1.952c14.707,0.413,28.514,0.813,41.355,1.201c70.483,2.135,118.744-10.72,149.103-32.552
                C510.725,321.348,515.591,302.632,509.41,285.857z M232.252,298.179c-1.132-0.022-2.269-0.047-3.404-0.07
                c-21.117-0.441-42.959-1.099-65.382-1.957c3.4-10.084,12.935-17.346,24.167-17.346h68.471c12.171,0,22.35,8.528,24.889,19.933
                c-1.489-0.003-2.984-0.008-4.486-0.014c-0.279-0.001-0.557-0.002-0.836-0.003c-1.26-0.006-2.53-0.013-3.8-0.021
                c-1.125-0.007-2.25-0.014-3.381-0.022c-0.796-0.006-1.594-0.012-2.393-0.019c-1.708-0.014-3.421-0.03-5.141-0.047
                c-0.395-0.004-0.789-0.008-1.185-0.012c-3.279-0.035-6.585-0.077-9.906-0.124c-1.713-0.024-3.428-0.049-5.15-0.076
                c-1.26-0.02-2.526-0.041-3.791-0.063c-1.456-0.025-2.915-0.051-4.377-0.078C235.118,298.234,233.689,298.207,232.252,298.179z
                M392.313,250.696c0.666-0.013,1.329-0.026,1.987-0.037c0.699-0.011,1.403-0.023,2.093-0.031
                c0.547-0.007,1.083-0.01,1.624-0.014c46.722-0.379,72.993,6.198,83.157,16.443c3.221,3.247,4.093,6.151,3.771,8.721
                c-10.292,9.508-45.696,16.696-99.856,20.248l0,0L392.313,250.696z M182.538,190.079c7.714-6.334,14.379-13.114,19.994-19.91
                c0.943-1.141,1.804-2.216,2.583-3.218l14.958,7.039l-5.562,9.465c-2.388,4.063-1.029,9.293,3.034,11.68s9.293,1.029,11.68-3.034
                l6.355-10.815l19.089,8.982l-6.025,10.252c-2.388,4.063-1.029,9.293,3.034,11.68c4.063,2.388,9.293,1.029,11.68-3.034
                l6.818-11.602l19.089,8.982l-6.487,11.04c-2.388,4.063-1.029,9.293,3.034,11.68s9.293,1.029,11.68-3.034l7.28-12.389
                l19.089,8.982l-6.95,11.827c-2.388,4.063-1.029,9.293,3.034,11.68c4.063,2.388,9.293,1.029,11.68-3.034l7.743-13.176
                l36.224,17.045l-7.944,49.848c-20.876,0.996-44.087,1.562-69.357,1.696c-2.738-20.863-20.574-36.976-42.187-36.976h-68.471
                c-20.472,0-37.561,14.455-41.636,33.711c-15.678-0.664-31.485-1.414-47.348-2.244c-23.192-1.214-44.945-2.514-64.632-3.814
                c-5.417-0.358-10.361-0.695-14.811-1.007c1.936-8.837,4.708-22.595,8.246-40.722c0.325-1.664,0.325-1.664,0.649-3.331
                c2.309-11.87,4.745-24.584,7.156-37.298c0.353-1.859,0.694-3.663,1.023-5.404c5.037,2.576,10.008,4.843,14.921,6.845v2.07
                c0,23.563,19.104,42.667,42.667,42.667h34.133c23.563,0,42.667-19.104,42.667-42.667v-11.664" />
            </g>
          </g>
        </g>
      </svg>
    ),
  },
  {
    id: 'clothing',
    name: 'Clothing',
    count: '1,547 Items',
    icon: (
      <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.75 10.5V6a3.75 3.75 0 10-7.5 0v4.5m11.356-1.993l1.263 12c.07.665-.45 1.243-1.119 1.243H4.25a1.125 1.125 0 01-1.12-1.243l1.264-12A1.125 1.125 0 015.513 7.5h12.974c.576 0 1.059.435 1.119 1.007zM8.625 10.5a.375.375 0 11-.75 0 .375.375 0 01.75 0zm7.5 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z" />
      </svg>
    ),
  },
  {
    id: 'accessories',
    name: 'Accessories',
    count: '864 Items',
    icon: (
      <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  }
]

export function CreateProductPage() {
  usePageTitle("Create Product")
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(false)
  const [isGeneratingDescription, setIsGeneratingDescription] = useState(false)
  const [formData, setFormData] = useState<ProductFormData>({
    title: "",
    description: "",
    images: [],
    type: "",
    brand: "",

    status: "draft",
    isLimited: false,
    identifiers: []
  })

  const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = event.target.files
    if (!files) return

    const newImages: ProductImage[] = Array.from(files).map(file => ({
      file,
      preview: URL.createObjectURL(file)
    }))

    setFormData(prev => ({
      ...prev,
      images: [...prev.images, ...newImages]
    }))
  }

  const removeImage = (index: number) => {
    setFormData(prev => ({
      ...prev,
      images: prev.images.filter((_, i) => i !== index)
    }))
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault()
    setIsLoading(true)

    try {
      // TODO: Implement API call to create product
      console.log("Product data:", formData)
      navigate("/products")
    } catch (error) {
      console.error("Error creating product:", error)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-6 sm:py-8">
        <h1 className="text-lg font-semibold">Create a New Product</h1>

        <div className="pt-6 sm:pt-8">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 sm:gap-8">
            {/* Left Column */}
            <div className="lg:col-span-1 space-y-6">
              {/* Product Image */}
              <div className="rounded-lg border bg-card">
                <div className="p-4">
                  <h3 className="font-medium">Product Images</h3>
                </div>
                <div className="border-t p-4">
                  <div className="grid gap-4">
                    <DndContext
                      sensors={useSensors(
                        useSensor(PointerSensor, {
                          activationConstraint: {
                            distance: 8,
                          },
                        }),
                        useSensor(KeyboardSensor, {
                          coordinateGetter: sortableKeyboardCoordinates,
                        })
                      )}
                      collisionDetection={closestCenter}
                      onDragEnd={(event) => {
                        const {active, over} = event;
                        
                        if (over && active.id !== over.id) {
                          const activeId = String(active.id);
                          const overId = String(over.id);
                          const oldIndex = parseInt(activeId.split('-')[1]);
                          const newIndex = parseInt(overId.split('-')[1]);
                          
                          const newImages = [...formData.images];
                          const [movedImage] = newImages.splice(oldIndex, 1);
                          newImages.splice(newIndex, 0, movedImage);
                          
                          setFormData(prev => ({
                            ...prev,
                            images: newImages
                          }));
                        }
                      }}
                    >
                      <SortableContext
                        items={formData.images.map((_, i) => `image-${i}`)}
                        strategy={rectSortingStrategy}
                      >
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                          {formData.images.map((image, index) => (
                            <SortableImage
                              key={index}
                              id={`image-${index}`}
                              image={image}
                              index={index}
                              onRemove={removeImage}
                            />
                          ))}
                        </div>
                      </SortableContext>
                    </DndContext>
                    <div className="flex aspect-square sm:aspect-video items-center justify-center rounded-lg border-2 border-dashed ">
                      <label className="flex cursor-pointer flex-col items-center justify-center gap-2">
                        <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8 text-muted-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                        </svg>
                        <span className="text-sm text-muted-foreground">Click to upload</span>
                        <input
                          type="file"
                          className="hidden"
                          accept="image/*"
                          multiple
                          onChange={handleImageUpload}
                        />
                      </label>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      You need at least 4 images. Pay attention to the quality of the pictures you add (important)
                    </p>
                  </div>
                </div>
              </div>

              {/* Settings */}
              <div className="rounded-lg border bg-card">
                <div className="p-4">
                  <h3 className="font-medium">Status</h3>
                </div>
                <div className="border-t p-4">
                  <select
                    value={formData.status}
                    onChange={e => setFormData(prev => ({ ...prev, status: e.target.value as 'draft' | 'published' }))}
                    className="w-full rounded-md border bg-background px-3 py-2 text-sm outline-none"
                  >
                    <option value="draft">Draft</option>
                    <option value="published">Published</option>
                  </select>
                </div>
              </div>
            </div>

            {/* Right Column */}
            <div className="lg:col-span-2 space-y-6">
              {/* Product Type */}
              <div className="rounded-lg border bg-card">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between p-4 gap-4">
                  <h3 className="font-medium">Product Type</h3>
                </div>
                <div className="border-t p-4">
                  <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
                    {PRODUCT_TYPES.map((type) => (
                      <button
                        key={type.id}
                        onClick={() => setFormData(prev => ({ ...prev, type: type.id }))}
                        className={`flex flex-col items-center rounded-lg border p-3 sm:p-4 text-center transition-colors hover:bg-accent ${formData.type === type.id ? 'border-primary bg-primary/5' : ''}`}
                      >
                        <div className="mb-2">{type.icon}</div>
                        <div className="text-sm font-medium">{type.name}</div>
                        <div className="text-xs text-muted-foreground">{type.count}</div>
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              {/* Product Details */}
              <div className="rounded-lg border bg-card">
                <div className="p-4">
                  <h3 className="font-medium">Product Detail</h3>
                </div>
                <div className="border-t p-4">
                  <div className="grid gap-6">
                    {/* Product Name & Brand */}
                    <div className="grid gap-4 sm:grid-cols-2">
                      <div className="space-y-2">
                        <Label htmlFor="title">Product Name</Label>
                        <Input
                          id="title"
                          placeholder="Enter the name or model of"
                          value={formData.title}
                          onChange={e => setFormData(prev => ({ ...prev, title: e.target.value }))}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="brand">Brand</Label>
                        <Input
                          id="brand"
                          placeholder="Select product brand"
                          value={formData.brand}
                          onChange={e => setFormData(prev => ({ ...prev, brand: e.target.value }))}
                        />
                      </div>
                    </div>

                    {/* Description */}
                    <div className="space-y-2">
                      <div className="flex items-center justify-between mb-2">
                        <Label htmlFor="description">Product Description</Label>
                        <Button 
                          variant="outline" 
                          size="sm"
                          className="flex items-center gap-2"
                          disabled={isGeneratingDescription || !formData.title || !formData.type}
                          onClick={async () => {
                            setIsGeneratingDescription(true);
                            try {
                              const productInfo = {
                                title: formData.title,
                                brand: formData.brand,
                                type: PRODUCT_TYPES.find(t => t.id === formData.type)?.name || formData.type
                              };
                              
                              // Simulate API delay
                              await new Promise(resolve => setTimeout(resolve, 2000));
                              
                              // For now, generate a placeholder description
                              const description = `${productInfo.title} is a premium ${productInfo.type.toLowerCase()} product${productInfo.brand ? ` from ${productInfo.brand}` : ''}. `;
                              setFormData(prev => ({ ...prev, description }));
                            } catch (error) {
                              console.error('Failed to generate description:', error);
                            } finally {
                              setIsGeneratingDescription(false);
                            }
                          }}
                        >
                          {isGeneratingDescription ? (
                            <>
                              <svg
                                className="animate-spin -ml-1 mr-2 h-4 w-4"
                                xmlns="http://www.w3.org/2000/svg"
                                fill="none"
                                viewBox="0 0 24 24"
                              >
                                <circle
                                  className="opacity-25"
                                  cx="12"
                                  cy="12"
                                  r="10"
                                  stroke="currentColor"
                                  strokeWidth="4"
                                />
                                <path
                                  className="opacity-75"
                                  fill="currentColor"
                                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                                />
                              </svg>
                              Generating...
                            </>
                          ) : (
                            <>
                              <svg
                                xmlns="http://www.w3.org/2000/svg"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                className="w-4 h-4"
                              >
                                <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
                              </svg>
                              Generate with AI
                            </>
                          )}
                        </Button>
                      </div>
                      <div className={isGeneratingDescription ? 'opacity-60 pointer-events-none' : ''}>
                        <RichTextEditor
                          value={formData.description}
                          onChange={(value) => setFormData(prev => ({ ...prev, description: value }))}
                          readOnly={isGeneratingDescription}
                        />
                      </div>
                    </div>

                    {/* Product Identifiers */}
                    <div className="space-y-4">
                      <div className="flex items-center justify-between">
                        <Label>Product Identifiers</Label>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          onClick={() => setFormData(prev => ({
                            ...prev,
                            identifiers: [...prev.identifiers, { type: 'SKU', value: '' }]
                          }))}
                        >
                          Add Identifier
                        </Button>
                      </div>
                      {formData.identifiers.map((identifier, index) => (
                        <div key={index} className="flex gap-4 items-start">
                          <div className="relative w-[120px]">
                            <select
                              value={identifier.type}
                              onChange={e => {
                                const newIdentifiers = [...formData.identifiers]
                                newIdentifiers[index] = {
                                  ...newIdentifiers[index],
                                  type: e.target.value as 'MPN' | 'SKU' | 'GTIN'
                                }
                                setFormData(prev => ({ ...prev, identifiers: newIdentifiers }))
                              }}
                              className="w-full appearance-none rounded-md border bg-background px-3 py-2 pr-8 text-sm outline-none"
                            >
                              <option value="SKU">SKU</option>
                              <option value="MPN">MPN</option>
                              <option value="GTIN">GTIN</option>
                            </select>
                            <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-2">
                              <svg className="h-4 w-4 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                                <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd" />
                              </svg>
                            </div>
                          </div>
                          <Input
                            value={identifier.value}
                            onChange={e => {
                              const newIdentifiers = [...formData.identifiers]
                              newIdentifiers[index] = {
                                ...newIdentifiers[index],
                                value: e.target.value
                              }
                              setFormData(prev => ({ ...prev, identifiers: newIdentifiers }))
                            }}
                            placeholder={`Enter ${identifier.type}`}
                            className="flex-1"
                          />
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-10 w-10 text-destructive"
                            onClick={() => {
                              const newIdentifiers = formData.identifiers.filter((_, i) => i !== index)
                              setFormData(prev => ({ ...prev, identifiers: newIdentifiers }))
                            }}
                          >
                            <svg xmlns="http://www.w3.org/2000/svg" className="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                              <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                            </svg>
                          </Button>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* Submit Button */}
              <div className="flex justify-end px-4 sm:px-0">
                <Button onClick={handleSubmit} disabled={isLoading} size="lg" className="w-full sm:w-auto">
                  Create Product
                </Button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
