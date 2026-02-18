export type SidebarItemConfig = {
  title: string; // translation key or custom label
  url: string;
  icon?: string; // icon name from registry
  iconColor?: string; // per-item icon color override (hex like '#ff0000' or HSL like '217 91% 60%')
  description?: string;
  active?: boolean;
  group?: 'workspace' | 'crm' | 'service' | 'system';
  id?: string;
  dropdown?: Array<{
    title: string;
    url: string;
    description?: string;
  }>;
};

const STORAGE_KEY = 'app-sidebar-config';

const HIDDEN_SIDEBAR_URLS = new Set<string>([
  // System logs live under Settings → System; no need to show it in the global sidebar.
  '/dashboard/settings/logs',
]);

export function loadSidebarConfig(): SidebarItemConfig[] | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as SidebarItemConfig[];

    const filtered = parsed.filter((p) => !HIDDEN_SIDEBAR_URLS.has(p?.url));
    // Normalize titles that may have been saved with a '.title' suffix (e.g. 'orders.title')
    return filtered.map(p => ({
      ...p,
      title: typeof p.title === 'string' && p.title.endsWith('.title') ? p.title.replace(/\.title$/, '') : p.title
    }));
  } catch (e) {
    console.error('Failed to load sidebar config', e);
    return null;
  }
}

export function saveSidebarConfig(items: SidebarItemConfig[]) {
  try {
    // Normalize items to ensure UI filtering works reliably.
    const normalized = items.map((it, idx) => {
      const copy: any = { ...it };
      if (!copy.id) copy.id = `${Date.now()}-${idx}`;
    // If group is missing, default to workspace
    if (!copy.group) {
      copy.group = 'workspace';
    }
    if (copy.active === undefined) copy.active = true;
      // sanitize title if someone saved a dotted key like 'orders.title'
      if (typeof copy.title === 'string' && copy.title.endsWith('.title')) {
        copy.title = copy.title.replace(/\.title$/, '');
      }
      return copy as SidebarItemConfig;
    });
    localStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
  } catch (e) {
    console.error('Failed to save sidebar config', e);
  }
}

export function resetSidebarConfig() {
  localStorage.removeItem(STORAGE_KEY);
}

// DEV / fallback: seed defaults from the JSON bundle if local cache is empty
// This ensures the app has a stable starting point during development and
// that subsequent user edits are saved to the user's cache (localStorage).
// If `solutionMode` is provided, only items matching that mode are seeded.
import defaultsJson from '../data/sidebar-defaults.json';
import { ICON_OPTIONS } from '../components/sidebarIcons';

export function seedSidebarDefaultsIfEmpty() {
  try {
    const raw = loadSidebarConfig();
    if (raw && raw.length > 0) return; // already seeded
    const allDefaults = (defaultsJson as any).items || [];
    // normalize group literal to match SidebarItemConfig type
    const mapped: SidebarItemConfig[] = allDefaults.map((it: any, idx: number) => ({
      id: it.id || `${Date.now()}-${idx}`,
      tenantId: it.tenantId ?? null,
      title: it.title,
      url: it.url,
      description: it.description,
      icon: it.iconName ?? it.iconId ?? undefined,
      active: it.active ?? true,
      ...(it.group === 'system' ? { group: 'system' } : 
          it.group === 'crm' ? { group: 'crm' } :
          it.group === 'service' ? { group: 'service' } :
          { group: 'workspace' }),
      dropdown: it.dropdown ?? undefined,
      meta: it.meta ?? {},
    }));
    // resolve icons to registry names before saving
    const resolved = resolveIconsForDefaults(mapped as SidebarItemConfig[]);
    saveSidebarConfig(resolved);
  } catch (e) {
    // fail silently for environments where JSON import may not work
    // caller will fall back to other defaults
    // eslint-disable-next-line no-console
    console.warn('Failed to seed sidebar defaults from JSON', e);
  }
}

// Merge missing defaults into existing config without removing user items.
// This will add any default entries (matched by url) that are not present in
// the user's saved config. It purposely does not remove or modify existing
// items so it's safe to run on app upgrade.
// Merges missing defaults. Returns array of added SidebarItemConfig items.
export function mergeMissingDefaults(defaults: SidebarItemConfig[], insertBeforeUrl?: string): SidebarItemConfig[] {
  try {
    const raw = loadSidebarConfig();
    if (!raw || raw.length === 0) {
      // no saved config, just write defaults
      // resolve any iconId/iconName to registry names before saving
      const resolved = resolveIconsForDefaults(defaults);
      saveSidebarConfig(resolved);
      return resolved;
    }

    const existingUrls = new Set(raw.map(r => r.url));
    const missing = defaults.filter(d => !existingUrls.has(d.url));
    if (missing.length === 0) return [];

    // resolve icons for the missing items too
    const resolvedMissing = resolveIconsForDefaults(missing);

    // If insertBeforeUrl is provided, insert missing items before that item in the user's config
    let merged: SidebarItemConfig[];
    if (insertBeforeUrl) {
      const idx = raw.findIndex(r => r.url === insertBeforeUrl);
      if (idx >= 0) {
        merged = [...raw.slice(0, idx), ...resolvedMissing.map(m => ({ ...m })), ...raw.slice(idx)];
      } else {
        merged = [...raw, ...resolvedMissing.map(m => ({ ...m }))];
      }
    } else {
      merged = [...raw, ...resolvedMissing.map(m => ({ ...m }))];
    }

    saveSidebarConfig(merged);
    return resolvedMissing;
  } catch (e) {
    // non-fatal
    // eslint-disable-next-line no-console
    console.warn('Failed to merge sidebar defaults', e);
    return [];
  }
}

// Attempt to map iconId or iconName from bundled defaults to a registry icon name
function resolveIconsForDefaults(items: SidebarItemConfig[]) {
  try {
    const bundled = (defaultsJson as any).icons || [];
    const byId: Record<string,string> = {};
    const byName: Record<string,string> = {};
    for (const ic of bundled) {
      if (ic.id) byId[ic.id] = ic.name;
      if (ic.name) byName[ic.name] = ic.name;
    }

    return items.map(it => {
      const resolved = { ...it } as any;
      // if it.icon already looks like a registry name, keep it
      if (resolved.icon && ICON_OPTIONS.includes(resolved.icon as any)) return resolved as SidebarItemConfig;

      // try to resolve from bundled icons: check iconName then iconId
      const candidateFromBundle = (byName[resolved.icon] || byId[resolved.icon]) || undefined;
      if (candidateFromBundle && ICON_OPTIONS.includes(candidateFromBundle as any)) {
        resolved.icon = candidateFromBundle;
        return resolved as SidebarItemConfig;
      }

      // fallback: if icon is a literal component name like 'Home' or 'Calendar', try to match
      if (typeof resolved.icon === 'string') {
        const match = ICON_OPTIONS.find(opt => opt.toLowerCase() === (resolved.icon as string).toLowerCase() || opt.toLowerCase() === (resolved.icon as string).replace(/[-_]/g, '').toLowerCase());
        if (match) resolved.icon = match;
      }

      return resolved as SidebarItemConfig;
    });
  } catch (e) {
    return items;
  }
}
