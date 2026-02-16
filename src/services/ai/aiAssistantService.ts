// AI Assistant Service - Handles OpenRouter API calls for the Ask AI feature
import { getSystemPrompt } from '@/config/ai-assistant-context';
import { detectAndExecuteDataQuery } from './aiDataService';
import { buildContextPrompt } from './contextAwareness';
import { fetchContextData, isDetailPage } from './contextDataService';
import { analyzeUserIssue, isIssueReportMessage } from './aiIssueDetectionService';

// Send notification to ntfy.sh for AI chat logging
const sendChatNotification = async (question: string, response: string) => {
  try {
    const truncatedQuestion = question.length > 200 ? question.slice(0, 200) + '...' : question;
    const truncatedResponse = response.length > 500 ? response.slice(0, 500) + '...' : response;
    
    await fetch('https://ntfy.sh/flowchat', {
      method: 'POST',
      headers: {
        'Title': 'AI Chat',
        'Priority': '3',
        'Tags': 'robot,speech_balloon'
      },
      body: `Q: ${truncatedQuestion}\n\nA: ${truncatedResponse}`
    });
  } catch (error) {
    console.warn('Failed to send chat notification:', error);
  }
};

// API configuration - models and keys loaded dynamically from backend
const OPENROUTER_CONFIG = {
  apiUrl: 'https://openrouter.ai/api/v1/chat/completions',
  
  // Default model tiers (used when no user keys are available)
  models: {
    primary: 'meta-llama/llama-3.3-70b-instruct:free',
    fallback1: 'google/gemma-3-27b-it:free',
    fallback2: 'mistralai/mistral-small-3.1-24b-instruct:free',
    fallback3: 'meta-llama/llama-3.1-8b-instruct:free',
  },
  
  // Vision-capable models for image analysis
  visionModels: {
    primary: 'google/gemma-3-27b-it:free',
    fallback1: 'mistralai/mistral-small-3.1-24b-instruct:free',
  },
  
  siteName: 'FlowService',
  siteUrl: window.location.origin,
  
  // Retry configuration
  maxRetries: 2,
  retryDelayMs: 1000,
};

// ─── Dynamic API key loading (local cache of unmasked keys) ───
import { getUsableApiKeys } from '@/services/openRouterModelsService';

const getApiKeys = async (): Promise<string[]> => {
  return getUsableApiKeys();
};

// Get all models in order for fallback
const getModelsList = (useVision: boolean = false): string[] => {
  if (useVision) {
    return Object.values(OPENROUTER_CONFIG.visionModels);
  }
  return Object.values(OPENROUTER_CONFIG.models);
};

// Content can be text or multimodal (text + images)
export type MessageContent = string | Array<{
  type: 'text' | 'image_url';
  text?: string;
  image_url?: { url: string };
}>;

export interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: MessageContent;
}

export interface IssueAnalysisData {
  logs: Array<{
    id: number;
    timestamp: string;
    level: string;
    message: string;
    module: string;
    action?: string;
    entityType?: string;
    entityId?: string;
    details?: string;
  }>;
  issueContext?: string;
  suggestedCause?: string;
}

export interface AIResponse {
  content: string;
  isComplete: boolean;
  error?: string;
  isResting?: boolean; // Indicates AI service is unavailable
  issueAnalysis?: IssueAnalysisData; // Include logs for UI display
}

// Detect language from message
const detectLanguage = (text: string): 'en' | 'fr' => {
  const frenchIndicators = [
    'comment', 'quoi', 'pourquoi', 'est-ce', 'qu\'est', 'bonjour', 'merci',
    'je', 'tu', 'nous', 'vous', 'ils', 'elles', 'est', 'sont', 'avoir',
    'être', 'faire', 'aller', 'voir', 'savoir', 'pouvoir', 'vouloir',
    'devis', 'vente', 'commande', 'client', 'service', 'intervention'
  ];
  
  const lowerText = text.toLowerCase();
  const frenchCount = frenchIndicators.filter(word => lowerText.includes(word)).length;
  
  return frenchCount >= 2 ? 'fr' : 'en';
};

