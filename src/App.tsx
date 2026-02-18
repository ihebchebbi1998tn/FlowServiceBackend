import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import React, { useEffect, Suspense } from "react";
import Login from "./modules/auth/pages/Login";
import UserLogin from "./modules/auth/pages/UserLogin";
// SSO callback route kept for OAuth callback page
import ApiTestsPage from "./modules/testing/pages/ApiTestsPage";
// Dashboard is gated to show a preloading screen and warm data/assets
import DashboardGate from "./modules/dashboard/components/DashboardGate";
import Onboarding from "./modules/onboarding/pages/Onboarding";
import NotFound from "./pages/NotFound";
import PublicFormPage from "./modules/dynamic-forms/pages/PublicFormPage";
const PublicWebsitePage = React.lazy(() => import("./modules/website-builder/pages/PublicWebsitePage"));
const PublicDashboardPage = React.lazy(() => import("./modules/dashboard-builder/pages/PublicDashboardPage"));
import SupportModuleRoutes from "./modules/support/SupportModuleRoutes";
import { OAuthCallbackPage } from "./modules/email-calendar/components/OAuthCallbackPage";
import { LookupsProvider } from "./shared/contexts/LookupsContext";
import { LoadingProvider } from "./shared";
import { TopProgressBar } from "./shared/components/TopProgressBar";
import "./lib/i18n";
import { ScrollToTop } from "./shared/components/ScrollToTop";
import AppLoader from "./shared/components/AppLoader";
import { runWhenIdle, preloadDashboard, preloadSupport, preloadOnboarding, preloadLogin } from "./shared/prefetch";
import { AuthProvider } from "./contexts/AuthContext";
import { PreferencesProvider } from "./contexts/PreferencesProvider";
import { PreferencesLoader } from "./components/PreferencesLoader";
import { SessionExpiredBanner } from "./components/SessionExpiredBanner";
// ErrorBoundary component is available at ./components/ErrorBoundary for module-level use
import { useErrorTracking } from "./hooks/useErrorTracking";
import { logger } from "./hooks/useLogger";

import { setQueryClient } from "./services/ai/aiTaskCreationService";
import { setQueryClientForFormCreation } from "./services/ai/aiFormCreationService";

// Import optimized QueryClient with caching configuration
import { queryClient } from "./lib/queryClient";

// Set the query client reference for AI services
setQueryClient(queryClient);
setQueryClientForFormCreation(queryClient);


// Global error tracking component
const GlobalErrorTracker: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  useErrorTracking({
    enableConsoleCapture: true,
    enableUnhandledRejections: true,
    enableWindowErrors: true,
  });
  return <>{children}</>;
};

// Enhanced App Error Boundary with logging
class AppErrorBoundary extends React.Component<{ children: React.ReactNode }, { hasError: boolean; errorId: string | null }> {
  constructor(props: any) {
    super(props);
    this.state = { hasError: false, errorId: null };
  }
  
