import { useState, useEffect } from 'react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useResizeObserver } from '@/hooks/use-resize-observer';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  Users,
  Settings,
  Menu,
  Bell,
  Search,
  User,
  LogOut,
  CreditCard,
  UserPlus,
  Package,
  Tags,
  ChevronRight,
  ShoppingBag,
  CloudCog
} from 'lucide-react';


import { Button } from '@/components/ui/button';
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuShortcut,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';

const SIDEBAR_WIDTH = 'w-56';

const menuSections = [
  {
    items: [
      { text: 'Dashboard', icon: <LayoutDashboard className="w-4 h-4" />, path: 'dashboard' },
       
    ]
  },
  {
    title: 'Products',
    items: [
      { text: 'Live feed', icon: <CloudCog className="w-4 h-4" />, path: 'LiveFeed' },
      { text: 'Review Products', icon: <Users className="w-4 h-4" />, path: 'Reviewproducts' },
      { text: 'Database', icon: <Settings className="w-4 h-4" />, path: 'Database' },
    ]
  },
  {
    title: 'Admin',
    items: [
      { text: 'Scrapers', icon: <CloudCog className="w-4 h-4" />, path: 'Scrapers' },
      { text: 'Users', icon: <Users className="w-4 h-4" />, path: 'users' },
      { text: 'Settings', icon: <Settings className="w-4 h-4" />, path: 'settings' },
    ]
  }
];

