import { useState, useEffect, useRef } from "react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { useNavigate, Link, useLocation } from "react-router-dom"
import { getRandomLoginImage, loginBackgroundImages } from "@/utils/random-image"
import { useAuth } from "@/contexts/auth-context"
import { useToast } from "@/components/ui/use-toast"

function LoginLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen w-full lg:grid-cols-2 overflow-hidden">
      {children}
    </div>
  )
}

import { usePageTitle } from "@/hooks/use-page-title";

export function LoginPage() {
  usePageTitle('Login');
  const { login } = useAuth()
  const { toast } = useToast()
  const [isLoading, setIsLoading] = useState(false)
  const [backgroundImage, setBackgroundImage] = useState('')
  const [imageLoaded, setImageLoaded] = useState(false)
  const location = useLocation()
  
  // Force re-render on route change
  const key = location.key || 'default'

  // Handle background image loading
  useEffect(() => {
    const selectedImage = getRandomLoginImage()
    const img = new Image()
    let mounted = true
    
    img.onload = () => {
      if (mounted) {
        setBackgroundImage(selectedImage)
        setImageLoaded(true)
      }
    }
    
    img.src = selectedImage
    
    // Preload other images for future visits
    const preloadOtherImages = () => {
      if (!mounted) return
      loginBackgroundImages
        .filter(src => src !== selectedImage)
        .forEach(src => {
          const img = new Image()
          img.src = src
        })
    }
    
   
    const timeoutId = setTimeout(preloadOtherImages, 1000)
    
  
    return () => {
      mounted = false
      clearTimeout(timeoutId)
    }
  }, [])
  
  async function onSubmit(event: React.SyntheticEvent) {
    event.preventDefault()
    const form = event.target as HTMLFormElement
    const formData = new FormData(form)
    const email = formData.get('email') as string
    const password = formData.get('password') as string

    setIsLoading(true)
    try {
      await login(email, password)
      // Navigation is handled by the auth context
    } catch (error) {
      console.error('Login failed:', error)
      toast({
        variant: "destructive",
        title: "Login Failed",
        description: "Invalid email or password. Please try again."
      })
      setIsLoading(false)
    }
  }

  return (
    <LoginLayout key={key}>
      <div className="relative hidden lg:flex bg-muted text-white">
        <div className="absolute inset-0">
          <div 
            className="absolute inset-0 bg-cover"
            style={{ 
              backgroundImage: `url(${backgroundImage})`,
              backgroundPosition: 'center',
              filter: 'brightness(0.7)',
              opacity: imageLoaded ? 1 : 0
            }}>
            {!imageLoaded && (
              <div className="absolute inset-0 bg-gray-900" />
            )}
          </div>
        </div>
        <div className="relative z-20 flex flex-col min-h-screen p-10">
          <div className="flex items-center text-lg font-medium">
            <svg 
              xmlns="http://www.w3.org/2000/svg" 
              viewBox="0 0 314.011 177.426" 
              className="h-12 w-auto mr-4"
            >
              <path 
                fill="#fff" 
                stroke="currentColor" 
                strokeMiterlimit="10" 
                strokeWidth="5" 
                d="M308.159 11.692c-3.597-6.128-8.522-9.192-14.772-9.192h-137.4l-38.976 127.39a2.63 2.63 0 0 1-2.515 1.86h-51.15a2.63 2.63 0 0 1-2.512-3.406l6.815-22.04a2.63 2.63 0 0 1 2.512-1.852h33.764c1.304 0 2.412-.96 2.602-2.25 1.715-11.635 2.18-14.255 3.81-26.719l1.15-7.618a2.63 2.63 0 0 0-.183-1.428L84.614 4.095A2.63 2.63 0 0 0 82.196 2.5H33.52c-1.887 0-3.16 1.93-2.417 3.664l24.53 57.297c.742 1.735-.53 3.664-2.418 3.664-7.658 0-14.574 2.55-20.751 7.66-6.173 5.106-10.588 12.022-13.247 20.753L3.742 145.957c-2.188 7.055-1.484 13.648 2.11 19.777 3.597 6.128 8.522 9.192 14.772 9.192h137.4L197 47.536a2.63 2.63 0 0 1 2.515-1.86h51.15a2.63 2.63 0 0 1 2.512 3.406l-6.815 22.04a2.63 2.63 0 0 1-2.512 1.853h-33.764c-1.304 0-2.412.96-2.602 2.25-1.715 11.634-2.18 14.254-3.81 26.718l-1.15 7.618c-.073.484-.01.978.183 1.428l26.69 62.343a2.63 2.63 0 0 0 2.418 1.594h48.676c1.888 0 3.16-1.93 2.418-3.664l-24.53-57.296c-.743-1.735.53-3.665 2.417-3.665 7.658 0 14.574-2.55 20.752-7.66 6.172-5.105 10.587-12.021 13.246-20.752l15.475-50.42c2.188-7.054 1.484-13.648-2.11-19.777z"/>
            </svg>
            <span className="text-3xl font-bold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-white to-white/80">Resellers Room</span>
          </div>
          <div className="relative z-20 mt-auto">
            <blockquote className="max-w-lg">
              <p className="text-xl font-light italic">
                &ldquo;This platform has redefined the way we handle our reselling operations, enhancing efficiency and making the entire experience more seamless and enjoyable.&rdquo;
              </p>
              <footer className="mt-3">
                <div className="font-medium">Jack Ely</div>
                <div className="text-sm text-white/70">Professional Reseller</div>
              </footer>
            </blockquote>
          </div>
        </div>
      </div>
      <main className="flex-1 flex items-center justify-center px-8 py-8 sm:px-12 lg:px-16">
        <div className="w-full max-w-md space-y-6">
            <div className="flex flex-col space-y-2 text-center">
            <h1 className="text-2xl font-semibold tracking-tight">
              Welcome back
            </h1>
            <p className="text-sm text-muted-foreground">
              Enter your email to sign in to your account
            </p>
          </div>
          <div className="grid gap-6">
            <form onSubmit={onSubmit}>
              <div className="grid gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    name="email"
                    placeholder="name@example.com"
                    type="email"
                    autoCapitalize="none"
                    autoComplete="email"
                    autoCorrect="off"
                    disabled={isLoading}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    name="password"
                    type="password"
                    autoCapitalize="none"
                    autoComplete="current-password"
                    disabled={isLoading}
                  />
                </div>
                <Button disabled={isLoading}>
                  {isLoading && (
                    <svg
                      className="mr-2 h-4 w-4 animate-spin"
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
                  )}
                  Sign In
                </Button>
              </div>
            </form>
            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <span className="w-full border-t" />
              </div>
              <div className="relative flex justify-center text-xs uppercase">
                <span className="bg-background px-2 text-muted-foreground">
                  Or continue with
                </span>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-6">
              <Button variant="outline" disabled={isLoading}>
                <svg role="img" viewBox="0 0 24 24" className="mr-2 h-4 w-4">
                  <path
                    fill="currentColor"
                    d="M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z"
                  />
                </svg>
                Google
              </Button>
              <Button variant="outline" disabled={isLoading}>
                <svg role="img" viewBox="0 0 24 24" className="mr-2 h-4 w-4">
                  <path
                    fill="currentColor"
                    d="M16.365 1.43c0 1.14-.493 2.27-1.177 3.08-.744.9-1.99 1.57-2.987 1.57-.12 0-.23-.02-.3-.03-.01-.06-.04-.22-.04-.39 0-1.15.572-2.27 1.206-2.98.804-.94 2.142-1.64 3.248-1.68.03.13.05.28.05.43zm4.565 15.71c-.03.07-.463 1.58-1.518 3.12-.945 1.34-1.94 2.71-3.43 2.71-1.517 0-1.9-.88-3.63-.88-1.698 0-2.302.91-3.67.91-1.377 0-2.332-1.26-3.428-2.8-1.287-1.82-2.323-4.63-2.323-7.28 0-4.28 2.797-6.55 5.552-6.55 1.448 0 2.675.95 3.6.95.865 0 2.222-1.01 3.902-1.01.613 0 2.886.06 4.374 2.19-.13.09-2.383 1.37-2.383 4.19 0 3.26 2.854 4.42 2.955 4.45z"
                  />
                </svg>
                Apple
              </Button>
            </div>
          </div>
          <p className="px-8 text-center text-sm text-muted-foreground">
            By clicking continue, you agree to our{" "}
            <Link
              to="/terms"
              className="underline underline-offset-4 hover:text-primary"
              target="_blank"
              rel="noopener noreferrer"
            >
              Terms of Service
            </Link>{" "}
            and{" "}
            <Link
              to="/privacy"
              className="underline underline-offset-4 hover:text-primary"
              target="_blank"
              rel="noopener noreferrer"
            >
              Privacy Policy
            </Link>
          </p>
        </div>
      </main>
    </LoginLayout>
  )
}
