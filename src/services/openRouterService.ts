// OpenRouter AI Service with intelligent routing, streaming, and fallback support
import { getUsableApiKeys } from '@/services/openRouterModelsService';
import { analyzeUserIntent, IntentAnalysis } from './aiIntentAnalyzer';

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

const OPENROUTER_URL = 'https://openrouter.ai/api/v1/chat/completions';

const getUserApiKey = (): string => getUsableApiKeys()[0] || '';

const getResponderModel = (): ModelConfig => ({
  id: 'primary', name: 'LLaMA 3.1 8B', model: 'meta-llama/llama-3.1-8b-instruct',
  apiKey: getUserApiKey(), apiUrl: OPENROUTER_URL, enabled: true, priority: 10, role: 'responder',
});

const getFallbackModel = (): ModelConfig | null => {
  const key = getUserApiKey();
  if (!key) return null;
  return { id: 'fallback', name: 'Mistral 7B', model: 'mistralai/mistral-7b-instruct',
    apiKey: key, apiUrl: OPENROUTER_URL, enabled: true, priority: 9, role: 'fallback' };
};

const getSecondaryFallbackModel = (): ModelConfig | null => null;

interface StreamCallbacks {
  onToken: (token: string) => void;
  onComplete: () => void;
  onError: (error: string) => void;
  onIntentAnalyzed?: (intent: IntentAnalysis) => void;
}

// Get language-specific system prompt
function getSystemPrompt(language: 'en' | 'fr' | 'ar' | 'unknown', intent: IntentAnalysis): string {
  const baseContext = `You are a helpful AI assistant for Flow Service, a business management platform.
You help users with managing contacts, tasks, offers, jobs, dispatches, service orders, and technician schedules.
Keep responses concise and actionable. When referring to entities, be specific.`;

  const contextInfo = intent.entities.length > 0 
    ? `\nDetected entities: ${intent.entities.join(', ')}`
    : '';

  if (language === 'fr') {
    return `${baseContext}
${contextInfo}
IMPORTANT: L'utilisateur parle français. Réponds TOUJOURS en français.
Sois professionnel mais amical. Utilise le vouvoiement.`;
  }
  
  if (language === 'ar') {
    return `${baseContext}
${contextInfo}
IMPORTANT: The user speaks Arabic. Respond in Arabic when possible, or in English if Arabic is not supported.`;
  }

  return `${baseContext}
${contextInfo}
Respond in English. Be professional but friendly.`;
}

export async function streamChatCompletion(
  messages: Array<{ role: 'user' | 'assistant' | 'system'; content: string }>,
  callbacks: StreamCallbacks
): Promise<void> {
  const lastUserMessage = [...messages].reverse().find(m => m.role === 'user')?.content || '';
  
  // Step 1: Analyze intent with lightweight model
  let intent: IntentAnalysis;
  try {
    intent = await analyzeUserIntent(lastUserMessage);
    callbacks.onIntentAnalyzed?.(intent);
    console.log('Intent analysis:', intent);
  } catch (error) {
    console.warn('Intent analysis failed, using defaults:', error);
    intent = {
      language: 'en',
      intent: 'general',
      subject: 'general',
      entities: [],
      confidence: 0.5,
    };
  }

  // Step 2: Stream response with appropriate model
  const model = getResponderModel();
  const systemPrompt = getSystemPrompt(intent.language, intent);
  
  try {
    await streamWithModel(model, messages, systemPrompt, callbacks);
  } catch (error) {
    console.error('Primary model failed:', error);
    
    // Step 3: Try fallback models
    if (true) { // always try fallbacks
      // Try first fallback
      const fallbackModel = getFallbackModel();
      if (fallbackModel) {
        console.log('Switching to fallback model:', fallbackModel.name);
        try {
          await streamWithModel(fallbackModel, messages, systemPrompt, callbacks);
          return;
        } catch (fallbackError) {
          console.error('Fallback model also failed:', fallbackError);
        }
      }
      
      // Try secondary fallback
      const fallback2Model = getSecondaryFallbackModel();
      if (fallback2Model) {
        console.log('Switching to secondary fallback model:', fallback2Model.name);
        try {
          await streamWithModel(fallback2Model, messages, systemPrompt, callbacks);
          return;
        } catch (fallback2Error) {
          console.error('Secondary fallback model also failed:', fallback2Error);
        }
      }
    }
    
    callbacks.onError(error instanceof Error ? error.message : 'All AI models failed');
  }
}

