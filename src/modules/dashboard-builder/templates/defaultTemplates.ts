import type { DashboardWidget } from '../types';

/**
 * Resolves the user's --primary CSS variable to hex at runtime.
 */
function getPrimaryHex(): string {
  try {
    const raw = getComputedStyle(document.documentElement).getPropertyValue('--primary').trim();
    if (!raw) return '#e63b2e';
    const [h, s, l] = raw.split(/\s+/).map(v => parseFloat(v));
    return hslToHex(h, s, l);
  } catch {
    return '#e63b2e';
  }
}

function hslToHex(h: number, s: number, l: number): string {
  s /= 100; l /= 100;
  const a = s * Math.min(l, 1 - l);
  const f = (n: number) => {
    const k = (n + h / 30) % 12;
    const color = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
    return Math.round(255 * color).toString(16).padStart(2, '0');
  };
  return `#${f(0)}${f(8)}${f(4)}`;
}

function lighten(hex: string, amount: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  const mix = (c: number) => Math.round(c + (255 - c) * amount).toString(16).padStart(2, '0');
  return `#${mix(r)}${mix(g)}${mix(b)}`;
}

function darken(hex: string, amount: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  const mix = (c: number) => Math.round(c * (1 - amount)).toString(16).padStart(2, '0');
  return `#${mix(r)}${mix(g)}${mix(b)}`;
}

// ════════════════════════════════════════════════════════════════
// CRM DASHBOARD — Sales & Revenue focused
// Layout: 4 KPIs → Revenue trend + Sales pipeline → Sparkline + Offers donut + Top sales table
// ════════════════════════════════════════════════════════════════

function createCrmWidgets(): DashboardWidget[] {
  const p = getPrimaryHex();
  const p2 = lighten(p, 0.25);
  const p3 = darken(p, 0.15);
  const p4 = lighten(p, 0.40);

  return [
    // ── Row 0: 4 KPI cards (h=2) ──
    {
      id: 'crm-revenue', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.salesRevenue',
      dataSource: 'sales', metric: 'revenue',
      layout: { x: 0, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        prefix: '', fontSize: 'lg', fontWeight: 'bold', icon: 'DollarSign', color: p,
        kpiBg: { style: 'gradient', color1: p, color2: p3, gradientAngle: 135, opacity: 90, textLight: true, effect: 'glow' },
      },
    },
    {
      id: 'crm-sales-count', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalSales',
      dataSource: 'sales', metric: 'count',
      layout: { x: 3, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'ShoppingCart', color: p,
        kpiBg: { style: 'subtle', color1: p, opacity: 8, effect: 'wave' },
      },
    },
    {
      id: 'crm-offers-count', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalOffers',
      dataSource: 'offers', metric: 'count',
      layout: { x: 6, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'FileText', color: p2,
        kpiBg: { style: 'subtle', color1: p2, opacity: 8, effect: 'dots' },
      },
    },
    {
      id: 'crm-contacts', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalContacts',
      dataSource: 'contacts', metric: 'count',
      layout: { x: 9, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Users', color: p3,
        kpiBg: { style: 'subtle', color1: p3, opacity: 8, effect: 'rings' },
      },
    },

    // ── Row 2: Sales trend (wide) + Sales pipeline bar ──
    {
      id: 'crm-sales-trend', type: 'area',
      titleKey: 'dashboardBuilder.presets.salesTrend',
      descriptionKey: 'dashboardBuilder.presets.salesTrendDesc',
      dataSource: 'sales', metric: 'monthlyTrend',
      layout: { x: 0, y: 2, w: 7, h: 4, minW: 4, minH: 3 },
      config: { color: p, showGrid: true, animated: true },
    },
    {
      id: 'crm-sales-status', type: 'bar',
      titleKey: 'dashboardBuilder.presets.salesStatus',
      descriptionKey: 'dashboardBuilder.presets.salesStatusDesc',
      dataSource: 'sales', metric: 'statusBreakdown',
      layout: { x: 7, y: 2, w: 5, h: 4, minW: 3, minH: 3 },
      config: { color: p, showLabels: true, borderRadius: 8 },
    },

    // ── Row 6: Conversion sparkline + Offers donut + Top sales table ──
    {
      id: 'crm-conversion-spark', type: 'sparkline',
      titleKey: 'dashboardBuilder.presets.conversionRate',
      dataSource: 'sales', metric: 'monthlyTrend',
      layout: { x: 0, y: 6, w: 3, h: 3, minW: 2, minH: 2 },
      config: { color: p, sparklineType: 'area', suffix: '%' },
    },
    {
      id: 'crm-offers-pipeline', type: 'donut',
      titleKey: 'dashboardBuilder.presets.offersPipeline',
      descriptionKey: 'dashboardBuilder.presets.offersPipelineDesc',
      dataSource: 'offers', metric: 'statusBreakdown',
      layout: { x: 3, y: 6, w: 4, h: 4, minW: 3, minH: 3 },
      config: { showLegend: true, innerRadius: 50, animated: true },
    },
    {
      id: 'crm-top-sales', type: 'table',
      titleKey: 'dashboardBuilder.presets.topSales',
      dataSource: 'sales', metric: 'topItems',
      layout: { x: 7, y: 6, w: 5, h: 4, minW: 3, minH: 3 },
    },

    // ── Row 10: Conversion gauge + Offers trend ──
    {
      id: 'crm-conversion-gauge', type: 'gauge',
      titleKey: 'dashboardBuilder.presets.conversionRate',
      dataSource: 'sales', metric: 'conversionRate',
      layout: { x: 0, y: 10, w: 3, h: 4, minW: 2, minH: 3 },
      config: { color: p, animated: true },
    },
    {
      id: 'crm-offers-trend', type: 'line',
      titleKey: 'dashboardBuilder.presets.offersPipeline',
      descriptionKey: 'dashboardBuilder.presets.offersPipelineDesc',
      dataSource: 'offers', metric: 'monthlyTrend',
      layout: { x: 3, y: 10, w: 9, h: 4, minW: 4, minH: 3 },
      config: { color: p2, showGrid: true },
    },
  ];
}

