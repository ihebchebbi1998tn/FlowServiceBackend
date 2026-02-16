import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';

interface AiThinkingIndicatorProps {
  isAnalyzingImage?: boolean;
  className?: string;
}

const stagesEn = [
  'AI is thinking',
  'Analysing your question',
  'Processing context',
  'Generating response',
];

const stagesFr = [
  'L\'IA réfléchit',
  'Analyse de votre question',
  'Traitement du contexte',
  'Génération de la réponse',
];

export function AiThinkingIndicator({ isAnalyzingImage, className }: AiThinkingIndicatorProps) {
  const { i18n } = useTranslation();
  const [currentIndex, setCurrentIndex] = useState(0);
  
  const stages = i18n.language === 'fr' ? stagesFr : stagesEn;

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentIndex((prev) => (prev < stages.length - 1 ? prev + 1 : prev));
    }, 2000);
    return () => clearInterval(interval);
  }, [stages.length]);

  const currentText = stages[currentIndex];

  return (
    <div className={cn('flex items-center gap-2 py-1', className)}>
      {/* Simple animated spinner */}
      <div className="relative h-4 w-4">
        <div className="absolute inset-0 rounded-full border-2 border-muted" />
        <div
          className="absolute inset-0 rounded-full border-2 border-transparent border-t-primary"
          style={{ animation: 'spin 0.8s linear infinite' }}
        />
      </div>

      {/* Stage text with subtle fade */}
      <span
        key={currentIndex}
        className="text-sm text-muted-foreground animate-fade-in"
      >
        {currentText}
        <span className="ml-0.5 inline-flex w-4">
          <span className="animate-[blink_1.4s_infinite]">.</span>
          <span className="animate-[blink_1.4s_infinite_0.2s]">.</span>
          <span className="animate-[blink_1.4s_infinite_0.4s]">.</span>
        </span>
      </span>

      {/* Image badge */}
      {isAnalyzingImage && (
        <span className="ml-1 rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
          + image
        </span>
      )}

      <style>{`
        @keyframes spin {
          to { transform: rotate(360deg); }
        }
        @keyframes blink {
          0%, 20% { opacity: 0; }
          40% { opacity: 1; }
          100% { opacity: 0; }
        }
      `}</style>
    </div>
  );
}

export default AiThinkingIndicator;
