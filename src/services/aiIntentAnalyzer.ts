// AI Intent Analyzer - Uses lightweight model to analyze user intent and language
import { getUsableApiKeys } from '@/services/openRouterModelsService';

export interface IntentAnalysis {
  language: 'en' | 'fr' | 'ar' | 'unknown';
  intent: 'question' | 'command' | 'planning' | 'lookup' | 'general';
  subject: 'dispatch' | 'service_order' | 'technician' | 'job' | 'contact' | 'offer' | 'sale' | 'schedule' | 'general';
  entities: string[];
  confidence: number;
}

const getAnalyzerModel = () => {
  const keys = getUsableApiKeys();
  const apiKey = keys[0] || '';
  return {
    model: 'meta-llama/llama-3.1-8b-instruct',
    apiKey,
    apiUrl: 'https://openrouter.ai/api/v1/chat/completions',
  };
};

export async function analyzeUserIntent(userMessage: string): Promise<IntentAnalysis> {
  const model = getAnalyzerModel();
  
  const systemPrompt = `You are an intent analyzer for a field service management system. Analyze the user message and respond ONLY with valid JSON (no markdown, no explanation).

Detect:
1. Language: "en" (English), "fr" (French), "ar" (Arabic), or "unknown"
2. Intent: "question", "command", "planning", "lookup", or "general"
3. Subject: "dispatch", "service_order", "technician", "job", "contact", "offer", "sale", "schedule", or "general"
4. Entities: Extract mentioned IDs, names, dates (array of strings)
5. Confidence: 0.0 to 1.0

Response format (JSON only):
{"language":"en","intent":"lookup","subject":"dispatch","entities":["DISP-001"],"confidence":0.9}`;

  try {
    const apiKey = model.apiKey;
    if (!apiKey) {
      console.warn('No API key for intent analyzer, using fallback');
      return fallbackIntentDetection(userMessage);
    }

    const response = await fetch(model.apiUrl, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${apiKey}`,
        'Content-Type': 'application/json',
        'HTTP-Referer': window.location.origin,
        'X-Title': 'Flow Service Intent Analyzer',
      },
      body: JSON.stringify({
        model: model.model,
        messages: [
          { role: 'system', content: systemPrompt },
          { role: 'user', content: userMessage }
        ],
        max_tokens: 200,
        temperature: 0.1, // Low temperature for consistent analysis
      }),
    });

    if (!response.ok) {
      console.warn('Intent analyzer failed, using fallback detection');
      return fallbackIntentDetection(userMessage);
    }

    const data = await response.json();
    const content = data.choices?.[0]?.message?.content?.trim();
    
    if (!content) {
      return fallbackIntentDetection(userMessage);
    }

    // Try to parse JSON from response
    try {
      // Remove any markdown code blocks if present
      const jsonStr = content.replace(/```json?\n?/g, '').replace(/```/g, '').trim();
      const parsed = JSON.parse(jsonStr);
      
      return {
        language: parsed.language || detectLanguageFallback(userMessage),
        intent: parsed.intent || 'general',
        subject: parsed.subject || 'general',
        entities: Array.isArray(parsed.entities) ? parsed.entities : [],
        confidence: typeof parsed.confidence === 'number' ? parsed.confidence : 0.5,
      };
    } catch {
      console.warn('Failed to parse intent analysis, using fallback');
      return fallbackIntentDetection(userMessage);
    }
  } catch (error) {
    console.error('Intent analysis error:', error);
    return fallbackIntentDetection(userMessage);
  }
}

// Fallback detection when AI is unavailable
function fallbackIntentDetection(message: string): IntentAnalysis {
  const lower = message.toLowerCase();
  
  return {
    language: detectLanguageFallback(message),
    intent: detectIntentFallback(lower),
    subject: detectSubjectFallback(lower),
    entities: extractEntitiesFallback(message),
    confidence: 0.6,
  };
}

function detectLanguageFallback(text: string): 'en' | 'fr' | 'ar' | 'unknown' {
  // Arabic characters
  if (/[\u0600-\u06FF]/.test(text)) return 'ar';
  
  // French indicators
  const frenchWords = ['bonjour', 'merci', 'comment', 'où', 'quoi', 'qui', 'pourquoi', 'travaille', 'technicien', 'dispatch', 'commande', 'planification', 'afficher', 'montrer'];
  const frenchCount = frenchWords.filter(w => text.toLowerCase().includes(w)).length;
  
  // English indicators
  const englishWords = ['hello', 'thanks', 'what', 'who', 'where', 'why', 'show', 'display', 'schedule', 'working', 'assigned'];
  const englishCount = englishWords.filter(w => text.toLowerCase().includes(w)).length;
  
  if (frenchCount > englishCount && frenchCount >= 1) return 'fr';
  if (englishCount > 0 || /[a-zA-Z]/.test(text)) return 'en';
  
  return 'unknown';
}

function detectIntentFallback(lower: string): IntentAnalysis['intent'] {
  if (/\?|what|who|where|when|how|quoi|qui|où|comment/.test(lower)) return 'question';
  if (/plan|assign|schedule|planifier|assigner/.test(lower)) return 'planning';
  if (/show|get|find|display|status|afficher|montrer|statut/.test(lower)) return 'lookup';
  if (/create|add|update|delete|créer|ajouter|modifier|supprimer/.test(lower)) return 'command';
  return 'general';
}

function detectSubjectFallback(lower: string): IntentAnalysis['subject'] {
  if (/dispatch|disp-?\d+/i.test(lower)) return 'dispatch';
  if (/service.?order|so-?\d+/i.test(lower)) return 'service_order';
  if (/technician|tech|@\w+/i.test(lower)) return 'technician';
  if (/job|task|travail|tâche/i.test(lower)) return 'job';
  if (/contact|client|customer/i.test(lower)) return 'contact';
  if (/offer|offre|off-?\d+/i.test(lower)) return 'offer';
  if (/sale|vente|sal-?\d+/i.test(lower)) return 'sale';
  if (/schedule|planning|calendar|agenda/i.test(lower)) return 'schedule';
  return 'general';
}

function extractEntitiesFallback(text: string): string[] {
  const entities: string[] = [];
  
  // Extract dispatch numbers
  const dispatchMatch = text.match(/DISP-?\d+/gi);
  if (dispatchMatch) entities.push(...dispatchMatch);
  
  // Extract service order numbers
  const soMatch = text.match(/SO-?\d+/gi);
  if (soMatch) entities.push(...soMatch);
  
  // Extract @mentions
  const mentionMatch = text.match(/@\w+/g);
  if (mentionMatch) entities.push(...mentionMatch);
  
  // Extract dates
  const dateMatch = text.match(/\d{1,2}[\/\-]\d{1,2}[\/\-]\d{2,4}|\btomorrow\b|\btoday\b|\bdemain\b|\baujourd'hui\b/gi);
  if (dateMatch) entities.push(...dateMatch);
  
  return entities;
}
