import { useEffect, useRef } from 'react';
import { useConnectedAccounts } from '@/modules/email-calendar/hooks/useConnectedAccounts';
import { toast } from '@/hooks/use-toast';
import i18n from '@/lib/i18n';

/**
 * After OAuth login, automatically triggers the email account connection flow
 * so the user's email/calendar is connected without visiting Settings.
 *
 * Checks for a sessionStorage flag set during OAuth login and triggers
 * the connect flow once. If already connected, does nothing.
 */
export function useAutoConnectEmail() {
  const { accounts, connectAccount, loading } = useConnectedAccounts();
  const attemptedRef = useRef(false);

  useEffect(() => {
    if (loading || attemptedRef.current) return;

    const provider = sessionStorage.getItem('auto-connect-email-provider') as 'google' | 'microsoft' | null;
    if (!provider) return;

    // Clear immediately to prevent re-triggering
    sessionStorage.removeItem('auto-connect-email-provider');
    attemptedRef.current = true;

    // If user already has an account with this provider, skip
    const alreadyConnected = accounts.some(a => a.provider === provider);
    if (alreadyConnected) return;

    // Small delay to let the dashboard fully render before opening popup
    const timer = setTimeout(async () => {
      try {
        const account = await connectAccount(provider);
        if (account) {
          const t = i18n.t.bind(i18n);
          toast({
            title: t('auth.success'),
            description: t('auth.email_auto_connected', 'Email account connected for syncing.'),
          });
        }
      } catch (err) {
        // User may have closed the popup — that's fine, they can connect later
        console.info('Auto-connect email skipped or cancelled:', err);
      }
    }, 1500);

    return () => clearTimeout(timer);
  }, [loading, accounts, connectAccount]);
}
