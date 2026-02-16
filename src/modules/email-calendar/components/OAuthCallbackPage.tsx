/**
 * OAuth Callback Page
 * 
 * This page is opened in a popup window during the OAuth flow.
 * It receives the authorization code from Google/Microsoft and
 * the parent window polls this URL to extract the code.
 * 
 * No actual rendering needed — the popup is closed by the parent.
 */
export function OAuthCallbackPage() {
  return (
    <div className="flex items-center justify-center min-h-screen bg-background">
      <div className="text-center space-y-2">
        <div className="animate-spin h-8 w-8 border-4 border-primary border-t-transparent rounded-full mx-auto" />
        <p className="text-sm text-muted-foreground">Completing authentication...</p>
        <p className="text-xs text-muted-foreground">This window will close automatically.</p>
      </div>
    </div>
  );
}
