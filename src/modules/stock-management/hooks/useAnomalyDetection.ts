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
  avgDailyUsage: number;
  transactions: {
    id: number;
    transactionType: string;
    quantity: number;
    createdAt: string;
    referenceType?: string;
    performedByName?: string;
  }[];
}

export type AnomalySeverity = 'critical' | 'high' | 'medium' | 'low';
export type AnomalyType = 'volume' | 'frequency' | 'pattern' | 'suspicious_removal' | 'discrepancy' | 'rapid_depletion' | 'unusual_return';

export interface StockAnomaly {
  id: string;
  articleId: number;
  articleName: string;
  articleNumber: string;
  severity: AnomalySeverity;
  type: AnomalyType;
  title: string;
  description: string;
  affectedTransactions: number[];
  detectedValue: string | number;
  expectedRange: string;
  recommendedAction: string;
  detectedAt: string;
}

export interface AnomalySummary {
  totalAnomalies: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  mostAffectedArticle: string | null;
  overallRiskLevel: 'critical' | 'high' | 'medium' | 'low' | 'none';
  summaryText: string;
}

export interface AnomalyDetectionResponse {
  anomalies: StockAnomaly[];
  summary: AnomalySummary;
}

async function detectAnomalies(
  articles: TransactionData[],
  language: string
): Promise<AnomalyDetectionResponse> {
  if (!supabaseUrl) {
    throw new Error("Cloud not configured");
  }
  
  return invokeEdgeFunction<AnomalyDetectionResponse>('stock-anomaly-detection', {
    articles,
    language
  });
}

export function useAnomalyDetection() {
  const { i18n, t } = useTranslation('stock-management');
  const [anomalyData, setAnomalyData] = useState<AnomalyDetectionResponse | null>(null);

  const mutation = useMutation({
    mutationFn: (articles: TransactionData[]) =>
      detectAnomalies(articles, i18n.language),
    onSuccess: (data) => {
      setAnomalyData(data);
      if (data.summary.totalAnomalies > 0) {
        if (data.summary.criticalCount > 0) {
          toast.error(t('anomaly.found_critical', { count: data.summary.criticalCount }));
        } else if (data.summary.highCount > 0) {
          toast.warning(t('anomaly.found_high', { count: data.summary.highCount }));
        } else {
          toast.info(t('anomaly.found', { count: data.summary.totalAnomalies }));
        }
      } else {
        toast.success(t('anomaly.none_found'));
      }
    },
    onError: (error: Error) => {
      console.error('Anomaly detection error:', error);
      if (error.message.includes('Rate limit')) {
        toast.error(t('anomaly.rate_limit'));
      } else if (error.message.includes('Credits')) {
        toast.error(t('anomaly.credits_exhausted'));
      } else {
        toast.error(t('anomaly.error'));
      }
    },
  });

  return {
    anomalyData,
    isLoading: mutation.isPending,
    error: mutation.error,
    detectAnomalies: mutation.mutate,
    clearAnomalies: () => setAnomalyData(null),
  };
}