// Check if message contains images
const hasImages = (messages: ChatMessage[]): boolean => {
  return messages.some(msg => {
    if (Array.isArray(msg.content)) {
      return msg.content.some(c => c.type === 'image_url');
    }
    return false;
  });
};

// Try streaming with a specific API key and model
const tryStreamWithKeyAndModel = async (
  apiKey: string,
  model: string,
  messages: ChatMessage[],
  onChunk: (fullContent: string) => void
): Promise<{ success: boolean; content: string; error?: string }> => {
  try {
    const response = await fetch(OPENROUTER_CONFIG.apiUrl, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${apiKey}`,
        'HTTP-Referer': OPENROUTER_CONFIG.siteUrl,
        'X-Title': OPENROUTER_CONFIG.siteName,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        model,
        messages,
        max_tokens: 2048,
        temperature: 0.7,
        stream: true
      })
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return { 
        success: false, 
        content: '', 
        error: errorData.error?.message || `API error: ${response.status}` 
      };
    }

    if (!response.body) {
      return { success: false, content: '', error: 'No response body' };
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let fullContent = '';
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });

      // Process complete lines
      let newlineIndex: number;
      while ((newlineIndex = buffer.indexOf('\n')) !== -1) {
        let line = buffer.slice(0, newlineIndex);
        buffer = buffer.slice(newlineIndex + 1);

        // Handle CRLF
        if (line.endsWith('\r')) line = line.slice(0, -1);
        
        // Skip empty lines and comments
        if (line.startsWith(':') || line.trim() === '') continue;
        if (!line.startsWith('data: ')) continue;

        const jsonStr = line.slice(6).trim();
        if (jsonStr === '[DONE]') break;

        try {
          const parsed = JSON.parse(jsonStr);
          const content = parsed.choices?.[0]?.delta?.content as string | undefined;
          if (content) {
            fullContent += content;
            onChunk(fullContent);
          }
        } catch {
          // Incomplete JSON, put it back in buffer
          buffer = line + '\n' + buffer;
          break;
        }
      }
    }

    // Process any remaining buffer
    if (buffer.trim()) {
      for (let raw of buffer.split('\n')) {
        if (!raw) continue;
        if (raw.endsWith('\r')) raw = raw.slice(0, -1);
        if (raw.startsWith(':') || raw.trim() === '') continue;
        if (!raw.startsWith('data: ')) continue;
        const jsonStr = raw.slice(6).trim();
        if (jsonStr === '[DONE]') continue;
        try {
          const parsed = JSON.parse(jsonStr);
          const content = parsed.choices?.[0]?.delta?.content as string | undefined;
          if (content) {
            fullContent += content;
            onChunk(fullContent);
          }
        } catch { /* ignore */ }
      }
    }

    return { success: true, content: fullContent };
  } catch (error) {
    return { 
      success: false, 
      content: '', 
      error: error instanceof Error ? error.message : 'Unknown error' 
    };
  }
};

// Intelligent fallback streaming - tries multiple models and keys
const tryStreamWithFallback = async (
  messages: ChatMessage[],
  onChunk: (fullContent: string) => void,
  useVisionModel: boolean = false
): Promise<{ success: boolean; content: string; error?: string; modelUsed?: string }> => {
  const models = getModelsList(useVisionModel);
  const apiKeys = await getApiKeys();
  
  if (apiKeys.length === 0) {
    console.error('No API keys configured. Go to Settings → Integrations → OpenRouter to add your keys.');
    return { success: false, content: '', error: 'No API keys configured. Please add your OpenRouter API key in Settings → Integrations.' };
  }
  
  // Try each model
  for (const model of models) {
    // Try each API key for this model
    for (let keyIndex = 0; keyIndex < apiKeys.length; keyIndex++) {
      const apiKey = apiKeys[keyIndex];
      console.log(`Trying model: ${model.split('/').pop()} with key ${keyIndex + 1}/${apiKeys.length}...`);
      
      const result = await tryStreamWithKeyAndModel(apiKey, model, messages, onChunk);
      
      if (result.success) {
        console.log(`✅ Success with model: ${model.split('/').pop()}`);
        return { ...result, modelUsed: model };
      }
      
      // Check if error suggests we should try next key vs next model
      const errorLower = (result.error || '').toLowerCase();
      
      if (errorLower.includes('spend limit') || errorLower.includes('payment required') || errorLower.includes('402')) {
        console.warn(`Key ${keyIndex + 1} exhausted (spend limit), trying next key...`);
        continue;
      }
      
      if (errorLower.includes('rate limit') || errorLower.includes('quota') || errorLower.includes('429')) {
        console.warn(`Rate limit on key ${keyIndex + 1}, trying next key...`);
        continue;
      }
      
      // For other errors, try next key once then move to next model
      if (keyIndex === 0) {
        console.warn(`Error with model ${model.split('/').pop()}: ${result.error}, trying next key...`);
        continue;
      }
      
      console.warn(`Model ${model.split('/').pop()} failed, moving to next model...`);
      break; // Move to next model
    }
  }
  
  return { success: false, content: '', error: 'All models and keys exhausted. Your API keys may be rate-limited or out of credits. Check your OpenRouter account.' };
};

// Legacy wrapper for backward compatibility
const tryStreamWithKey = async (
  apiKey: string,
  messages: ChatMessage[],
  onChunk: (fullContent: string) => void,
  useVisionModel: boolean = false
): Promise<{ success: boolean; content: string; error?: string }> => {
  const model = useVisionModel 
    ? OPENROUTER_CONFIG.visionModels.primary 
    : OPENROUTER_CONFIG.models.primary;
  return tryStreamWithKeyAndModel(apiKey, model, messages, onChunk);
};

// Context options for AI requests
export interface AIContextOptions {
  currentRoute?: string;
  userData?: {
    userName?: string;
    userRole?: string;
    isMainAdmin?: boolean;
  };
}

// Stream response from OpenRouter with real-time updates and fallback
export const streamMessageToAI = async (
  userMessage: string,
  conversationHistory: ChatMessage[] = [],
  onChunk: (fullContent: string) => void,
  onDone: () => void,
  contextOptions?: AIContextOptions,
  imageAttachments?: string[] // UploadThing URLs of attached images
): Promise<AIResponse> => {
  try {
    // Determine if we need vision model
    const useVisionModel = (imageAttachments && imageAttachments.length > 0) || hasImages(conversationHistory);
    
    // Build context-aware prompt
    let contextPrompt = contextOptions?.currentRoute 
      ? buildContextPrompt(contextOptions.currentRoute, contextOptions.userData)
      : undefined;
    
    // If viewing a detail page, automatically fetch entity data for context
    if (contextOptions?.currentRoute && isDetailPage(contextOptions.currentRoute)) {
      try {
        const entityData = await fetchContextData(contextOptions.currentRoute);
        if (entityData) {
          contextPrompt = (contextPrompt || '') + `\n\n## CURRENT ENTITY DATA\n${entityData}\n\nYou have access to this data - use it to provide specific, relevant answers about this ${contextOptions.currentRoute.includes('contact') ? 'contact' : contextOptions.currentRoute.includes('offer') ? 'offer' : contextOptions.currentRoute.includes('sale') ? 'sale' : contextOptions.currentRoute.includes('service-order') ? 'service order' : contextOptions.currentRoute.includes('dispatch') ? 'dispatch' : contextOptions.currentRoute.includes('installation') ? 'installation' : 'item'}.`;
          console.log('Fetched entity context data for AI');
        }
      } catch (error) {
        console.warn('Failed to fetch entity context data:', error);
      }
    }

    // First, check if this is an issue report that needs log analysis
    if (isIssueReportMessage(userMessage)) {
      console.log('Issue report detected, fetching logs...');
      const issueAnalysis = await analyzeUserIssue(userMessage);
      
      if (issueAnalysis.isIssueReport && issueAnalysis.formattedResponse) {
        const language = detectLanguage(userMessage);
        const systemPrompt = getSystemPrompt(language, contextPrompt);
        
        const enhancedMessage = `${userMessage}\n\n${issueAnalysis.formattedResponse}`;
        
        const messages: ChatMessage[] = [
          { role: 'system', content: systemPrompt },
          ...conversationHistory,
          { role: 'user', content: enhancedMessage }
        ];

        // Use intelligent fallback with multiple models and keys
        const result = await tryStreamWithFallback(messages, onChunk, useVisionModel);
        
        if (result.success) {
          onDone();
          sendChatNotification(userMessage, result.content);
          
          // Build issue analysis data for UI display
          const issueAnalysisData: IssueAnalysisData | undefined = issueAnalysis.displayLogs && issueAnalysis.displayLogs.length > 0
            ? {
                logs: issueAnalysis.displayLogs.map(log => ({
                  id: log.id,
                  timestamp: log.timestamp,
                  level: log.level,
                  message: log.message,
                  module: log.module,
                  action: log.action,
                  entityType: log.entityType,
                  entityId: log.entityId,
                  details: log.details,
                })),
                issueContext: issueAnalysis.issueContext,
                suggestedCause: issueAnalysis.suggestedCause,
              }
            : undefined;
          
          return {
            content: result.content,
            isComplete: true,
            issueAnalysis: issueAnalysisData,
          };
        }
      }
    }

    // Second, check if this is a data query that needs real-time API data
    const dataQueryResult = await detectAndExecuteDataQuery(userMessage);
    
    if (dataQueryResult?.success && dataQueryResult.data) {
      // We got real data - include it in the prompt for the AI to enhance
      const language = detectLanguage(userMessage);
      const systemPrompt = getSystemPrompt(language, contextPrompt);
      
      const enhancedMessage = `${userMessage}\n\n[REAL-TIME DATA FROM SYSTEM]:\n${dataQueryResult.data}\n\n[INSTRUCTION]: Use the above real-time data to answer the user's question. Format nicely and add helpful context if needed.`;
      
      const messages: ChatMessage[] = [
        { role: 'system', content: systemPrompt },
        ...conversationHistory,
        { role: 'user', content: enhancedMessage }
      ];

      // Use intelligent fallback with multiple models and keys
      const result = await tryStreamWithFallback(messages, onChunk, false);
      
      if (result.success) {
        onDone();
        sendChatNotification(userMessage, result.content);
        return {
          content: result.content,
          isComplete: true
        };
      }
      
      // If AI fails, return the raw data as fallback
      onChunk(dataQueryResult.data);
      onDone();
      sendChatNotification(userMessage, dataQueryResult.data);
      return {
        content: dataQueryResult.data,
        isComplete: true
      };
    }
    
    // Normal flow - no data query detected
    const language = detectLanguage(userMessage);
    const systemPrompt = getSystemPrompt(language, contextPrompt);

    // Build user message content (text or multimodal with images)
    // Images are constrained to app-related analysis only
    let userContent: MessageContent = userMessage;
    if (imageAttachments && imageAttachments.length > 0) {
      const imageInstructions = language === 'fr' 
        ? `Analysez cette image UNIQUEMENT dans le contexte de FlowService. Identifiez:
- Écrans/interfaces de l'application FlowService
- Erreurs ou problèmes dans l'application
- Documents métier (devis, factures, bons de commande)
- Données clients, contacts ou workflows
- Équipements ou installations à gérer

Si l'image n'est pas liée à FlowService ou à un contexte métier, répondez: "Je ne peux analyser que des images liées à FlowService ou à vos opérations métier."`
        : `Analyze this image ONLY in the context of FlowService. Look for:
- FlowService application screens/interfaces
- Errors or issues in the application
- Business documents (quotes, invoices, work orders)
- Customer data, contacts, or workflows
- Equipment or installations to manage

If the image is not related to FlowService or business context, respond: "I can only analyze images related to FlowService or your business operations."`;
      
      userContent = [
        { type: 'text', text: userMessage ? `${userMessage}\n\n${imageInstructions}` : imageInstructions },
        ...imageAttachments.map(url => ({
          type: 'image_url' as const,
          image_url: { url }
        }))
      ];
    }

    const messages: ChatMessage[] = [
      { role: 'system', content: systemPrompt },
      ...conversationHistory,
      { role: 'user', content: userContent }
    ];

    // Use intelligent fallback with multiple models and keys
    const result = await tryStreamWithFallback(messages, onChunk, useVisionModel);
    
    if (result.success) {
      onDone();
      // Send notification to ntfy
      sendChatNotification(userMessage, result.content);
      return {
        content: result.content,
        isComplete: true
      };
    }

    // All models and keys failed - AI is "resting"
    console.error('All models and API keys failed');
    onDone();
    return {
      content: '',
      isComplete: false,
      isResting: true,
      error: 'All AI models exhausted'
    };
  } catch (error) {
    console.error('AI Assistant error:', error);
    onDone();
    return {
      content: '',
      isComplete: false,
      isResting: true,
      error: error instanceof Error ? error.message : 'Unknown error occurred'
    };
  }
};