// ════════════════════════════════════════════════════════════════
// FIELD OPERATIONS — Service Orders, Tasks, Dispatches
// Layout: 4 KPIs → Tasks donut + SO bar → Map + Dispatch funnel + Task trend
// ════════════════════════════════════════════════════════════════

function createFieldWidgets(): DashboardWidget[] {
  const p = getPrimaryHex();
  const p2 = lighten(p, 0.20);
  const p3 = darken(p, 0.12);

  return [
    // ── Row 0: 4 KPI cards ──
    {
      id: 'field-so-count', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.activeServiceOrders',
      dataSource: 'serviceOrders', metric: 'count',
      layout: { x: 0, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        fontSize: 'lg', icon: 'ClipboardList', color: p,
        kpiBg: { style: 'gradient', color1: p, color2: p3, opacity: 90, textLight: true, effect: 'wave' },
      },
    },
    {
      id: 'field-tasks', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalTasks',
      dataSource: 'tasks', metric: 'count',
      layout: { x: 3, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'CheckCircle', color: p,
        kpiBg: { style: 'subtle', color1: p, opacity: 8, effect: 'dots' },
      },
    },
    {
      id: 'field-dispatches', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalDispatches',
      dataSource: 'dispatches', metric: 'count',
      layout: { x: 6, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Send', color: p2,
        kpiBg: { style: 'subtle', color1: p2, opacity: 8, effect: 'rings' },
      },
    },
    {
      id: 'field-completion', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.completionRate',
      dataSource: 'tasks', metric: 'completionRate',
      layout: { x: 9, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Target', color: p3, suffix: '%',
        kpiBg: { style: 'subtle', color1: p3, opacity: 8, effect: 'glow' },
      },
    },

    // ── Row 2: Task donut + Service order bar ──
    {
      id: 'field-task-status', type: 'donut',
      titleKey: 'dashboardBuilder.presets.tasksByStatus',
      descriptionKey: 'dashboardBuilder.presets.tasksByStatusDesc',
      dataSource: 'tasks', metric: 'statusBreakdown',
      layout: { x: 0, y: 2, w: 5, h: 4, minW: 3, minH: 3 },
      config: { showLegend: true, innerRadius: 50, animated: true },
    },
    {
      id: 'field-so-status', type: 'bar',
      titleKey: 'dashboardBuilder.presets.serviceOrderStatus',
      descriptionKey: 'dashboardBuilder.presets.serviceOrderStatusDesc',
      dataSource: 'serviceOrders', metric: 'statusBreakdown',
      layout: { x: 5, y: 2, w: 7, h: 4, minW: 4, minH: 3 },
      config: { color: p, showLabels: true, borderRadius: 8 },
    },

    // ── Row 6: Dispatch map + Dispatch funnel + Task trend ──
    {
      id: 'field-dispatch-map', type: 'map',
      titleKey: 'dashboardBuilder.presets.dispatchLocations',
      dataSource: 'dispatches', metric: 'count',
      layout: { x: 0, y: 6, w: 5, h: 5, minW: 3, minH: 3 },
      config: { color: p },
    },
    {
      id: 'field-dispatch-funnel', type: 'funnel',
      titleKey: 'dashboardBuilder.presets.dispatchStatus',
      descriptionKey: 'dashboardBuilder.presets.dispatchStatusDesc',
      dataSource: 'dispatches', metric: 'statusBreakdown',
      layout: { x: 5, y: 6, w: 3, h: 5, minW: 3, minH: 3 },
      config: { color: p2 },
    },
    {
      id: 'field-monthly', type: 'area',
      titleKey: 'dashboardBuilder.presets.monthlyTrend',
      descriptionKey: 'dashboardBuilder.presets.monthlyTrendDesc',
      dataSource: 'tasks', metric: 'monthlyTrend',
      layout: { x: 8, y: 6, w: 4, h: 5, minW: 3, minH: 3 },
      config: { color: p, showGrid: true },
    },

    // ── Row 11: Completion gauge + SO trend ──
    {
      id: 'field-completion-gauge', type: 'gauge',
      titleKey: 'dashboardBuilder.presets.completionRate',
      dataSource: 'tasks', metric: 'completionRate',
      layout: { x: 0, y: 11, w: 3, h: 4, minW: 2, minH: 3 },
      config: { color: p, animated: true },
    },
    {
      id: 'field-so-trend', type: 'line',
      titleKey: 'dashboardBuilder.presets.serviceOrderStatus',
      dataSource: 'serviceOrders', metric: 'monthlyTrend',
      layout: { x: 3, y: 11, w: 9, h: 4, minW: 4, minH: 3 },
      config: { color: p3, showGrid: true },
    },
  ];
}

