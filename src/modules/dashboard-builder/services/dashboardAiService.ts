import modelsConfig from '@/config/models.json';
import type { DashboardWidget, WidgetType, DataSourceKey, MetricKey } from '../types';

interface ModelConfig {
  id: string;
  name: string;
  model: string;
  apiKey: string;
  apiUrl: string;
  enabled: boolean;
  priority: number;
  role?: string;
}

const getModel = (): ModelConfig => {
  const responders = modelsConfig.models.filter(m => m.enabled && m.role !== 'analyzer');
  responders.sort((a, b) => b.priority - a.priority);
  return responders[0] || modelsConfig.models[0];
};

const getFallback = (): ModelConfig | null =>
  modelsConfig.models.find(m => m.role === 'fallback' && m.enabled) || null;

/** Get primary CSS color as hex */
function getPrimaryHex(): string {
  try {
    const raw = getComputedStyle(document.documentElement).getPropertyValue('--primary').trim();
    if (!raw) return '#e63b2e';
    const [h, s, l] = raw.split(/\s+/).map(v => parseFloat(v));
    const sn = s / 100, ln = l / 100;
    const a = sn * Math.min(ln, 1 - ln);
    const f = (n: number) => {
      const k = (n + h / 30) % 12;
      const c = ln - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
      return Math.round(255 * c).toString(16).padStart(2, '0');
    };
    return `#${f(0)}${f(8)}${f(4)}`;
  } catch {
    return '#e63b2e';
  }
}

const SYSTEM_PROMPT = `You are an expert dashboard builder AI for FlowService, a CRM/business management platform.

Your job is to generate a JSON array of dashboard widgets based on the user's request. 

Available widget types: kpi, bar, pie, donut, line, area, table, gauge, sparkline, funnel, radar, stackedBar, heatmap, map

Available data sources: sales, offers, contacts, tasks, articles, serviceOrders, dispatches, timeExpenses

Available metrics per type:
- kpi/sparkline: count, total, revenue, average, conversionRate, completionRate
- bar/stackedBar: statusBreakdown, priorityBreakdown, monthlyTrend, topItems
- pie/donut/funnel: statusBreakdown, priorityBreakdown
- line/area: monthlyTrend, revenue
- table: topItems
- gauge: conversionRate, completionRate
- radar/heatmap: statusBreakdown, priorityBreakdown, monthlyTrend
- map: count, statusBreakdown (only for contacts, dispatches, serviceOrders, tasks, sales)

Grid is 12 columns wide. KPIs are typically w:3 h:2. Charts are w:4-7 h:4. Tables w:4-6 h:4-5.

Available KPI icons: TrendingUp, Users, FileText, CheckCircle, Package, ClipboardList, Briefcase, Calendar, MapPin, Clock, Activity, DollarSign, ShoppingCart, BarChart3, Zap, Target, Star, Heart, Shield, Globe, Layers, Send, Eye, Award

Available kpiBg styles: subtle, solid, gradient, glass
Available kpiBg effects: none, wave, dots, rings, diagonal, glow, mesh, hexagons, noise, crosshatch, aurora

Rules:
1. Always generate a complete, balanced dashboard layout
2. Use proper grid positions (x, y, w, h) that don't overlap. 12 cols total.
3. Start with KPI cards in row 0, then charts below
4. Use the PRIMARY_COLOR for color fields
5. Make it visually appealing with varied widget types
6. Every widget needs a unique id (use descriptive kebab-case like "ai-sales-revenue")
7. titleKey should use "dashboardBuilder.presets." prefix for known keys, or use titleCustom for custom titles
8. Return ONLY a valid JSON array of widgets, no markdown, no explanation

Known titleKey values:
salesRevenue, totalSales, totalOffers, totalContacts, salesStatus, offersPipeline, conversionRate, salesTrend, topSales, activeServiceOrders, totalTasks, totalDispatches, completionRate, tasksByStatus, serviceOrderStatus, monthlyTrend, dispatchStatus, dispatchLocations, salesStatusDesc, offersPipelineDesc, salesTrendDesc, tasksByStatusDesc, serviceOrderStatusDesc, monthlyTrendDesc, dispatchStatusDesc

PRIMARY_COLOR: {{PRIMARY_COLOR}}
`;

export interface AiDashboardMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

export interface StreamCallbacks {
  onToken: (token: string) => void;
  onComplete: (widgets: DashboardWidget[] | null) => void;
  onError: (error: string) => void;
}