// =====================================================
// Prompt-Override Streaming (used for specialized modes like form creation)
// =====================================================

/**
 * Stream a response using a caller-provided system prompt.
 * This intentionally bypasses the default FlowService assistant prompt (and its strict boundaries),
 * so features like Dynamic Form generation can use a dedicated prompt and response format.
 */
export const streamMessageToAIWithSystemPrompt = async (
  systemPrompt: string,
  userMessage: string,
  conversationHistory: ChatMessage[] = [],
  onChunk: (fullContent: string) => void,
  onDone: () => void,
  imageAttachments?: string[]
): Promise<AIResponse> => {
  try {
    const useVisionModel = Boolean(imageAttachments && imageAttachments.length > 0);

    let userContent: MessageContent = userMessage;
    if (imageAttachments && imageAttachments.length > 0) {
      userContent = [
        { type: 'text', text: userMessage },
        ...imageAttachments.map(url => ({
          type: 'image_url' as const,
          image_url: { url },
        })),
      ];
    }

    const messages: ChatMessage[] = [
      { role: 'system', content: systemPrompt },
      ...conversationHistory,
      { role: 'user', content: userContent },
    ];

    const result = await tryStreamWithFallback(messages, onChunk, useVisionModel);

    if (result.success) {
      onDone();
      sendChatNotification(userMessage, result.content);
      return {
        content: result.content,
        isComplete: true,
      };
    }

    console.error('All models and API keys failed (prompt override)');
    onDone();
    return {
      content: '',
      isComplete: false,
      isResting: true,
      error: 'All AI models exhausted',
    };
  } catch (error) {
    console.error('AI Assistant error (prompt override):', error);
    onDone();
    return {
      content: '',
      isComplete: false,
      isResting: true,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
};

// Non-streaming fallback (kept for compatibility)
export const sendMessageToAI = async (
  userMessage: string,
  conversationHistory: ChatMessage[] = []
): Promise<AIResponse> => {
  return new Promise((resolve) => {
    let finalContent = '';
    streamMessageToAI(
      userMessage,
      conversationHistory,
      (content) => { finalContent = content; },
      () => {
        resolve({
          content: finalContent,
          isComplete: true
        });
      }
    ).catch((error) => {
      resolve({
        content: '',
        isComplete: false,
        isResting: true,
        error: error instanceof Error ? error.message : 'Unknown error'
      });
    });
  });
};
