/**
 * Subscription API Service
 * Handles all subscription/billing related API calls.
 * Uses mock data until backend endpoints are ready.
 */
import { apiFetch } from './apiClient';

// ─── Types ───

export type PlanKey = 'free' | 'starter' | 'pro' | 'enterprise';
export type SubscriptionStatus = 'active' | 'trialing' | 'past_due' | 'canceled' | 'incomplete';
export type BillingInterval = 'monthly' | 'yearly';
export type InvoiceStatus = 'paid' | 'pending' | 'failed' | 'void';

export interface SubscriptionPlan {
  id: number;
  planKey: PlanKey;
  name: string;
  description: string;
  monthlyPricePerSeat: number;
  yearlyPricePerSeat: number;
  currency: string;
  maxSeats: number | null;
  creditsPerPeriod: number;
  features: string[];
  isActive: boolean;
  sortOrder: number;
}

export interface Subscription {
  id: number;
  tenantId: number;
  planKey: PlanKey;
  status: SubscriptionStatus;
  interval: BillingInterval;
  pricePerSeat: number;
  currency: string;
  seats: number;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEnd: string | null;
  stripeSubscriptionId: string | null;
  stripeCustomerId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SubscriptionUsage {
  id: number;
  subscriptionId: number;
  usageType: string;
  usedAmount: number;
  grantedAmount: number;
  rolloverAmount: number;
  periodStart: string;
  periodEnd: string;
}

export interface BillingInvoice {
  id: number;
  subscriptionId: number;
  stripeInvoiceId: string | null;
  amount: number;
  currency: string;
  status: InvoiceStatus;
  invoiceDate: string;
  paidAt: string | null;
  pdfUrl: string | null;
  description: string;
}

// ─── Mock Data (used until backend is ready) ───

const USE_MOCK = true;

const MOCK_PLANS: SubscriptionPlan[] = [
  {
    id: 1, planKey: 'free', name: 'Free', description: 'Basic features for small teams',
    monthlyPricePerSeat: 0, yearlyPricePerSeat: 0, currency: 'TND',
    maxSeats: 3, creditsPerPeriod: 100, isActive: true, sortOrder: 0,
    features: ['3 users', '100 credits/month', 'Basic CRM', 'Email support'],
  },
  {
    id: 2, planKey: 'starter', name: 'Starter', description: 'Growing teams with more power',
    monthlyPricePerSeat: 35, yearlyPricePerSeat: 27, currency: 'TND',
    maxSeats: 10, creditsPerPeriod: 1000, isActive: true, sortOrder: 1,
    features: ['10 users', '1,000 credits/month', 'Full CRM', 'Workflows', 'Priority support'],
  },
  {
    id: 3, planKey: 'pro', name: 'Pro', description: 'Advanced features for scaling businesses',
    monthlyPricePerSeat: 89, yearlyPricePerSeat: 72, currency: 'TND',
    maxSeats: 50, creditsPerPeriod: 10000, isActive: true, sortOrder: 2,
    features: ['50 users', '10,000 credits/month', 'AI assistant', 'Custom forms', 'Advanced analytics'],
  },
  {
    id: 4, planKey: 'enterprise', name: 'Enterprise', description: 'Unlimited power for large organizations',
    monthlyPricePerSeat: 179, yearlyPricePerSeat: 149, currency: 'TND',
    maxSeats: null, creditsPerPeriod: 100000, isActive: true, sortOrder: 3,
    features: ['Unlimited users', '100,000 credits/month', 'Dedicated support', 'Custom integrations', 'SSO', 'SLA guarantee'],
  },
];

const MOCK_SUBSCRIPTION: Subscription = {
  id: 1, tenantId: 1, planKey: 'pro', status: 'active', interval: 'monthly',
  pricePerSeat: 89, currency: 'TND', seats: 5,
  currentPeriodStart: '2026-02-01T00:00:00Z',
  currentPeriodEnd: '2026-03-01T00:00:00Z',
  trialEnd: null,
  stripeSubscriptionId: 'sub_mock_123',
  stripeCustomerId: 'cus_mock_456',
  createdAt: '2025-06-15T10:00:00Z',
  updatedAt: '2026-02-01T00:00:00Z',
};

const MOCK_USAGE: SubscriptionUsage[] = [
  {
    id: 1, subscriptionId: 1, usageType: 'workflow_credits',
    usedAmount: 3420, grantedAmount: 10000, rolloverAmount: 500,
    periodStart: '2026-02-01T00:00:00Z', periodEnd: '2026-03-01T00:00:00Z',
  },
];

const MOCK_INVOICES: BillingInvoice[] = [
  { id: 1, subscriptionId: 1, stripeInvoiceId: 'inv_001', amount: 445, currency: 'TND', status: 'paid', invoiceDate: '2026-02-01T00:00:00Z', paidAt: '2026-02-01T08:30:00Z', pdfUrl: null, description: 'Pro Plan - 5 seats - February 2026' },
  { id: 2, subscriptionId: 1, stripeInvoiceId: 'inv_002', amount: 445, currency: 'TND', status: 'paid', invoiceDate: '2026-01-01T00:00:00Z', paidAt: '2026-01-01T09:00:00Z', pdfUrl: null, description: 'Pro Plan - 5 seats - January 2026' },
  { id: 3, subscriptionId: 1, stripeInvoiceId: 'inv_003', amount: 445, currency: 'TND', status: 'paid', invoiceDate: '2025-12-01T00:00:00Z', paidAt: '2025-12-01T10:00:00Z', pdfUrl: null, description: 'Pro Plan - 5 seats - December 2025' },
  { id: 4, subscriptionId: 1, stripeInvoiceId: 'inv_004', amount: 356, currency: 'TND', status: 'paid', invoiceDate: '2025-11-01T00:00:00Z', paidAt: '2025-11-01T07:45:00Z', pdfUrl: null, description: 'Pro Plan - 4 seats - November 2025' },
];

const mockDelay = <T>(data: T, ms = 400): Promise<{ data: T }> =>
  new Promise(resolve => setTimeout(() => resolve({ data }), ms));

// ─── API ───

const BASE = '/api/subscriptions';

export const subscriptionApi = {
  getPlans: (): Promise<{ data: SubscriptionPlan[] }> =>
    USE_MOCK ? mockDelay(MOCK_PLANS) : apiFetch<SubscriptionPlan[]>(`${BASE}/plans`),

  getCurrentSubscription: (): Promise<{ data: Subscription }> =>
    USE_MOCK ? mockDelay(MOCK_SUBSCRIPTION) : apiFetch<Subscription>(`${BASE}/current`),

  getUsage: (): Promise<{ data: SubscriptionUsage[] }> =>
    USE_MOCK ? mockDelay(MOCK_USAGE) : apiFetch<SubscriptionUsage[]>(`${BASE}/usage`),

  getInvoices: (): Promise<{ data: BillingInvoice[] }> =>
    USE_MOCK ? mockDelay(MOCK_INVOICES) : apiFetch<BillingInvoice[]>(`${BASE}/invoices`),

  switchPlan: (planKey: PlanKey): Promise<{ data: Subscription }> =>
    USE_MOCK
      ? mockDelay({ ...MOCK_SUBSCRIPTION, planKey })
      : apiFetch<Subscription>(`${BASE}/switch-plan`, { method: 'POST', body: JSON.stringify({ planKey }) }),

  switchInterval: (interval: BillingInterval): Promise<{ data: Subscription }> =>
    USE_MOCK
      ? mockDelay({ ...MOCK_SUBSCRIPTION, interval })
      : apiFetch<Subscription>(`${BASE}/switch-interval`, { method: 'POST', body: JSON.stringify({ interval }) }),

  cancel: (): Promise<{ data: { success: boolean } }> =>
    USE_MOCK
      ? mockDelay({ success: true })
      : apiFetch<{ success: boolean }>(`${BASE}/cancel`, { method: 'POST' }),

  getBillingPortalUrl: (): Promise<{ data: { url: string } }> =>
    USE_MOCK
      ? mockDelay({ url: '#billing-portal' })
      : apiFetch<{ url: string }>(`${BASE}/billing-portal`),
};