  static getDerivedStateFromError() { 
    return { hasError: true }; 
  }
  
  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    const errorId = `APP-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
    this.setState({ errorId });
    
    // Log to backend
    logger.error(
      `Application crashed: ${error.message}`,
      'App',
      'other',
      {
        entityType: 'AppCrash',
        entityId: errorId,
        details: JSON.stringify({
          errorId,
          name: error.name,
          message: error.message,
          stack: error.stack,
          componentStack: errorInfo.componentStack,
          url: window.location.href,
          timestamp: new Date().toISOString(),
        }),
      }
    ).catch(() => {});
  }
  
  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center p-6 bg-background">
          <div className="max-w-md w-full text-center bg-card border border-border rounded-2xl p-8 shadow-lg">
            <div className="flex flex-col items-center gap-4">
              <div className="w-20 h-20 rounded-full bg-destructive/10 flex items-center justify-center">
                <svg className="w-10 h-10 text-destructive" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.996-.833-2.764 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z" />
                </svg>
              </div>
              
              <div className="space-y-2">
                <h2 className="text-xl font-semibold text-foreground">Oops! Something went wrong</h2>
                <p className="text-sm text-muted-foreground">
                  We encountered an unexpected error while loading the application. Our team has been notified.
                </p>
                {this.state.errorId && (
                  <p className="text-xs text-muted-foreground font-mono">
                    Error ID: {this.state.errorId}
                  </p>
                )}
              </div>

              <button 
                onClick={() => window.location.reload()}
                className="inline-flex items-center gap-2 px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 transition-colors font-medium"
              >
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                Reload Page
              </button>
            </div>
          </div>
        </div>
      );
    }
    return this.props.children as any;
  }
}

const App = () => {
  useEffect(() => {
    // ─── Purge stale caches on app start ───
    // Clear old logo caches so fresh data is fetched from the server
    localStorage.removeItem('company-logo');
    localStorage.removeItem('company-logo-blob-data');
    // Clear cached PDF settings (logo embedded inside, lookups references, etc.)
    ['pdf-settings', 'pdf-settings-offers', 'pdf-settings-sales', 'pdf-settings-service-orders', 'pdf-settings-dispatches', 'pdf-settings-payments'].forEach(k => localStorage.removeItem(k));
    // Clear sidebar config cache so it re-seeds from defaults
    // (Don't clear sidebar config — user customizations should persist)
    console.log('[App] Cleared stale logo & PDF caches on startup');

    // Initialize theme on app start
    const savedTheme = localStorage.getItem('flowsolution-theme') || 'system';
    const root = window.document.documentElement;
    
    // Always clear existing theme classes first
    root.classList.remove('light', 'dark');
    
    if (savedTheme === 'system') {
      const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
      root.classList.add(systemTheme);
    } else {
      root.classList.add(savedTheme);
    }

    // Warm critical routes during idle to improve perceived nav speed
    runWhenIdle(() => {
      preloadDashboard();
      preloadOnboarding();
      preloadSupport();
      preloadLogin();
    }, 800);
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <PreferencesProvider>
          <PreferencesLoader />
          <LoadingProvider>
          <GlobalErrorTracker>
          <LookupsProvider>
            <TooltipProvider>
              <TopProgressBar />
              <SessionExpiredBanner />
              
              <Toaster />
              <Sonner />
              <BrowserRouter>
                <ScrollToTop />
                <AppErrorBoundary>
                  <Suspense fallback={<AppLoader />}>
                   <Routes>
                     <Route path="/" element={<Login />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/user-login" element={<UserLogin />} />
                    {/* OAuth callback route for email/calendar */}
                    <Route path="/onboarding" element={<Onboarding />} />
                    {/* API Testing System */}
                    <Route path="/tests" element={<ApiTestsPage />} />
                    {/* Redirect standalone paths into dashboard so sidebar shows */}
                    <Route path="/offers" element={<Navigate to="/dashboard/offers" replace />} />
                    <Route path="/offers/*" element={<Navigate to="/dashboard/offers" replace />} />
                    <Route path="/calendar" element={<Navigate to="/dashboard/calendar" replace />} />
                    <Route path="/calendar/*" element={<Navigate to="/dashboard/calendar" replace />} />
                   <Route path="/dashboard/*" element={<DashboardGate />} />
                    {/* Customer Support Module */}
                    <Route path="/support/*" element={<SupportModuleRoutes />} />
                    {/* Public Forms (no authentication required) */}
                    <Route path="/public/forms/:slug" element={<PublicFormPage />} />
                    {/* Public Dashboards (no authentication required) */}
                    <Route path="/public/dashboards/:token" element={<Suspense fallback={<AppLoader />}><PublicDashboardPage /></Suspense>} />
                    {/* Public Websites (no authentication required) */}
                    <Route path="/public/sites/:siteSlug" element={<PublicWebsitePage />} />
                    <Route path="/public/sites/:siteSlug/:pageSlug" element={<PublicWebsitePage />} />
                    {/* OAuth callback pages (opened in popup during Gmail/Outlook connection) */}
                    <Route path="/oauth/callback" element={<OAuthCallbackPage />} />
                    <Route path="/oauth/google/callback" element={<OAuthCallbackPage />} />
                    <Route path="/oauth/microsoft/callback" element={<OAuthCallbackPage />} />
                    {/* ADD ALL CUSTOM ROUTES ABOVE THE CATCH-ALL "*" ROUTE */}
                    <Route path="*" element={<NotFound />} />
                  </Routes>
                  </Suspense>
                </AppErrorBoundary>
              </BrowserRouter>
            </TooltipProvider>
          </LookupsProvider>
          </GlobalErrorTracker>
          </LoadingProvider>
        </PreferencesProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
};

export default App;
