/**
 * OpenRouter Models & Keys Service
 * Fetches models from OpenRouter public API.
 * Keys are persisted via backend userAiSettingsApi AND cached locally for API calls
 * (because backend masks keys on GET, making them unusable for Authorization headers).
 */

import { userAiSettingsApi, type UserAiKeyDto, type CreateUserAiKeyDto } from './api/userAiSettingsApi';

const OPENROUTER_MODELS_API = 'https://openrouter.ai/api/v1/models';

export interface OpenRouterModel {
  id: string;
  name: string;
  description: string;
  contextLength: number;
  pricing: { prompt: string; completion: string };
  isFree: boolean;
  provider: string;
  modality: string;
}

// Re-export backend key type for components
export type OpenRouterApiKey = UserAiKeyDto;

// ─── Local key cache (full unmasked keys for API calls) ───
const LOCAL_KEYS_STORAGE = 'openrouter_keys_cache';

interface LocalKeyEntry {
  id: number;
  apiKey: string;
  label: string;
  priority: number;
}

function getLocalKeys(): LocalKeyEntry[] {
  try {
    const raw = localStorage.getItem(LOCAL_KEYS_STORAGE);
    return raw ? JSON.parse(raw) : [];
  } catch { return []; }
}

function saveLocalKeys(keys: LocalKeyEntry[]) {
  localStorage.setItem(LOCAL_KEYS_STORAGE, JSON.stringify(keys));
}

function addLocalKey(id: number, apiKey: string, label: string, priority: number) {
  const keys = getLocalKeys().filter(k => k.id !== id);
  keys.push({ id, apiKey, label, priority });
  saveLocalKeys(keys);
}

function removeLocalKey(id: number) {
  saveLocalKeys(getLocalKeys().filter(k => k.id !== id));
}

/**
 * Fallback API keys — add as many as you want here.
 * They are tried in order if the previous one fails (rate limit, spend limit, etc.).
 * User-added keys from Settings → Integrations take priority over these.
 */
const FALLBACK_API_KEYS: string[] = [
  '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',
  // Add more fallback keys below:
  // 'sk-or-v1-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx',
  // 'sk-or-v1-yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy',
];

/**
 * Returns all usable API keys for making OpenRouter API calls.
 * Priority: user-added keys first, then hardcoded fallback keys.
 * The AI services try each key in order — if one fails, the next is used.
 */
export function getUsableApiKeys(): string[] {
  const localKeys = getLocalKeys()
    .sort((a, b) => a.priority - b.priority)
    .map(k => k.apiKey)
    .filter(k => k.startsWith('sk-'));
  
  // Combine user keys + fallback keys, deduplicated
  const allKeys = [...localKeys, ...FALLBACK_API_KEYS];
  return [...new Set(allKeys)];
}

// ─── API Key Management (backend-persisted + locally cached) ───

export async function getOpenRouterKeys(): Promise<UserAiKeyDto[]> {
  try {
    return await userAiSettingsApi.getKeys();
  } catch {
    return [];
  }
}

export async function addOpenRouterKey(key: string, label: string): Promise<UserAiKeyDto | null> {
  const dto: CreateUserAiKeyDto = { apiKey: key, label: label || 'Key' };
  const result = await userAiSettingsApi.addKey(dto);
  if (result) {
    // Cache the full key locally for API calls
    addLocalKey(result.id, key, result.label, result.priority);
  }
  return result;
}

export async function removeOpenRouterKey(id: number): Promise<boolean> {
  const ok = await userAiSettingsApi.deleteKey(id);
  if (ok) removeLocalKey(id);
  return ok;
}

// ─── Model Fetching (public API, no auth needed) ───

let cachedModels: OpenRouterModel[] | null = null;
let cacheTimestamp = 0;
const CACHE_DURATION = 10 * 60 * 1000;

export async function fetchOpenRouterModels(): Promise<OpenRouterModel[]> {
  if (cachedModels && Date.now() - cacheTimestamp < CACHE_DURATION) {
    return cachedModels;
  }
  try {
    const response = await fetch(OPENROUTER_MODELS_API);
    if (!response.ok) throw new Error(`OpenRouter API error: ${response.status}`);
    const json = await response.json();
    const models: OpenRouterModel[] = (json.data || []).map((m: any) => ({
      id: m.id,
      name: m.name || m.id,
      description: m.description || '',
      contextLength: m.context_length || 0,
      pricing: { prompt: m.pricing?.prompt || '0', completion: m.pricing?.completion || '0' },
      isFree: parseFloat(m.pricing?.prompt || '0') === 0 && parseFloat(m.pricing?.completion || '0') === 0,
      provider: m.id.split('/')[0] || 'unknown',
      modality: m.architecture?.modality || 'text→text',
    }));
    cachedModels = models;
    cacheTimestamp = Date.now();
    return models;
  } catch (error) {
    console.error('Failed to fetch OpenRouter models:', error);
    return cachedModels || [];
  }
}