async function streamWithModel(
  model: ModelConfig,
  messages: Array<{ role: 'user' | 'assistant' | 'system'; content: string }>,
  systemPrompt: string,
  callbacks: StreamCallbacks
): Promise<void> {
  const response = await fetch(model.apiUrl, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${model.apiKey}`,
      'Content-Type': 'application/json',
      'HTTP-Referer': window.location.origin,
      'X-Title': 'Flow Service Assistant',
    },
    body: JSON.stringify({
      model: model.model,
      messages: [
        { role: 'system', content: systemPrompt },
        ...messages
      ],
      stream: true,
      max_tokens: 1000,
    }),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`API error (${model.name}): ${response.status} - ${errorText}`);
  }

  const reader = response.body?.getReader();
  if (!reader) {
    throw new Error('No response body');
  }

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    
    // Process complete SSE lines
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith(':')) continue;
      if (!trimmed.startsWith('data: ')) continue;

      const jsonStr = trimmed.slice(6);
      if (jsonStr === '[DONE]') {
        callbacks.onComplete();
        return;
      }

      try {
        const parsed = JSON.parse(jsonStr);
        const content = parsed.choices?.[0]?.delta?.content;
        if (content) {
          callbacks.onToken(content);
        }
      } catch {
        // Partial JSON, continue buffering
      }
    }
  }

  // Process remaining buffer
  if (buffer.trim()) {
    const lines = buffer.split('\n');
    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed.startsWith('data: ')) continue;
      const jsonStr = trimmed.slice(6);
      if (jsonStr === '[DONE]') continue;
      try {
        const parsed = JSON.parse(jsonStr);
        const content = parsed.choices?.[0]?.delta?.content;
        if (content) callbacks.onToken(content);
      } catch {}
    }
  }

  callbacks.onComplete();
}

// Non-streaming completion for simple queries
export async function getChatCompletion(
  messages: Array<{ role: 'user' | 'assistant' | 'system'; content: string }>
): Promise<string> {
  const lastUserMessage = [...messages].reverse().find(m => m.role === 'user')?.content || '';
  
  // Analyze intent
  const intent = await analyzeUserIntent(lastUserMessage);
  const model = getResponderModel();
  const systemPrompt = getSystemPrompt(intent.language, intent);

  const tryWithModel = async (m: ModelConfig): Promise<string> => {
    const response = await fetch(m.apiUrl, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${m.apiKey}`,
        'Content-Type': 'application/json',
        'HTTP-Referer': window.location.origin,
        'X-Title': 'Flow Service Assistant',
      },
      body: JSON.stringify({
        model: m.model,
        messages: [
          { role: 'system', content: systemPrompt },
          ...messages
        ],
        max_tokens: 1000,
      }),
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    const data = await response.json();
    return data.choices?.[0]?.message?.content || '';
  };

  try {
    return await tryWithModel(model);
  } catch (error) {
    console.error('Primary model failed, trying fallback:', error);
    
    if (true) { // always try fallbacks
      // Try first fallback
      const fallbackModel = getFallbackModel();
      if (fallbackModel) {
        try {
          return await tryWithModel(fallbackModel);
        } catch (fallbackError) {
          console.error('Fallback model failed, trying secondary:', fallbackError);
        }
      }
      
      // Try secondary fallback
      const fallback2Model = getSecondaryFallbackModel();
      if (fallback2Model) {
        return await tryWithModel(fallback2Model);
      }
    }
    
    throw error;
  }
}

// Export intent analyzer for external use
export { analyzeUserIntent, type IntentAnalysis } from './aiIntentAnalyzer';
