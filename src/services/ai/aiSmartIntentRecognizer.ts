// AI Smart Intent Recognizer - Uses lightweight LLM for fuzzy pattern matching
// This acts as a "second brain" to catch requests that are semantically close but don't match exact patterns

import { getUsableApiKeys } from '@/services/openRouterModelsService';

const RECOGNIZER_CONFIG = {
  apiUrl: 'https://openrouter.ai/api/v1/chat/completions',
  // Paid models first (work with user's own key), then free fallbacks
  paidModels: [
    'meta-llama/llama-3.3-70b-instruct',
    'mistralai/mistral-small-3.1-24b-instruct',
    'meta-llama/llama-3.1-8b-instruct',
  ],
  freeModels: [
    'meta-llama/llama-3.3-70b-instruct:free',
    'mistralai/mistral-small-3.1-24b-instruct:free',
  ],
};

const getRecognizerApiKey = (): string | null => {
  const keys = getUsableApiKeys();
  return keys[0] || null;
};

// =====================================================
// Types
// =====================================================

export type ActionType = 
  | 'form_creation'
  | 'task_creation'
  | 'dispatch_assignment'
  | 'stock_modification'
  | 'contact_lookup'
  | 'offer_creation'
  | 'schedule_query'
  | 'analytics_query'
  | 'general_query'
  | 'none';

export interface SmartIntentResult {
  action: ActionType;
  confidence: number;
  extractedEntities: {
    topic?: string;
    targetName?: string;
    quantity?: number;
    date?: string;
    time?: string;
    dispatchId?: string;
    technicianName?: string;
  };
  suggestedFields?: string[];
  reasoning?: string;
}

export interface FormIntentResult {
  isFormCreation: boolean;
  confidence: number;
  extractedTopic?: string;
  suggestedFields?: string[];
  reasoning?: string;
}

// =====================================================
// Main Smart Recognizer
// =====================================================

/**
 * Smart intent recognizer - uses LLM to understand fuzzy/conversational requests
 * Classifies into multiple action types
 */