/** Parse AI response JSON into validated widgets */
function parseWidgets(raw: string): DashboardWidget[] | null {
  try {
    // Extract JSON array from response (handle markdown wrapping)
    let jsonStr = raw.trim();
    const arrayMatch = jsonStr.match(/\[[\s\S]*\]/);
    if (!arrayMatch) return null;
    jsonStr = arrayMatch[0];

    const parsed = JSON.parse(jsonStr);
    if (!Array.isArray(parsed) || parsed.length === 0) return null;

    const validTypes: WidgetType[] = ['kpi', 'bar', 'pie', 'donut', 'line', 'area', 'table', 'gauge', 'sparkline', 'funnel', 'radar', 'stackedBar', 'heatmap', 'map'];
    const validSources: DataSourceKey[] = ['sales', 'offers', 'contacts', 'tasks', 'articles', 'serviceOrders', 'dispatches', 'timeExpenses', 'externalApi'];

    return parsed
      .filter((w: any) => w && validTypes.includes(w.type) && validSources.includes(w.dataSource))
      .map((w: any, i: number) => ({
        id: w.id || `ai-widget-${Date.now()}-${i}`,
        type: w.type as WidgetType,
        titleKey: w.titleKey || 'dashboardBuilder.presets.totalSales',
        titleCustom: w.titleCustom,
        descriptionKey: w.descriptionKey,
        descriptionCustom: w.descriptionCustom,
        dataSource: w.dataSource as DataSourceKey,
        metric: (w.metric || 'count') as MetricKey,
        layout: {
          x: w.layout?.x ?? 0,
          y: w.layout?.y ?? i * 2,
          w: w.layout?.w ?? 3,
          h: w.layout?.h ?? 2,
          minW: w.layout?.minW ?? 2,
          minH: w.layout?.minH ?? 2,
        },
        config: w.config || {},
        filters: w.filters,
      }));
  } catch (e) {
    console.error('Failed to parse AI dashboard response:', e);
    return null;
  }
}

export async function streamDashboardAI(
  userMessage: string,
  callbacks: StreamCallbacks,
): Promise<void> {
  const primary = getPrimaryHex();
  const systemPrompt = SYSTEM_PROMPT.replace(/\{\{PRIMARY_COLOR\}\}/g, primary);
  const model = getModel();

  const tryStream = async (m: ModelConfig) => {
    const response = await fetch(m.apiUrl, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${m.apiKey}`,
        'Content-Type': 'application/json',
        'HTTP-Referer': window.location.origin,
        'X-Title': 'FlowService Dashboard AI',
      },
      body: JSON.stringify({
        model: m.model,
        messages: [
          { role: 'system', content: systemPrompt },
          { role: 'user', content: userMessage },
        ],
        stream: true,
        max_tokens: 4000,
      }),
    });

    if (!response.ok) throw new Error(`API error: ${response.status}`);

    const reader = response.body?.getReader();
    if (!reader) throw new Error('No body');

    const decoder = new TextDecoder();
    let buffer = '';
    let fullContent = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      buffer += decoder.decode(value, { stream: true });

      const lines = buffer.split('\n');
      buffer = lines.pop() || '';

      for (const line of lines) {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith(':') || !trimmed.startsWith('data: ')) continue;
        const jsonStr = trimmed.slice(6);
        if (jsonStr === '[DONE]') {
          const widgets = parseWidgets(fullContent);
          callbacks.onComplete(widgets);
          return;
        }
        try {
          const parsed = JSON.parse(jsonStr);
          const content = parsed.choices?.[0]?.delta?.content;
          if (content) {
            fullContent += content;
            callbacks.onToken(content);
          }
        } catch { /* partial JSON */ }
      }
    }

    // Final flush
    if (buffer.trim()) {
      for (const line of buffer.split('\n')) {
        const trimmed = line.trim();
        if (!trimmed.startsWith('data: ')) continue;
        const jsonStr = trimmed.slice(6);
        if (jsonStr === '[DONE]') continue;
        try {
          const parsed = JSON.parse(jsonStr);
          const content = parsed.choices?.[0]?.delta?.content;
          if (content) fullContent += content;
        } catch {}
      }
    }

    const widgets = parseWidgets(fullContent);
    callbacks.onComplete(widgets);
  };

  try {
    await tryStream(model);
  } catch (err) {
    console.error('Primary model failed:', err);
    const fallback = getFallback();
    if (fallback) {
      try {
        await tryStream(fallback);
        return;
      } catch (fbErr) {
        console.error('Fallback failed:', fbErr);
      }
    }
    callbacks.onError(err instanceof Error ? err.message : 'AI generation failed');
  }
}

/** Quick presets users can click */
export const AI_QUICK_PROMPTS = [
  { labelKey: 'dashboardBuilder.ai.promptSales', prompt: 'Build me a sales-focused dashboard with revenue KPIs, sales trends, and pipeline analysis' },
  { labelKey: 'dashboardBuilder.ai.promptCRM', prompt: 'Create a CRM overview dashboard showing contacts, offers, sales funnel, and conversion metrics' },
  { labelKey: 'dashboardBuilder.ai.promptField', prompt: 'Build a field operations dashboard with service orders, tasks, dispatches map, and completion rates' },
  { labelKey: 'dashboardBuilder.ai.promptExecutive', prompt: 'Create an executive summary with high-level KPIs, trends, and performance gauges across all modules' },
  { labelKey: 'dashboardBuilder.ai.promptMinimal', prompt: 'Build a clean minimal dashboard with just 4 KPI cards and 2 charts' },
];