const DashboardLayout = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isDesktop, setIsDesktop] = useState(false);
  const location = useLocation();
  const dimensions = useResizeObserver();

  // Handle window resize
  useEffect(() => {
    const handleResize = () => {
      const isLargeScreen = window.innerWidth >= 1024;
      setIsDesktop(isLargeScreen);
      if (isLargeScreen) {
        setIsSidebarOpen(true);
      }
    };

    // Set initial state
    handleResize();

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // Handle mobile sidebar toggle
  const toggleSidebar = () => {
    if (!isDesktop) {
      setIsSidebarOpen(prev => !prev);
    }
  };

  useEffect(() => {
    // Only force layout recalculation on pathname change
    window.dispatchEvent(new Event('resize'));
  }, [location.pathname]);

  return (
    <div className="relative flex min-h-screen overflow-hidden">
      {/* Sidebar */}
      {/* Backdrop */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 z-50 bg-background/80 backdrop-blur-sm lg:hidden"
          onClick={() => setIsSidebarOpen(false)}
          aria-hidden="true"
        />
      )}
      
      {/* Sidebar */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-[51] flex flex-col border-r bg-background transition-transform duration-300 ease-in-out print:hidden',
          SIDEBAR_WIDTH,
          isSidebarOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0 lg:inset-0',
        )}
      >
        <div className="flex h-16 items-center px-6">
          <Link to="/" className="flex items-center gap-2">
            <svg 
              xmlns="http://www.w3.org/2000/svg" 
              viewBox="0 0 314.011 177.426" 
              className="h-5 w-5"
            >
              <path 
                fill="#fff" 
                stroke="currentColor" 
                strokeMiterlimit="10" 
                strokeWidth="5" 
                d="M308.159 11.692c-3.597-6.128-8.522-9.192-14.772-9.192h-137.4l-38.976 127.39a2.63 2.63 0 0 1-2.515 1.86h-51.15a2.63 2.63 0 0 1-2.512-3.406l6.815-22.04a2.63 2.63 0 0 1 2.512-1.852h33.764c1.304 0 2.412-.96 2.602-2.25 1.715-11.635 2.18-14.255 3.81-26.719l1.15-7.618a2.63 2.63 0 0 0-.183-1.428L84.614 4.095A2.63 2.63 0 0 0 82.196 2.5H33.52c-1.887 0-3.16 1.93-2.417 3.664l24.53 57.297c.742 1.735-.53 3.664-2.418 3.664-7.658 0-14.574 2.55-20.751 7.66-6.173 5.106-10.588 12.022-13.247 20.753L3.742 145.957c-2.188 7.055-1.484 13.648 2.11 19.777 3.597 6.128 8.522 9.192 14.772 9.192h137.4L197 47.536a2.63 2.63 0 0 1 2.515-1.86h51.15a2.63 2.63 0 0 1 2.512 3.406l-6.815 22.04a2.63 2.63 0 0 1-2.512 1.853h-33.764c-1.304 0-2.412.96-2.602 2.25-1.715 11.634-2.18 14.254-3.81 26.718l-1.15 7.618c-.073.484-.01.978.183 1.428l26.69 62.343a2.63 2.63 0 0 0 2.418 1.594h48.676c1.888 0 3.16-1.93 2.418-3.664l-24.53-57.296c-.743-1.735.53-3.665 2.417-3.665 7.658 0 14.574-2.55 20.752-7.66 6.172-5.105 10.587-12.021 13.246-20.752l15.475-50.42c2.188-7.054 1.484-13.648-2.11-19.777z"/>
            </svg>
            <span className="text-sm font-medium">Resellers Room</span>
          </Link>
        </div>
        <div className="flex-1 overflow-auto py-4">
          <nav className="grid items-start gap-2 px-3 text-sm font-medium">
            {menuSections.map((section, index) => (
              <div key={section.title || index} className={cn(
                "grid gap-1",
                index === 1 && "pt-6"
              )}>
                {section.title && (
                  <h2 className="text-xs font-medium text-black/60 mb-1">
                    {section.title}
                  </h2>
                )}
                <div className="grid gap-1">
                  {section.items.map((item) => {
                    const currentPath = location.pathname.split('/').filter(Boolean)[0] || 'dashboard';
                    const isActive = currentPath === item.path;
                    return (
                      <Link
                        key={item.text}
                        to={item.path}
                        className={cn(
                          "group flex items-center gap-x-3 rounded-md px-3 py-2 text-sm font-medium text-black transition-colors",
                          isActive
                            ? "bg-black/10 hover:bg-black/15"
                            : "hover:bg-black/5"
                        )}
                      >
                        {item.icon}
                        <span>{item.text}</span>
                      </Link>
                    );
                  })}
                </div>
              </div>
            ))}
          </nav>
        </div>
        <div className="relative mt-auto px-6 py-4">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                className="w-full justify-between px-2 group"
              >
                <div className="flex items-center gap-2">
                  <Avatar className="h-6 w-6">
                  <AvatarImage src="" alt="Jack Ely" />
                  <AvatarFallback className="text-xs">JE</AvatarFallback>
                </Avatar>
                  <span className="text-sm font-medium">Jack Ely</span>
                </div>
                <ChevronRight className="h-4 w-4 text-muted-foreground transition-transform group-data-[state=open]:rotate-90" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent 
              className="w-56 z-[60]" 
              align="start" 
              alignOffset={-8}
              sideOffset={8}
            >
              <DropdownMenuLabel className="font-normal">
                <div className="flex flex-col space-y-1">
                  <p className="text-sm font-medium">Jack Ely</p>
                  <p className="text-xs text-muted-foreground">jack.ely@example.com</p>
                </div>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuGroup>
                <DropdownMenuItem>
                  <User className="mr-2 h-4 w-4" />
                  <span>Profile</span>
                  <DropdownMenuShortcut>⇧⌘P</DropdownMenuShortcut>
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <CreditCard className="mr-2 h-4 w-4" />
                  <span>Billing</span>
                  <DropdownMenuShortcut>⌘B</DropdownMenuShortcut>
                </DropdownMenuItem>
                <DropdownMenuItem>
                  <Settings className="mr-2 h-4 w-4" />
                  <span>Settings</span>
                  <DropdownMenuShortcut>⌘S</DropdownMenuShortcut>
                </DropdownMenuItem>
              </DropdownMenuGroup>
              <DropdownMenuSeparator />
              <DropdownMenuItem>
                <LogOut className="mr-2 h-4 w-4" />
                <span>Log out</span>
                <DropdownMenuShortcut>⇧⌘Q</DropdownMenuShortcut>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </aside>

      {/* Main Content */}
      <div className="flex flex-1 flex-col lg:pl-56">
        {/* Header */}
        <header className="fixed top-0 right-0 left-0 z-40 flex h-14 items-center gap-4 border-b bg-background/95 backdrop-blur px-4 sm:px-6 lg:gap-6 lg:left-56">
          <button
            type="button"
            onClick={toggleSidebar}
            className="inline-flex items-center justify-center rounded-lg w-10 h-10 hover:bg-gray-100 lg:hidden relative z-50"
            aria-label="Toggle sidebar"
            aria-expanded={isSidebarOpen}
          >
            <Menu className="h-5 w-5" />
          </button>

          <div className="flex flex-1 items-center justify-end gap-x-4">
            <div className="flex items-center gap-x-4">
              <Button variant="ghost" size="icon" className="w-9 h-9 px-0">
                <Search className="h-4 w-4" />
              </Button>
              
              <Button variant="ghost" size="icon" className="w-9 h-9 px-0 relative">
                <Bell className="h-4 w-4" />
                <div className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] rounded-full bg-red-500 text-[11px] font-medium text-white grid place-items-center leading-none">4</div>
              </Button>

            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto flex flex-col page-gradient px-4 sm:px-6 pt-14">
          <div className="flex-1 flex flex-col">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};

export default DashboardLayout;
