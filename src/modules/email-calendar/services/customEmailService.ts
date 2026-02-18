/**
 * Service for managing custom SMTP/IMAP email accounts.
 * Stores account configurations locally and communicates with edge functions for
 * sending/fetching emails.
 */

import type { CustomEmailConfig, ConnectedAccount, SyncStatus } from '../types';
import { DEFAULT_EMAIL_SETTINGS, DEFAULT_CALENDAR_SETTINGS } from '../types';
import { invokeEdgeFunction } from '@/integrations/supabase/client';

const STORAGE_KEY = 'custom-email-accounts';

// ─── Local Storage Helpers ───

function loadCustomAccounts(): ConnectedAccount[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return parsed.map((a: any) => ({
      ...a,
      lastSyncedAt: a.lastSyncedAt ? new Date(a.lastSyncedAt) : undefined,
      authFailedAt: a.authFailedAt ? new Date(a.authFailedAt) : null,
      createdAt: new Date(a.createdAt),
    }));
  } catch {
    return [];
  }
}

function saveCustomAccounts(accounts: ConnectedAccount[]) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(accounts));
}

// ─── Public API ───

export const customEmailService = {
  /** Get all custom email accounts */
  getAll(): ConnectedAccount[] {
    return loadCustomAccounts();
  },

  /** Add a new custom email account */
  async addAccount(config: CustomEmailConfig): Promise<ConnectedAccount> {
    // Test the connection first via edge function
    const testResult = await customEmailService.testConnection(config);
    if (!testResult.success) {
      throw new Error(testResult.error || 'Connection test failed');
    }

    const accounts = loadCustomAccounts();
    // Check if already exists
    const existing = accounts.find(a => a.handle === config.email);
    if (existing) {
      // Update existing
      existing.customConfig = config;
      existing.authFailedAt = null;
      existing.syncStatus = 'active';
      saveCustomAccounts(accounts);
      return existing;
    }

    const newAccount: ConnectedAccount = {
      id: `custom-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
      handle: config.email,
      provider: 'custom',
      syncStatus: 'active',
      lastSyncedAt: undefined,
      authFailedAt: null,
      createdAt: new Date(),
      emailSettings: { ...DEFAULT_EMAIL_SETTINGS },
      calendarSettings: { ...DEFAULT_CALENDAR_SETTINGS },
      customConfig: config,
    };

    accounts.push(newAccount);
    saveCustomAccounts(accounts);
    return newAccount;
  },

  /** Remove a custom email account */
  removeAccount(id: string): boolean {
    const accounts = loadCustomAccounts();
    const filtered = accounts.filter(a => a.id !== id);
    if (filtered.length === accounts.length) return false;
    saveCustomAccounts(filtered);
    return true;
  },

  /** Update sync status */
  updateSyncStatus(id: string, status: SyncStatus) {
    const accounts = loadCustomAccounts();
    const account = accounts.find(a => a.id === id);
    if (account) {
      account.syncStatus = status;
      if (status === 'active') {
        account.lastSyncedAt = new Date();
      }
      saveCustomAccounts(accounts);
    }
  },

  /** Test SMTP/IMAP connection via edge function */
  async testConnection(config: CustomEmailConfig): Promise<{ success: boolean; error?: string }> {
    try {
      const data = await invokeEdgeFunction<{ success: boolean; error?: string }>('custom-email', {
        action: 'test-connection',
        config: {
          email: config.email,
          password: config.password,
          imapServer: config.imapServer,
          imapPort: config.imapPort,
          imapSecurity: config.imapSecurity,
          smtpServer: config.smtpServer,
          smtpPort: config.smtpPort,
          smtpSecurity: config.smtpSecurity,
        },
      });
      return data;
    } catch (err: any) {
      return { success: false, error: err.message || 'Connection test failed' };
    }
  },

  /** Send email via SMTP through edge function */
  async sendEmail(
    config: CustomEmailConfig,
    to: string[],
    subject: string,
    body: string,
    bodyHtml?: string,
    cc?: string[],
    bcc?: string[]
  ): Promise<{ success: boolean; messageId?: string; error?: string }> {
    try {
      const data = await invokeEdgeFunction<{ success: boolean; messageId?: string; error?: string }>('custom-email', {
        action: 'send-email',
        config: {
          email: config.email,
          password: config.password,
          displayName: config.displayName,
          smtpServer: config.smtpServer,
          smtpPort: config.smtpPort,
          smtpSecurity: config.smtpSecurity,
        },
        email: { to, cc, bcc, subject, body, bodyHtml },
      });
      return data;
    } catch (err: any) {
      return { success: false, error: err.message || 'Failed to send email' };
    }
  },

  /** Fetch emails via IMAP through edge function */
  async fetchEmails(
    config: CustomEmailConfig,
    folder = 'INBOX',
    limit = 50
  ): Promise<{ success: boolean; emails?: any[]; error?: string }> {
    try {
      const data = await invokeEdgeFunction<{ success: boolean; emails?: any[]; error?: string }>('custom-email', {
        action: 'fetch-emails',
        config: {
          email: config.email,
          password: config.password,
          imapServer: config.imapServer,
          imapPort: config.imapPort,
          imapSecurity: config.imapSecurity,
        },
        options: { folder, limit },
      });
      return data;
    } catch (err: any) {
      return { success: false, error: err.message || 'Failed to fetch emails' };
    }
  },
};
