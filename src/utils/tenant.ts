/**
 * Multi-tenant subdomain detection utility.
 * 
 * Extracts the tenant identifier from the current hostname.
 * For example: demo.flowentra.app → "demo"
 * 
 * Priority chain:
 * 1. localStorage override (for dev/testing)
 * 2. Subdomain of BASE_DOMAIN (e.g. demo.flowentra.app → "demo")
 * 3. Query param ?tenant=xxx (useful for preview URLs)
 * 4. VITE_DEFAULT_TENANT env var (fallback)
 * 5. null (no tenant — uses default DB)
 */

const BASE_DOMAIN = 'flowentra.app';

const DEFAULT_TENANT = import.meta.env.VITE_DEFAULT_TENANT || null;

const TENANT_OVERRIDE_KEY = 'tenant_override';

export function getTenantFromHostname(hostname: string = window.location.hostname): string | null {
  // 1. Manual override via localStorage
  const override = localStorage.getItem(TENANT_OVERRIDE_KEY);
  if (override) {
    return override.trim().toLowerCase();
  }

  // 2. Subdomain detection on production domain
  if (hostname.endsWith(`.${BASE_DOMAIN}`)) {
    const subdomain = hostname.slice(0, hostname.length - `.${BASE_DOMAIN}`.length);
    if (subdomain && !subdomain.includes('.')) {
      return subdomain.toLowerCase();
    }
  }

  // 3. Query param ?tenant=xxx (useful for Lovable preview / any environment)
  try {
    const url = new URL(window.location.href);
    const tenantParam = url.searchParams.get('tenant');
    if (tenantParam) {
      return tenantParam.trim().toLowerCase();
    }
  } catch {
    // ignore
  }

  // 4. Env var fallback (only if explicitly set and non-empty)
  if (DEFAULT_TENANT) {
    return DEFAULT_TENANT;
  }

  // 5. No tenant detected
  return null;
}

/**
 * Returns the current tenant identifier (cached per page load).
 */
let cachedTenant: string | null | undefined;
export function getCurrentTenant(): string | null {
  if (cachedTenant === undefined) {
    cachedTenant = getTenantFromHostname();
  }
  return cachedTenant;
}

/**
 * Set a tenant override (for dev/preview environments).
 * Reloads the page to apply.
 */
export function setTenantOverride(tenant: string | null): void {
  if (tenant) {
    localStorage.setItem(TENANT_OVERRIDE_KEY, tenant);
  } else {
    localStorage.removeItem(TENANT_OVERRIDE_KEY);
  }
  cachedTenant = undefined;
  window.location.reload();
}
