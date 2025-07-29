import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useResizeObserver } from '@/hooks/use-resize-observer';
import DashboardLayout from './layouts/DashboardLayout';
import { LoginPage } from './pages/auth/login';
import { AuthProvider } from '@/contexts/auth-context';
import {ScrapperProvider} from '@/contexts/Scrapper-context'

import { ProtectedRoute } from '@/components/protected-route';
import { Toaster } from '@/components/ui/toaster';


// Import pages
import DashboardPage from './pages/dashboard';
import ProductsPage from './pages/products';
import { CreateProductPage } from './pages/products/create';
import ProductSearchPage from './pages/products/search';
import UsersPage from './pages/users';
import Scraperspage from './pages/Scrapers';
import SettingsPage from './pages/settings';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

function RootLayout({ children }: { children: React.ReactNode }) {
  const dimensions = useResizeObserver();

  return (
    <div className="relative min-h-screen bg-background antialiased" style={{ minHeight: `${dimensions.height}px` }}>
      <div className="flex min-h-screen flex-col">
        {children}
      </div>
    </div>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <ScrapperProvider>
          <RootLayout>
            <Routes>
              <Route path="login" element={<LoginPage />} />
              <Route
                path="/*"
                element={
                  <ProtectedRoute>
                    <DashboardLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<Navigate to="dashboard" replace />} />
                <Route path="dashboard" element={<DashboardPage />} />
                <Route path="products" element={<ProductsPage />} />
                <Route path="products/search" element={<ProductSearchPage />} />
                <Route path="products/create" element={<CreateProductPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="Scrapers" element={<Scraperspage />} />
                <Route path="settings" element={<SettingsPage />} />
                <Route path="*" element={<Navigate to="dashboard" replace />} />
              </Route>
            </Routes>
            <Toaster />
          </RootLayout>
          </ScrapperProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