// ════════════════════════════════════════════════════════════════
// EXECUTIVE HUB — Full business overview with premium styling
// Layout: 4 gradient KPIs → Sales line + Offers pie + Gauge → Heatmap + Radar + Table → Map + Stacked bar
// ════════════════════════════════════════════════════════════════

function createExecutiveWidgets(): DashboardWidget[] {
  const p = getPrimaryHex();
  const p2 = lighten(p, 0.25);
  const p3 = darken(p, 0.15);
  const p4 = lighten(p, 0.40);

  return [
    // ── Row 0: 4 premium gradient KPI cards ──
    {
      id: 'exec-revenue', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.salesRevenue',
      dataSource: 'sales', metric: 'revenue',
      layout: { x: 0, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        prefix: '', fontSize: 'lg', fontWeight: 'bold', icon: 'DollarSign', color: p,
        kpiBg: { style: 'gradient', color1: p, color2: p3, gradientAngle: 135, opacity: 92, textLight: true, effect: 'glow' },
      },
    },
    {
      id: 'exec-contacts', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalContacts',
      dataSource: 'contacts', metric: 'count',
      layout: { x: 3, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Users', color: p3,
        kpiBg: { style: 'gradient', color1: p3, color2: p, gradientAngle: 135, opacity: 90, textLight: true, effect: 'wave' },
      },
    },
    {
      id: 'exec-tasks', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.totalTasks',
      dataSource: 'tasks', metric: 'count',
      layout: { x: 6, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Briefcase', color: p,
        kpiBg: { style: 'gradient', color1: p, color2: p2, gradientAngle: 135, opacity: 88, textLight: true, effect: 'dots' },
      },
    },
    {
      id: 'exec-conversion', type: 'kpi',
      titleKey: 'dashboardBuilder.presets.conversionRate',
      dataSource: 'sales', metric: 'conversionRate',
      layout: { x: 9, y: 0, w: 3, h: 2, minW: 2, minH: 2 },
      config: {
        icon: 'Target', color: p2, suffix: '%',
        kpiBg: { style: 'gradient', color1: p2, color2: p3, gradientAngle: 135, opacity: 90, textLight: true, effect: 'rings' },
      },
    },

    // ── Row 2: Sales trend (large) + Offers pie + Completion gauge ──
    {
      id: 'exec-sales-trend', type: 'line',
      titleKey: 'dashboardBuilder.presets.salesTrend',
      descriptionKey: 'dashboardBuilder.presets.salesTrendDesc',
      dataSource: 'sales', metric: 'monthlyTrend',
      layout: { x: 0, y: 2, w: 5, h: 4, minW: 4, minH: 3 },
      config: {
        color: p, showGrid: true, animated: true,
        kpiBg: { style: 'glass', color1: p, color2: p2, opacity: 8, effect: 'aurora' },
      },
    },
    {
      id: 'exec-offers-pie', type: 'pie',
      titleKey: 'dashboardBuilder.presets.offersPipeline',
      descriptionKey: 'dashboardBuilder.presets.offersPipelineDesc',
      dataSource: 'offers', metric: 'statusBreakdown',
      layout: { x: 5, y: 2, w: 4, h: 4, minW: 3, minH: 3 },
      config: {
        showLegend: true, animated: true,
        kpiBg: { style: 'glass', color1: p, color2: p3, opacity: 6, effect: 'hexagons' },
      },
    },
    {
      id: 'exec-completion', type: 'gauge',
      titleKey: 'dashboardBuilder.presets.completionRate',
      dataSource: 'tasks', metric: 'completionRate',
      layout: { x: 9, y: 2, w: 3, h: 4, minW: 2, minH: 3 },
      config: {
        color: p, animated: true,
        kpiBg: { style: 'glass', color1: p, color2: p2, opacity: 8, effect: 'mesh' },
      },
    },

    // ── Row 6: SO heatmap + Task radar + Top sales table ──
    {
      id: 'exec-so-heatmap', type: 'heatmap',
      titleKey: 'dashboardBuilder.presets.serviceOrderStatus',
      descriptionKey: 'dashboardBuilder.presets.serviceOrderStatusDesc',
      dataSource: 'serviceOrders', metric: 'statusBreakdown',
      layout: { x: 0, y: 6, w: 4, h: 4, minW: 3, minH: 3 },
      config: {
        color: p,
        kpiBg: { style: 'glass', color1: p3, color2: p, opacity: 6, effect: 'diagonal' },
      },
    },
    {
      id: 'exec-task-radar', type: 'radar',
      titleKey: 'dashboardBuilder.presets.tasksByStatus',
      descriptionKey: 'dashboardBuilder.presets.tasksByStatusDesc',
      dataSource: 'tasks', metric: 'statusBreakdown',
      layout: { x: 4, y: 6, w: 4, h: 4, minW: 3, minH: 3 },
      config: {
        color: p2, animated: true,
        kpiBg: { style: 'glass', color1: p, color2: p2, opacity: 6, effect: 'crosshatch' },
      },
    },
    {
      id: 'exec-top-sales', type: 'table',
      titleKey: 'dashboardBuilder.presets.topSales',
      dataSource: 'sales', metric: 'topItems',
      layout: { x: 8, y: 6, w: 4, h: 4, minW: 3, minH: 3 },
    },

    // ── Row 10: Dispatch map + Stacked bar + Revenue sparkline ──
    {
      id: 'exec-dispatch-map', type: 'map',
      titleKey: 'dashboardBuilder.presets.dispatchLocations',
      dataSource: 'dispatches', metric: 'count',
      layout: { x: 0, y: 10, w: 5, h: 5, minW: 3, minH: 3 },
      config: { color: p },
    },
    {
      id: 'exec-sales-stacked', type: 'stackedBar',
      titleKey: 'dashboardBuilder.presets.salesStatus',
      descriptionKey: 'dashboardBuilder.presets.salesStatusDesc',
      dataSource: 'sales', metric: 'statusBreakdown',
      layout: { x: 5, y: 10, w: 4, h: 5, minW: 3, minH: 3 },
      config: {
        showLegend: true, showGrid: true, animated: true, borderRadius: 6,
        kpiBg: { style: 'glass', color1: p, color2: p2, opacity: 6, effect: 'noise' },
      },
    },
    {
      id: 'exec-revenue-spark', type: 'sparkline',
      titleKey: 'dashboardBuilder.presets.salesRevenue',
      dataSource: 'sales', metric: 'monthlyTrend',
      layout: { x: 9, y: 10, w: 3, h: 2, minW: 2, minH: 2 },
      config: { color: p, sparklineType: 'area' },
    },
    {
      id: 'exec-offers-donut', type: 'donut',
      titleKey: 'dashboardBuilder.presets.offersPipeline',
      dataSource: 'offers', metric: 'statusBreakdown',
      layout: { x: 9, y: 12, w: 3, h: 3, minW: 3, minH: 3 },
      config: {
        showLegend: false, innerRadius: 55, animated: true,
        kpiBg: { style: 'glass', color1: p3, color2: p, opacity: 8, effect: 'glow' },
      },
    },
  ];
}

// Backward-compat static exports
export const CRM_TEMPLATE_WIDGETS = createCrmWidgets();
export const FIELD_TEMPLATE_WIDGETS = createFieldWidgets();
export const EXECUTIVE_TEMPLATE_WIDGETS = createExecutiveWidgets();

// Factory map — call the function each time so it picks up the latest primary color
export const TEMPLATE_MAP: Record<string, { nameKey: string; widgets: () => DashboardWidget[] }> = {
  crm: { nameKey: 'dashboardBuilder.templates.crm', widgets: createCrmWidgets },
  field: { nameKey: 'dashboardBuilder.templates.field', widgets: createFieldWidgets },
  executive: { nameKey: 'dashboardBuilder.templates.executive', widgets: createExecutiveWidgets },
};
