import { useEffect, useState } from "react";
import { AlertTriangle } from "lucide-react";
import { useTranslation } from "react-i18next";

// Public routes where session banner should never appear
const PUBLIC_ROUTES = ['/', '/login', '/user-login', '/onboarding', '/sso-callback'];

export function SessionExpiredBanner() {
  const [isExpired, setIsExpired] = useState(false);
  const { t } = useTranslation();

  useEffect(() => {
    const handleSessionExpired = () => {
      // Don't show banner if user is already on a public/login page
      const currentPath = window.location.pathname;
      if (PUBLIC_ROUTES.includes(currentPath)) {
        return;
      }
      setIsExpired(true);
    };

    window.addEventListener('auth:session-expired', handleSessionExpired);
    return () => window.removeEventListener('auth:session-expired', handleSessionExpired);
  }, []);

  const handleReconnect = () => {
    // Clear any remaining auth data
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('token_expires_at');
    // Use window.location since this component is outside BrowserRouter
    window.location.href = '/login';
  };

  if (!isExpired) return null;

  return (
    <div className="fixed top-0 left-0 right-0 z-[9999] bg-destructive text-destructive-foreground px-4 py-2 flex items-center justify-center gap-2 text-sm font-medium shadow-lg">
      <AlertTriangle className="h-4 w-4" />
      <span>Session expired.</span>
      <button
        onClick={handleReconnect}
        className="underline hover:no-underline font-semibold cursor-pointer"
      >
        Please reconnect
      </button>
    </div>
  );
}
