import { createContext, useContext, useEffect, useState } from "react"
import { useNavigate } from "react-router-dom"
import { api } from "@/lib/api"

interface User {
  id: string
  email: string
  name: string
  role: string
}

interface AuthContextType {
  user: User | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const navigate = useNavigate()

  const checkAuth = async () => {
    try {
      const response = await api.get<{ userinfo: User }>("/User/me")
      setUser(response.data?.userinfo ?? null)
    } catch (error) {
      setUser(null)
      // Only redirect to login if we're not already there
      if (!window.location.pathname.includes("/login")) {
        navigate("/login")
      }
    } finally {
      setIsLoading(false)
    }
  }

  const login = async (email: string, password: string) => {
    try {
      await api.post("/User/Login", { email, password })
      await checkAuth()
      navigate("/dashboard")
    } catch (error) {
      throw new Error("Invalid credentials")
    }
  }

  const logout = async () => {
    try {
      await api.post("/auth/logout", {}, { withCredentials: true })
      setUser(null)
      navigate("/login")
    } catch (error) {
      console.error("Error during logout:", error)
    }
  }

  useEffect(() => {
    checkAuth()
  }, [])

  const value = {
    user,
    isLoading,
    login,
    logout,
    checkAuth,
  }

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-32 w-32 animate-spin rounded-full border-b-2 border-primary"></div>
      </div>
    )
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider")
  }
  return context
}
