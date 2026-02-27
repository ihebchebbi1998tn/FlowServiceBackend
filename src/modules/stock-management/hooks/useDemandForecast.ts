import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { invokeEdgeFunction, supabaseUrl } from '@/integrations/supabase/client';

interface TransactionData {
  articleId: number;
  articleName: string;
  articleNumber: string;
  currentStock: number;
  minStock: number;
  transactions: {
    transactionType: string;
    quantity: number;
    createdAt: string;
    referenceType?: string;
  }[];
}

export interface ForecastPrediction {
  period: string;
  predictedDemand: number;
  confidence: number;
  trend: 'increasing' | 'decreasing' | 'stable';
}

export interface ForecastRecommendation {
  reorderPoint: number;
  suggestedOrderQuantity: number;
  urgency: 'critical' | 'high' | 'medium' | 'low';
  reasoning: string;
}

export interface ArticleForecast {
  articleId: number;
  articleName: string;
  articleNumber: string;
  currentStock: number;
  minStock: number;
  predictions: ForecastPrediction;
  recommendations: ForecastRecommendation;
  insights: string[];
}

export interface ForecastSummary {
  criticalItems: number;
  totalPredictedDemand: number;
  highestDemandItem: string;
  overallTrend: string;
}

export interface DemandForecastResponse {
  forecasts: ArticleForecast[];
  summary: ForecastSummary;
}

async function fetchDemandForecast(
  articles: TransactionData[],
  language: string
): Promise<DemandForecastResponse> {
  if (!supabaseUrl) {
    throw new Error("Cloud not configured");
  }
  
  return invokeEdgeFunction<DemandForecastResponse>('stock-demand-forecast', { 
    articles, 
    language 
  });
}

export function useDemandForecast() {
  const { i18n, t } = useTranslation('stock-management');
  const [forecastData, setForecastData] = useState<DemandForecastResponse | null>(null);

  const mutation = useMutation({
    mutationFn: (articles: TransactionData[]) =>
      fetchDemandForecast(articles, i18n.language),
    onSuccess: (data) => {
      setForecastData(data);
      toast.success(t('forecast.success'));
    },
    onError: (error: Error) => {
      console.error('Demand forecast error:', error);
      if (error.message.includes('Rate limit')) {
        toast.error(t('forecast.rate_limit'));
      } else if (error.message.includes('Credits')) {
        toast.error(t('forecast.credits_exhausted'));
      } else {
        toast.error(t('forecast.error'));
      }
    },
  });

  return {
    forecastData,
    isLoading: mutation.isPending,
    error: mutation.error,
    generateForecast: mutation.mutate,
    clearForecast: () => setForecastData(null),
  };
}
