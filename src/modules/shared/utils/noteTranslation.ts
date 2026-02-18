// Utility for translating backend notes to current locale
// This handles notes that come from the backend in one language and need to be displayed in the user's current language

// Pattern matching for known note patterns with their translations
const notePatterns: Array<{
  pattern: RegExp;
  en: string;
  fr: string;
}> = [
  // Checklist added patterns
  {
    pattern: /^Checklist ajouté\s*:\s*(.+)$/i,
    en: 'Checklist added: $1',
    fr: 'Checklist ajouté : $1',
  },
  {
    pattern: /^Checklist added\s*:\s*(.+)$/i,
    en: 'Checklist added: $1',
    fr: 'Checklist ajouté : $1',
  },
  // Checklist completed patterns
  {
    pattern: /^Checklist complété\s*:\s*(.+)$/i,
    en: 'Checklist completed: $1',
    fr: 'Checklist complété : $1',
  },
  {
    pattern: /^Checklist completed\s*:\s*(.+)$/i,
    en: 'Checklist completed: $1',
    fr: 'Checklist complété : $1',
  },
  // Checklist details - added
  {
    pattern: /^Un nouveau checklist "(.+)" a été ajouté\.$/i,
    en: 'A new checklist "$1" has been added.',
    fr: 'Un nouveau checklist "$1" a été ajouté.',
  },
  {
    pattern: /^A new checklist "(.+)" has been added\.$/i,
    en: 'A new checklist "$1" has been added.',
    fr: 'Un nouveau checklist "$1" a été ajouté.',
  },
  // Checklist details - completed
  {
    pattern: /^Le checklist "(.+)" a été complété\.$/i,
    en: 'The checklist "$1" has been completed.',
    fr: 'Le checklist "$1" a été complété.',
  },
  {
    pattern: /^The checklist "(.+)" has been completed\.$/i,
    en: 'The checklist "$1" has been completed.',
    fr: 'Le checklist "$1" a été complété.',
  },
  // Status changed patterns
  {
    pattern: /^🔄 Status changed from '([^']+)' to '([^']+)'$/,
    en: '🔄 Status changed from \'$1\' to \'$2\'',
    fr: '🔄 Statut changé de \'$1\' à \'$2\'',
  },
  {
    pattern: /^🔄 Statut changé de '([^']+)' à '([^']+)'$/,
    en: '🔄 Status changed from \'$1\' to \'$2\'',
    fr: '🔄 Statut changé de \'$1\' à \'$2\'',
  },
  // Note type translations
  {
    pattern: /^Note added$/i,
    en: 'Note added',
    fr: 'Note ajoutée',
  },
  {
    pattern: /^Note ajoutée$/i,
    en: 'Note added',
    fr: 'Note ajoutée',
  },
  // Checklist added from patterns (showing source)
  {
    pattern: /^Checklist added from (Offer|Sale|Service Order|Dispatch)\s*:\s*(.+)$/i,
    en: 'Checklist added from $1: $2',
    fr: 'Checklist ajouté depuis $1 : $2',
  },
  {
    pattern: /^Checklist ajouté depuis (Offre|Vente|Ordre de Service|Intervention)\s*:\s*(.+)$/i,
    en: 'Checklist added from $1: $2',
    fr: 'Checklist ajouté depuis $1 : $2',
  },
  // Checklist completed from patterns (showing source)
  {
    pattern: /^Checklist completed from (Offer|Sale|Service Order|Dispatch)\s*:\s*(.+)$/i,
    en: 'Checklist completed from $1: $2',
    fr: 'Checklist complété depuis $1 : $2',
  },
  {
    pattern: /^Checklist complété depuis (Offre|Vente|Ordre de Service|Intervention)\s*:\s*(.+)$/i,
    en: 'Checklist completed from $1: $2',
    fr: 'Checklist complété depuis $1 : $2',
  },
];

// Entity type translations
const entityTypeTranslations: Record<string, { en: string; fr: string }> = {
  'offer': { en: 'Offer', fr: 'Offre' },
  'sale': { en: 'Sale', fr: 'Vente' },
  'service_order': { en: 'Service Order', fr: 'Ordre de Service' },
  'dispatch': { en: 'Dispatch', fr: 'Intervention' },
  // French to English mappings
  'offre': { en: 'Offer', fr: 'Offre' },
  'vente': { en: 'Sale', fr: 'Vente' },
  'ordre de service': { en: 'Service Order', fr: 'Ordre de Service' },
  'intervention': { en: 'Dispatch', fr: 'Intervention' },
};

