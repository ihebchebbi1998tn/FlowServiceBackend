// AI Follow-up Questions Service
// Generates contextual follow-up question suggestions using LLM

import { getUsableApiKeys } from '@/services/openRouterModelsService';

const OPENROUTER_API_URL = 'https://openrouter.ai/api/v1/chat/completions';
const MODEL = 'openrouter/aurora-alpha';

// Rate limiting state
let lastRequestTime = 0;
const MIN_REQUEST_INTERVAL = 2000;

/**
 * Generate follow-up question suggestions using LLM based on the conversation context
 * @param userMessage The user's original question
 * @param aiResponse The AI's response
 * @param language The current language ('en' | 'fr')
 * @returns Promise of array of suggested follow-up questions (3 suggestions)
 */
export const generateFollowUpQuestions = async (
  userMessage: string,
  aiResponse: string,
  language: 'en' | 'fr' = 'en'
): Promise<string[]> => {
  // Rate limiting - wait if needed
  const now = Date.now();
  const timeSinceLastRequest = now - lastRequestTime;
  if (timeSinceLastRequest < MIN_REQUEST_INTERVAL) {
    await new Promise(resolve => setTimeout(resolve, MIN_REQUEST_INTERVAL - timeSinceLastRequest));
  }
  lastRequestTime = Date.now();

  try {
    const systemPrompt = language === 'fr' 
      ? `Tu es un assistant qui génère des questions de suivi pertinentes pour Flowentra (application de gestion de services terrain et CRM). 
Basé sur la conversation, génère exactement 3 questions de suivi DIFFÉRENTES de la question originale.
Les questions doivent:
- Être en français, concises (max 10 mots)
- Explorer des aspects NOUVEAUX liés au sujet
- NE JAMAIS répéter ou reformuler la question originale
- Être pertinentes pour Flowentra
Réponds UNIQUEMENT avec un tableau JSON de 3 questions, sans explication.`
      : `You are an assistant that generates relevant follow-up questions for Flowentra (a field service management and CRM application).
Based on the conversation, generate exactly 3 follow-up questions that are DIFFERENT from the original question.
Questions must:
- Be in English, concise (max 10 words)
- Explore NEW aspects related to the topic
- NEVER repeat or rephrase the original question
- Be relevant to Flowentra features
Reply ONLY with a JSON array of 3 questions, no explanation.`;

    const userPrompt = language === 'fr'
      ? `Question originale (NE PAS RÉPÉTER): "${userMessage}"\n\nRéponse de l'IA: "${aiResponse.substring(0, 500)}"\n\nGénère 3 questions de suivi DIFFÉRENTES en JSON:`
      : `Original question (DO NOT REPEAT): "${userMessage}"\n\nAI response: "${aiResponse.substring(0, 500)}"\n\nGenerate 3 DIFFERENT follow-up questions as JSON:`;

    // Try with fallback keys from local cache
    const apiKeys = getUsableApiKeys();
    if (apiKeys.length === 0) {
      return getSmartFallbackQuestions(userMessage, aiResponse, language);
    }
    let lastError: Error | null = null;
    for (let attempt = 0; attempt < apiKeys.length; attempt++) {
      const apiKey = apiKeys[attempt];
      
      try {
        const response = await fetch(OPENROUTER_API_URL, {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${apiKey}`,
            'Content-Type': 'application/json',
            'HTTP-Referer': window.location.origin,
          },
          body: JSON.stringify({
            model: MODEL,
            messages: [
              { role: 'system', content: systemPrompt },
              { role: 'user', content: userPrompt }
            ],
            max_tokens: 200,
            temperature: 0.7,
          }),
        });

        if (response.ok) {
          const data = await response.json();
          const content = data.choices?.[0]?.message?.content?.trim();

          if (content) {
            // Parse JSON from response
            const jsonMatch = content.match(/\[[\s\S]*?\]/);
            if (jsonMatch) {
              const parsed = JSON.parse(jsonMatch[0]);
              if (Array.isArray(parsed) && parsed.length > 0) {
                // Filter out any questions too similar to the original
                const filtered = parsed
                  .slice(0, 3)
                  .map(q => String(q).trim())
                  .filter(q => !isSimilarQuestion(q, userMessage));
                
                if (filtered.length >= 2) {
                  return filtered.slice(0, 3);
                }
              }
            }
          }
        } else if (response.status === 429) {
          console.warn(`OpenRouter rate limited on key ${attempt + 1}, trying next key...`);
          continue; // Try next key
        } else {
          console.error('OpenRouter API error:', response.status);
        }
      } catch (err) {
        lastError = err as Error;
        console.warn(`API call failed on key ${attempt + 1}:`, err);
      }
    }

    if (lastError) {
      console.error('All API keys exhausted:', lastError);
    }
    return getSmartFallbackQuestions(userMessage, aiResponse, language);
  } catch (error) {
    console.error('Error generating follow-up questions:', error);
    return getSmartFallbackQuestions(userMessage, aiResponse, language);
  }
};

/**
 * Check if two questions are semantically similar
 */
const isSimilarQuestion = (question1: string, question2: string): boolean => {
  const normalize = (s: string) => s.toLowerCase().replace(/[?!.,]/g, '').trim();
  const q1 = normalize(question1);
  const q2 = normalize(question2);
  
  // Exact match
  if (q1 === q2) return true;
  
  // One contains the other
  if (q1.includes(q2) || q2.includes(q1)) return true;
  
  // Extract key words (more than 3 chars)
  const words1 = new Set(q1.split(/\s+/).filter(w => w.length > 3));
  const words2 = new Set(q2.split(/\s+/).filter(w => w.length > 3));
  
  // Calculate word overlap
  const intersection = [...words1].filter(w => words2.has(w));
  const minSetSize = Math.min(words1.size, words2.size);
  
  // If more than 60% of words overlap, consider similar
  return minSetSize > 0 && intersection.length / minSetSize > 0.6;
};

/**
 * Smart fallback questions that never repeat the user's question
 */
const getSmartFallbackQuestions = (
  userMessage: string,
  aiResponse: string,
  language: 'en' | 'fr'
): string[] => {
  const lowerResponse = aiResponse.toLowerCase();
  const lowerMessage = userMessage.toLowerCase();
  
  // Topic detection with expanded patterns - ordered by specificity
  const topics = {
    onboarding: /newly set up|getting started|start by adding|recommended starting|first contact|setup tasks|checklist|begin with|initial setup|no data|0 contacts|empty|configure|new account/i,
    summary: /summary|overview|résumé|aperçu|how.*business|état.*entreprise|business health|full summary|comprehensive/i,
    tasks: /task|tâche|todo|action|complete|assign|overdue|priority|deadline/i,
    contacts: /contact|client|customer|supplier|fournisseur|lead|prospect/i,
    sales: /sale|vente|revenue|chiffre|order|commande|invoice|payment/i,
    service: /service|intervention|dispatch|technician|technicien|field|terrain/i,
    offers: /offer|devis|quote|proposal|proposition|estimate/i,
    projects: /project|projet|kanban|board|column|sprint/i,
    inventory: /inventory|stock|article|product|material|asset/i,
    reports: /report|analytics|statistics|dashboard|metrics|kpi/i,
  };
  
  // Detect which topic the conversation is about - check response first for better context
  let detectedTopic: string | null = null;
  for (const [topic, pattern] of Object.entries(topics)) {
    if (pattern.test(lowerResponse)) {
      detectedTopic = topic;
      break;
    }
  }
  // Fallback to message if no topic detected from response
  if (!detectedTopic) {
    for (const [topic, pattern] of Object.entries(topics)) {
      if (pattern.test(lowerMessage)) {
        detectedTopic = topic;
        break;
      }
    }
  }
  
  // Question pools organized by topic - each has multiple options to avoid repetition
  const questionPools: Record<string, { en: string[], fr: string[] }> = {
    onboarding: {
      en: [
        "How do I add my first contact?",
        "Guide me through product setup",
        "How to create my first offer?",
        "What should I set up first?",
        "Import contacts from CSV?",
        "Configure company settings",
        "Set up my team members"
      ],
      fr: [
        "Comment ajouter mon premier contact ?",
        "Guide pour configurer les produits",
        "Comment créer mon premier devis ?",
        "Que dois-je configurer en premier ?",
        "Importer contacts depuis CSV ?",
        "Configurer les paramètres entreprise",
        "Ajouter des membres d'équipe"
      ]
    },
    summary: {
      en: [
        "Show today's activity",
        "What needs my attention?",
        "Compare with last month",
        "Show pending actions",
        "Revenue breakdown by source",
        "Team performance overview",
        "What are the top priorities?"
      ],
      fr: [
        "Activité d'aujourd'hui",
        "Qu'est-ce qui nécessite mon attention ?",
        "Comparer avec le mois dernier",
        "Actions en attente",
        "Répartition revenus par source",
        "Performance de l'équipe",
        "Quelles sont les priorités ?"
      ]
    },
    tasks: {
      en: [
        "What are my priority tasks?",
        "How to assign tasks to team?",
        "Show completed tasks today",
        "What deadlines are coming up?",
        "How to bulk update task status?",
        "Create a recurring task",
        "Filter tasks by assignee"
      ],
      fr: [
        "Quelles sont mes tâches prioritaires ?",
        "Comment assigner des tâches ?",
        "Tâches terminées aujourd'hui ?",
        "Quelles échéances arrivent ?",
        "Mise à jour groupée des statuts",
        "Créer une tâche récurrente",
        "Filtrer par responsable"
      ]
    },
    sales: {
      en: [
        "What's the revenue this month?",
        "Show pending sales orders",
        "How to create a sale?",
        "Top customers by revenue?",
        "Sales conversion rate?",
        "Export sales report",
        "Show sales by status"
      ],
      fr: [
        "Chiffre d'affaires ce mois ?",
        "Ventes en attente ?",
        "Comment créer une vente ?",
        "Meilleurs clients ?",
        "Taux de conversion ?",
        "Exporter rapport ventes",
        "Ventes par statut"
      ]
    },
    service: {
      en: [
        "What service orders today?",
        "How to assign a technician?",
        "Show ongoing interventions",
        "Dispatch calendar view?",
        "Technician availability?",
        "Service orders by priority",
        "Unplanned jobs needing dispatch"
      ],
      fr: [
        "Interventions aujourd'hui ?",
        "Comment assigner un technicien ?",
        "Interventions en cours ?",
        "Voir le calendrier ?",
        "Disponibilité techniciens ?",
        "Ordres par priorité",
        "Travaux non planifiés"
      ]
    },
    contacts: {
      en: [
        "How to add a new contact?",
        "Search contacts by company",
        "View contact history",
        "Export contacts list",
        "How to tag contacts?",
        "Show VIP customers",
        "Contact communication log"
      ],
      fr: [
        "Ajouter un nouveau contact ?",
        "Rechercher par entreprise",
        "Historique du contact",
        "Exporter les contacts",
        "Comment taguer contacts ?",
        "Clients VIP ?",
        "Journal des communications"
      ]
    },
    offers: {
      en: [
        "How to create an offer?",
        "Pending offers to follow up?",
        "Convert offer to sale",
        "Offer expiring soon?",
        "Duplicate an existing offer",
        "Add discount to offer",
        "Send offer by email"
      ],
      fr: [
        "Comment créer un devis ?",
        "Devis en attente ?",
        "Convertir devis en vente",
        "Devis expirant bientôt ?",
        "Dupliquer un devis",
        "Ajouter remise au devis",
        "Envoyer devis par email"
      ]
    },
    projects: {
      en: [
        "Create a new project",
        "View project kanban board",
        "Add team to project",
        "Project progress overview",
        "Set project milestones",
        "Project task statistics"
      ],
      fr: [
        "Créer un projet",
        "Voir tableau kanban",
        "Ajouter équipe au projet",
        "Avancement du projet",
        "Définir jalons projet",
        "Statistiques tâches projet"
      ]
    },
    inventory: {
      en: [
        "Low stock alerts?",
        "Add inventory item",
        "Stock movement history",
        "Inventory by location",
        "Reorder point settings",
        "Search by SKU"
      ],
      fr: [
        "Alertes stock bas ?",
        "Ajouter article stock",
        "Historique mouvements",
        "Stock par emplacement",
        "Seuils de réapprovisionnement",
        "Rechercher par SKU"
      ]
    },
    reports: {
      en: [
        "View dashboard KPIs",
        "Export analytics report",
        "Revenue trends chart",
        "Team performance metrics",
        "Weekly summary report",
        "Custom date range report"
      ],
      fr: [
        "Voir KPIs tableau de bord",
        "Exporter rapport analytique",
        "Graphique tendance revenus",
        "Métriques équipe",
        "Rapport hebdomadaire",
        "Rapport période personnalisée"
      ]
    },
    default: {
      en: [
        "What can I do in Flowentra?",
        "Show me today's summary",
        "Navigate to settings",
        "View my notifications",
        "Help with navigation",
        "What features are available?"
      ],
      fr: [
        "Que puis-je faire ?",
        "Résumé d'aujourd'hui",
        "Aller aux paramètres",
        "Voir mes notifications",
        "Aide à la navigation",
        "Fonctionnalités disponibles ?"
      ]
    }
  };
  
  const pool = questionPools[detectedTopic || 'default'] || questionPools.default;
  const questions = language === 'fr' ? pool.fr : pool.en;
  
  // Filter out questions similar to the user's original question
  const filtered = questions.filter(q => !isSimilarQuestion(q, userMessage));
  
  // Shuffle and pick 3
  const shuffled = filtered.sort(() => Math.random() - 0.5);
  return shuffled.slice(0, 3);
};

/**
 * Detect language from text - analyzes user's message to determine language
 * Returns 'fr' for French, 'en' for English
 */
export const detectLanguageFromText = (text: string): 'en' | 'fr' => {
  const lowerText = text.toLowerCase();
  
  // Strong French indicators - single word is enough
  const strongFrenchIndicators = [
    'bonjour', 'bonsoir', 'salut', 'merci', 'oui', 'non', 's\'il vous plaît',
    'qu\'est-ce', 'est-ce que', 'pourquoi', 'comment', 'combien', 'quand',
    'où', 'qui', 'quoi', 'quel', 'quelle', 'quels', 'quelles',
    'je voudrais', 'j\'aimerais', 'pouvez-vous', 'peux-tu',
    'aujourd\'hui', 'demain', 'hier', 'maintenant'
  ];
  
  // Check for strong indicators first (single match is enough)
  if (strongFrenchIndicators.some(word => lowerText.includes(word))) {
    return 'fr';
  }
  
  // Common French words that appear in sentences
  const frenchWords = [
    'je', 'tu', 'il', 'elle', 'nous', 'vous', 'ils', 'elles',
    'le', 'la', 'les', 'un', 'une', 'des', 'du', 'de', 'à',
    'est', 'sont', 'suis', 'es', 'sommes', 'êtes',
    'ai', 'as', 'a', 'avons', 'avez', 'ont',
    'mon', 'ma', 'mes', 'ton', 'ta', 'tes', 'son', 'sa', 'ses',
    'ce', 'cet', 'cette', 'ces', 'ça', 'cela',
    'pour', 'avec', 'dans', 'sur', 'par', 'entre', 'vers',
    'faire', 'voir', 'avoir', 'être', 'aller', 'venir', 'prendre',
    'devis', 'vente', 'ventes', 'commande', 'commandes', 'client', 'clients',
    'contact', 'contacts', 'tâche', 'tâches', 'projet', 'projets',
    'service', 'services', 'intervention', 'interventions', 'technicien',
    'aide', 'besoin', 'problème', 'erreur', 'afficher', 'montrer', 'créer'
  ];
  
  // Count French word matches
  const words = lowerText.split(/\s+/);
  const frenchWordCount = words.filter(word => frenchWords.includes(word.replace(/[.,?!;:]/g, ''))).length;
  
  // If more than 30% of words are French, or at least 2 French words detected
  const frenchRatio = words.length > 0 ? frenchWordCount / words.length : 0;
  
  return (frenchWordCount >= 1 && frenchRatio >= 0.2) || frenchWordCount >= 2 ? 'fr' : 'en';
};
