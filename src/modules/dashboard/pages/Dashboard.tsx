import { SidebarProvider, useSidebar } from "@/components/ui/sidebar";
import { AppSidebar } from "@/modules/dashboard/components/AppSidebar";
import { DashboardHeader } from "@/modules/dashboard/components/DashboardHeader";
import { DashboardContent } from "@/modules/dashboard/components/DashboardContent";
import { TopNavigation } from "@/components/navigation/TopNavigation";
import { LayoutModeProvider } from "@/components/providers/LayoutModeProvider";
import { useLayoutModeContext } from "@/hooks/useLayoutMode";
import { useLocation, Navigate } from "react-router-dom";
import { useEffect } from "react";
import { authService } from "@/services/authService";
import { ProductTourProvider } from "@/contexts/ProductTourContext";
import { ProductTour } from "@/components/onboarding/ProductTour";
import { useProductTourContext } from "@/contexts/ProductTourContext";
import { CommandPalette } from "@/components/ui/command-palette";
import { useAutoConnectEmail } from "@/hooks/useAutoConnectEmail";

function DashboardLayout() {
  const { layoutMode, isMobile } = useLayoutModeContext();
  const { isRunning, endTour } = useProductTourContext();
  const location = useLocation();
  const _isOnDashboardHome = location.pathname === "/dashboard";
  
  // Auto-connect email account after OAuth login
  useAutoConnectEmail();
  
  // Check if user has completed onboarding from server data
  const userData = authService.getCurrentUserFromStorage();
  const hasCompletedOnboarding = userData?.onboardingCompleted || localStorage.getItem('onboarding-completed');
  
  if (!hasCompletedOnboarding) {
    return <Navigate to="/onboarding" replace />;
  }

  // Mobile view - always render content (dashboard home is empty for now)
  if (isMobile) {
    return (
      <div className="min-h-screen bg-background">
        <ProductTour isRunning={isRunning} onEnd={endTour} />
        <CommandPalette />
        <TopNavigation />
        <main className="flex-1">
          <DashboardContent />
        </main>
      </div>
    );
  }

  // Desktop/Tablet view - respect layout mode preference
  if (layoutMode === 'topbar') {
    return (
      <div className="min-h-screen bg-background">
        <ProductTour isRunning={isRunning} onEnd={endTour} />
        <CommandPalette />
        <DashboardHeader />
        <TopNavigation />
        <main className="flex-1">
          <DashboardContent />
        </main>
      </div>
    );
  }

  // Auto-collapse sidebar on specific routes
  function SidebarWrapper() {
    const location = useLocation();
    const { state, setOpen, open } = useSidebar();

    const isDispatcher = location.pathname === '/dashboard/field/dispatcher/interface';
    const shouldAutoCollapse = isDispatcher;
    
    useEffect(() => {
      const key = 'sidebar:auto-collapsed-prev-open';
      if (shouldAutoCollapse) {
        // Store current state before collapsing
        try { sessionStorage.setItem(key, JSON.stringify(open)); } catch (e) { /* ignore */ }
        setOpen(false);
      } else {
        // Leaving: restore sidebar to previous state
        try {
          const prev = sessionStorage.getItem(key);
          if (prev !== null) {
            setOpen(true);
            sessionStorage.removeItem(key);
          }
        } catch (e) { /* ignore */ }
      }
    }, [location.pathname, setOpen, open, shouldAutoCollapse]);

    return (
      <>
        <AppSidebar />
        <div className="flex-1 flex flex-col relative">
          <DashboardHeader />
          <main className="flex-1 overflow-auto">
            <DashboardContent />
          </main>
        </div>
      </>
    );
  }

  // Default sidebar layout
  return (
    <SidebarProvider>
      <div className="min-h-screen flex w-full bg-background">
        <ProductTour isRunning={isRunning} onEnd={endTour} />
        <CommandPalette />
        <SidebarWrapper />
      </div>
    </SidebarProvider>
  );
}

const Dashboard = () => {
  return (
    <ProductTourProvider>
      <LayoutModeProvider>
        <DashboardLayout />
      </LayoutModeProvider>
    </ProductTourProvider>
  );
};

export default Dashboard;