// Status translations
const statusTranslations: Record<string, { en: string; fr: string }> = {
  'draft': { en: 'Draft', fr: 'Brouillon' },
  'brouillon': { en: 'Draft', fr: 'Brouillon' },
  'sent': { en: 'Sent', fr: 'Envoyé' },
  'envoyé': { en: 'Sent', fr: 'Envoyé' },
  'pending': { en: 'Pending', fr: 'En attente' },
  'en attente': { en: 'Pending', fr: 'En attente' },
  'in_progress': { en: 'In Progress', fr: 'En cours' },
  'en cours': { en: 'In Progress', fr: 'En cours' },
  'completed': { en: 'Completed', fr: 'Terminé' },
  'terminé': { en: 'Completed', fr: 'Terminé' },
  'cancelled': { en: 'Cancelled', fr: 'Annulé' },
  'annulé': { en: 'Cancelled', fr: 'Annulé' },
  'accepted': { en: 'Accepted', fr: 'Accepté' },
  'accepté': { en: 'Accepted', fr: 'Accepté' },
  'rejected': { en: 'Rejected', fr: 'Rejeté' },
  'rejeté': { en: 'Rejected', fr: 'Rejeté' },
  'won': { en: 'Won', fr: 'Gagné' },
  'gagné': { en: 'Won', fr: 'Gagné' },
  'lost': { en: 'Lost', fr: 'Perdu' },
  'perdu': { en: 'Lost', fr: 'Perdu' },
  'scheduled': { en: 'Scheduled', fr: 'Planifié' },
  'planifié': { en: 'Scheduled', fr: 'Planifié' },
  'on_hold': { en: 'On Hold', fr: 'En attente' },
  'ready_for_planning': { en: 'Ready for Planning', fr: 'Prêt pour planification' },
  'prêt pour planification': { en: 'Ready for Planning', fr: 'Prêt pour planification' },
};

/**
 * Translate a note content from backend to the specified locale
 * @param content - The note content from backend
 * @param targetLocale - The target locale ('en' or 'fr')
 * @returns The translated note content
 */
export function translateNote(content: string, targetLocale: 'en' | 'fr'): string {
  if (!content) return content;

  let translatedContent = content;

  // Try to match against known patterns
  for (const { pattern, en, fr } of notePatterns) {
    const match = content.match(pattern);
    if (match) {
      // Replace with the target locale version
      const template = targetLocale === 'fr' ? fr : en;
      
      // Replace capture groups
      translatedContent = template;
      for (let i = 1; i < match.length; i++) {
        let replacement = match[i];
        
        // Also translate entity types and statuses within the captured group
        const lowerReplacement = replacement.toLowerCase();
        if (entityTypeTranslations[lowerReplacement]) {
          replacement = entityTypeTranslations[lowerReplacement][targetLocale];
        } else if (statusTranslations[lowerReplacement]) {
          replacement = statusTranslations[lowerReplacement][targetLocale];
        }
        
        translatedContent = translatedContent.replace(`$${i}`, replacement);
      }
      
      return translatedContent;
    }
  }

  // If no pattern matched, return original content
  return content;
}

/**
 * Translate an array of notes
 * @param notes - Array of note objects with content property
 * @param targetLocale - The target locale ('en' or 'fr')
 * @returns Array of notes with translated content
 */
export function translateNotes<T extends { content?: string; note?: string; description?: string }>(
  notes: T[],
  targetLocale: 'en' | 'fr'
): T[] {
  return notes.map(note => ({
    ...note,
    content: note.content ? translateNote(note.content, targetLocale) : note.content,
    note: note.note ? translateNote(note.note, targetLocale) : note.note,
    description: note.description ? translateNote(note.description, targetLocale) : note.description,
  }));
}

/**
 * Get entity type label in the specified locale
 */
export function getEntityTypeLabel(entityType: string, locale: 'en' | 'fr'): string {
  const key = entityType.toLowerCase();
  return entityTypeTranslations[key]?.[locale] || entityType;
}

/**
 * Get status label in the specified locale
 */
export function getStatusLabel(status: string, locale: 'en' | 'fr'): string {
  const key = status.toLowerCase();
  return statusTranslations[key]?.[locale] || status;
}