export async function recognizeIntent(message: string): Promise<SmartIntentResult> {
  const systemPrompt = `You are an intent classifier for FlowService, a field service management app. Classify user messages into action types.

RESPOND WITH JSON ONLY. No explanation, no markdown.

ACTION TYPES:
1. "form_creation" - User wants to CREATE a new form, checklist, survey, or questionnaire
2. "task_creation" - User wants to CREATE personal tasks, todos, or reminders
3. "dispatch_assignment" - User wants to ASSIGN a dispatch/job to a technician, or ask who to assign
4. "stock_modification" - User wants to ADD or REMOVE stock from inventory
5. "contact_lookup" - User wants to FIND or VIEW contact/customer information
6. "offer_creation" - User wants to CREATE a new quote/offer for a customer
7. "schedule_query" - User asks about schedules, availability, who is working when
8. "analytics_query" - User asks for reports, statistics, performance metrics
9. "general_query" - User asks general questions about the system or data
10. "none" - Not a recognizable action (greeting, off-topic, etc.)

EXAMPLES:
- "create a satisfaction form" → form_creation
- "can you please make a form, I need it for inspections" → form_creation
- "I need to do X, Y, Z today" → task_creation
- "remind me to call John" → task_creation
- "assign dispatch DISP-001 to Ahmed" → dispatch_assignment
- "who should handle this job?" → dispatch_assignment
- "add 50 units to laptop stock" → stock_modification
- "find customer John Smith" → contact_lookup
- "create a quote for ABC Company" → offer_creation
- "@Sarah schedule" → schedule_query
- "who is working tomorrow?" → schedule_query
- "how many sales this month?" → analytics_query
- "top performing technicians" → analytics_query
- "what is a dispatch?" → general_query
- "hello" → none

Response format (JSON only):
{
  "action": "form_creation",
  "confidence": 0.95,
  "extractedEntities": {
    "topic": "customer satisfaction",
    "targetName": null,
    "dispatchId": null,
    "technicianName": null
  },
  "suggestedFields": ["rating", "text", "signature"],
  "reasoning": "user wants satisfaction form"
}`;

  try {
    console.log('[SmartRecognizer] Analyzing:', message.substring(0, 80));
    const apiKey = await getRecognizerApiKey();
    if (!apiKey) {
      console.warn('[SmartRecognizer] No API key available');
      return null;
    }

    // Try paid models first, then free fallbacks
    const allModels = [...RECOGNIZER_CONFIG.paidModels, ...RECOGNIZER_CONFIG.freeModels];
    for (const model of allModels) {
      const response = await fetch(RECOGNIZER_CONFIG.apiUrl, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${apiKey}`,
          'Content-Type': 'application/json',
          'HTTP-Referer': window.location.origin,
          'X-Title': 'FlowService Smart Recognizer',
        },
        body: JSON.stringify({
          model,
          messages: [
            { role: 'system', content: systemPrompt },
            { role: 'user', content: message },
          ],
          max_tokens: 200,
          temperature: 0.1,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        const errMsg = errorData?.error?.message || `HTTP ${response.status}`;
        console.warn(`[SmartRecognizer] Model failed (${model}): ${errMsg}`);
        continue;
      }

      const data = await response.json();
      const content = data.choices?.[0]?.message?.content?.trim();

      if (!content) {
        console.warn(`[SmartRecognizer] Empty response content (${model})`);
        continue;
      }

      // Parse JSON response
      try {
        const jsonStr = content.replace(/```json?\n?/g, '').replace(/```/g, '').trim();
        const parsed = JSON.parse(jsonStr);

        const result: SmartIntentResult = {
          action: isValidActionType(parsed.action) ? parsed.action : 'none',
          confidence: typeof parsed.confidence === 'number' ? parsed.confidence : 0.5,
          extractedEntities: {
            topic: parsed.extractedEntities?.topic || undefined,
            targetName: parsed.extractedEntities?.targetName || undefined,
            quantity: parsed.extractedEntities?.quantity || undefined,
            date: parsed.extractedEntities?.date || undefined,
            time: parsed.extractedEntities?.time || undefined,
            dispatchId: parsed.extractedEntities?.dispatchId || undefined,
            technicianName: parsed.extractedEntities?.technicianName || undefined,
          },
          suggestedFields: Array.isArray(parsed.suggestedFields) ? parsed.suggestedFields : [],
          reasoning: parsed.reasoning,
        };

        console.log('[SmartRecognizer] Result:', result.action, 'confidence:', result.confidence);
        return result;
      } catch {
        console.warn(`[SmartRecognizer] Failed to parse JSON (${model})`);
        continue;
      }
    }

    console.warn('[SmartRecognizer] All models failed, returning none');
    return { action: 'none', confidence: 0, extractedEntities: {} };
  } catch (error) {
    console.error('[SmartRecognizer] Error:', error);
    return { action: 'none', confidence: 0, extractedEntities: {} };
  }
}

function isValidActionType(action: string): action is ActionType {
  const validTypes: ActionType[] = [
    'form_creation', 'task_creation', 'dispatch_assignment', 
    'stock_modification', 'contact_lookup', 'offer_creation',
    'schedule_query', 'analytics_query', 'general_query', 'none'
  ];
  return validTypes.includes(action as ActionType);
}

// =====================================================
// Form-Specific Recognition (backward compatible)
// =====================================================

/**
 * Smart intent recognizer for form creation specifically
 * Only called when regex patterns have low confidence (<0.6)
 */
export async function recognizeFormIntent(message: string): Promise<FormIntentResult> {
  const result = await recognizeIntent(message);
  
  return {
    isFormCreation: result.action === 'form_creation',
    confidence: result.confidence,
    extractedTopic: result.extractedEntities.topic,
    suggestedFields: result.suggestedFields,
    reasoning: result.reasoning,
  };
}

/**
 * Enhanced intent detection that combines regex + LLM for robust recognition
 */
export async function detectFormIntentSmart(
  message: string,
  regexConfidence: number
): Promise<{
  hasIntent: boolean;
  confidence: number;
  source: 'regex' | 'llm' | 'combined';
  topic?: string;
  suggestedFields?: string[];
}> {
  // If regex is confident enough, trust it
  if (regexConfidence >= 0.8) {
    console.log('[SmartRecognizer] Regex confident enough, skipping LLM');
    return {
      hasIntent: true,
      confidence: regexConfidence,
      source: 'regex',
    };
  }
  
  // If regex found something but low confidence, use LLM to verify
  if (regexConfidence >= 0.4) {
    console.log('[SmartRecognizer] Medium regex confidence, using LLM to verify');
    const llmResult = await recognizeFormIntent(message);
    
    if (llmResult.isFormCreation && llmResult.confidence >= 0.7) {
      return {
        hasIntent: true,
        confidence: Math.max(regexConfidence, llmResult.confidence),
        source: 'combined',
        topic: llmResult.extractedTopic,
        suggestedFields: llmResult.suggestedFields,
      };
    }
    
    // LLM disagrees, trust LLM if it's very confident
    if (!llmResult.isFormCreation && llmResult.confidence >= 0.9) {
      return {
        hasIntent: false,
        confidence: llmResult.confidence,
        source: 'llm',
      };
    }
    
    // Uncertain - combine scores
    return {
      hasIntent: regexConfidence >= 0.5 || llmResult.isFormCreation,
      confidence: (regexConfidence + llmResult.confidence) / 2,
      source: 'combined',
      topic: llmResult.extractedTopic,
    };
  }
  
  // Regex found nothing - rely entirely on LLM
  console.log('[SmartRecognizer] Low regex confidence, relying on LLM');
  const llmResult = await recognizeFormIntent(message);
  
  return {
    hasIntent: llmResult.isFormCreation && llmResult.confidence >= 0.7,
    confidence: llmResult.confidence,
    source: 'llm',
    topic: llmResult.extractedTopic,
    suggestedFields: llmResult.suggestedFields,
  };
}

// =====================================================
// Task-Specific Recognition
// =====================================================

/**
 * Smart task creation intent detection with LLM fallback
 */
export async function detectTaskIntentSmart(
  message: string,
  regexMatched: boolean
): Promise<{
  hasIntent: boolean;
  confidence: number;
  source: 'regex' | 'llm' | 'combined';
}> {
  // If regex matched, trust it
  if (regexMatched) {
    return {
      hasIntent: true,
      confidence: 0.9,
      source: 'regex',
    };
  }
  
  // Use LLM for fuzzy matching
  const result = await recognizeIntent(message);
  
  return {
    hasIntent: result.action === 'task_creation' && result.confidence >= 0.7,
    confidence: result.confidence,
    source: 'llm',
  };
}

// =====================================================
// Dispatch Assignment Recognition
// =====================================================

/**
 * Smart dispatch assignment intent detection
 */
export async function detectDispatchAssignmentIntent(
  message: string,
  regexConfidence: number = 0
): Promise<{
  hasIntent: boolean;
  confidence: number;
  source: 'regex' | 'llm' | 'combined';
  dispatchId?: string;
  technicianName?: string;
  time?: string;
}> {
  // If regex is confident enough, trust it
  if (regexConfidence >= 0.8) {
    return {
      hasIntent: true,
      confidence: regexConfidence,
      source: 'regex',
    };
  }
  
  // Use LLM for fuzzy matching
  const result = await recognizeIntent(message);
  
  if (result.action === 'dispatch_assignment' && result.confidence >= 0.7) {
    return {
      hasIntent: true,
      confidence: result.confidence,
      source: regexConfidence > 0 ? 'combined' : 'llm',
      dispatchId: result.extractedEntities.dispatchId,
      technicianName: result.extractedEntities.technicianName,
      time: result.extractedEntities.time,
    };
  }
  
  return {
    hasIntent: false,
    confidence: result.confidence,
    source: 'llm',
  };
}

// =====================================================
// Stock Modification Recognition
// =====================================================

/**
 * Smart stock modification intent detection
 */
export async function detectStockModificationIntent(
  message: string
): Promise<{
  hasIntent: boolean;
  confidence: number;
  action?: 'add' | 'remove';
  quantity?: number;
  itemName?: string;
}> {
  const result = await recognizeIntent(message);
  
  if (result.action === 'stock_modification' && result.confidence >= 0.7) {
    // Determine add vs remove from message
    const lowerMessage = message.toLowerCase();
    const isRemove = /remove|subtract|take|reduce|retirer|enlever/i.test(lowerMessage);
    const isAdd = /add|increase|restock|ajouter|augmenter/i.test(lowerMessage);
    
    return {
      hasIntent: true,
      confidence: result.confidence,
      action: isRemove ? 'remove' : isAdd ? 'add' : undefined,
      quantity: result.extractedEntities.quantity,
      itemName: result.extractedEntities.targetName || result.extractedEntities.topic,
    };
  }
  
  return {
    hasIntent: false,
    confidence: result.confidence,
  };
}

// =====================================================
// General Smart Detection (combines all)
// =====================================================

export interface SmartDetectionResult {
  primaryAction: ActionType;
  confidence: number;
  source: 'regex' | 'llm' | 'combined';
  entities: SmartIntentResult['extractedEntities'];
  suggestedFields?: string[];
}

/**
 * Universal smart intent detector - use this for comprehensive detection
 * Combines regex patterns with LLM fallback for all action types
 */
export async function detectIntentUniversal(
  message: string,
  regexResults?: {
    formConfidence?: number;
    taskMatched?: boolean;
    dispatchConfidence?: number;
  }
): Promise<SmartDetectionResult> {
  const regex = regexResults || {};
  
  // Check if any regex was confident enough
  if ((regex.formConfidence || 0) >= 0.8) {
    return {
      primaryAction: 'form_creation',
      confidence: regex.formConfidence!,
      source: 'regex',
      entities: {},
    };
  }
  
  if (regex.taskMatched) {
    return {
      primaryAction: 'task_creation',
      confidence: 0.9,
      source: 'regex',
      entities: {},
    };
  }
  
  if ((regex.dispatchConfidence || 0) >= 0.8) {
    return {
      primaryAction: 'dispatch_assignment',
      confidence: regex.dispatchConfidence!,
      source: 'regex',
      entities: {},
    };
  }
  
  // Fall back to LLM for comprehensive detection
  console.log('[SmartRecognizer] Using LLM for universal detection');
  const result = await recognizeIntent(message);
  
  return {
    primaryAction: result.action,
    confidence: result.confidence,
    source: 'llm',
    entities: result.extractedEntities,
    suggestedFields: result.suggestedFields,
  };
}
