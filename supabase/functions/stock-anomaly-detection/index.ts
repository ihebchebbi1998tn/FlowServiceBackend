import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, x-supabase-client-platform, x-supabase-client-platform-version, x-supabase-client-runtime, x-supabase-client-runtime-version",
};

const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

// ─── Multi-key fallback pool ──────────────────────────────────────────────────
const FALLBACK_KEYS: string[] = [
  "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",
];

const MODEL_ROTATION: string[] = [
  "arcee-ai/trinity-large-preview:free",
  "sourceful/riverflow-v2-pro",
  "google/gemini-2.5-flash-preview-05-20",
  "meta-llama/llama-3.3-70b-instruct",
];

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

async function callAIWithFallback(
  systemPrompt: string,
  userPrompt: string,
  primaryKey: string | undefined,
): Promise<{ content: string; error?: string }> {
  const keyPool = primaryKey
    ? [primaryKey, ...FALLBACK_KEYS.filter(k => k !== primaryKey)]
    : FALLBACK_KEYS;

  const blacklisted = new Set<string>();
  const coolingUntil = new Map<string, number>();

  for (const model of MODEL_ROTATION) {
    for (const key of keyPool) {
      if (blacklisted.has(key)) continue;
      const cool = coolingUntil.get(key) ?? 0;
      if (cool > Date.now()) {
        console.log(`Key ${key.slice(0, 18)}… cooling, skipping`);
        continue;
      }

      console.log(`Anomaly: trying ${model.split("/").pop()} with key ${key.slice(0, 18)}…`);
      try {
        const resp = await fetch(OPENROUTER_URL, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${key}`,
            "Content-Type": "application/json",
            "HTTP-Referer": "https://lovable.dev",
            "X-Title": "Stock Anomaly Detection",
          },
          body: JSON.stringify({
            model,
            messages: [
              { role: "system", content: systemPrompt },
              { role: "user", content: userPrompt },
            ],
            temperature: 0.2,
            max_tokens: 4000,
          }),
        });

        if (!resp.ok) {
          const errorText = await resp.text();
          if (resp.status === 402 || errorText.toLowerCase().includes("spend limit")) {
            console.warn(`Key ${key.slice(0, 18)}… BLACKLISTED (spend limit)`);
            blacklisted.add(key);
            continue;
          }
          if (resp.status === 429) {
            const retryAfter = parseInt(resp.headers.get("retry-after") || "60", 10);
            console.warn(`Key ${key.slice(0, 18)}… COOLING ${retryAfter}s (rate limit)`);
            coolingUntil.set(key, Date.now() + retryAfter * 1000);
            continue;
          }
          if (resp.status === 503) {
            console.warn(`Model ${model} unavailable, trying next model`);
            break;
          }
          console.warn(`HTTP ${resp.status}: ${errorText.slice(0, 100)}`);
          continue;
        }

        const data = await resp.json();
        const content = data.choices?.[0]?.message?.content;
        if (!content) {
          console.warn(`Empty content from ${model}`);
          continue;
        }

        console.log(`✅ Anomaly detection success — ${model.split("/").pop()}`);
        return { content };
      } catch (err) {
        const msg = err instanceof Error ? err.message : "Network error";
        console.warn(`Fetch error: ${msg}`);
        coolingUntil.set(key, Date.now() + 5000);
        continue;
      }
    }
  }

  return { content: "", error: "All AI models and keys exhausted for anomaly detection." };
}

const EMPTY_RESULT = {
  anomalies: [],
  summary: {
    totalAnomalies: 0,
    criticalCount: 0,
    highCount: 0,
    mediumCount: 0,
    lowCount: 0,
    mostAffectedArticle: null,
    overallRiskLevel: "none",
    summaryText: "Analysis completed — no anomalies detected.",
  },
};

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const primaryKey = Deno.env.get("OPENROUTER_API_KEY");

    const { articles, language = "en" }: { articles: TransactionData[]; language?: string } = await req.json();

    if (!articles || articles.length === 0) {
      return new Response(JSON.stringify(EMPTY_RESULT), {
        status: 200,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    console.log(`Analyzing anomalies for ${articles.length} articles`);

    const systemPrompt = `You are a stock inventory anomaly detection expert. Analyze transaction patterns to identify unusual or suspicious activities.

Look for these types of anomalies:
1. **Volume Anomalies**: Unusually large single transactions (3x+ normal)
2. **Frequency Anomalies**: Sudden increase/decrease in transaction frequency
3. **Pattern Breaks**: Transactions outside normal business hours or days
4. **Suspicious Removals**: Large manual removals without clear reference
5. **Stock Discrepancies**: Adjustments that seem unusual
6. **Rapid Depletion**: Stock dropping faster than historical average
7. **Unusual Returns**: Higher than normal return rates

For each anomaly found, provide:
- Severity: critical (requires immediate action), high (investigate soon), medium (monitor), low (informational)
- Clear description of what was detected
- The specific transaction(s) involved
- Recommended action

Language for descriptions: ${language === 'fr' ? 'French' : 'English'}`;

    const articlesData = articles.map(a => ({
      id: a.articleId,
      name: a.articleName,
      sku: a.articleNumber,
      currentStock: a.currentStock,
      minStock: a.minStock,
      avgDailyUsage: a.avgDailyUsage,
      recentTransactions: a.transactions.slice(0, 30).map(t => ({
        id: t.id,
        type: t.transactionType,
        qty: t.quantity,
        date: t.createdAt,
        ref: t.referenceType,
        by: t.performedByName,
      })),
    }));

    const userPrompt = `Analyze these stock transactions for anomalies:

${JSON.stringify(articlesData, null, 2)}

Return ONLY a valid JSON object with this structure:
{
  "anomalies": [
    {
      "id": "unique_anomaly_id",
      "articleId": number,
      "articleName": "string",
      "articleNumber": "string",
      "severity": "critical" | "high" | "medium" | "low",
      "type": "volume" | "frequency" | "pattern" | "suspicious_removal" | "discrepancy" | "rapid_depletion" | "unusual_return",
      "title": "brief title",
      "description": "detailed description of the anomaly",
      "affectedTransactions": [transaction_ids],
      "detectedValue": number or string (the anomalous value),
      "expectedRange": "what was expected",
      "recommendedAction": "what to do",
      "detectedAt": "ISO date string"
    }
  ],
  "summary": {
    "totalAnomalies": number,
    "criticalCount": number,
    "highCount": number,
    "mediumCount": number,
    "lowCount": number,
    "mostAffectedArticle": "article name or null",
    "overallRiskLevel": "critical" | "high" | "medium" | "low" | "none",
    "summaryText": "brief overall assessment"
  }
}

If no anomalies are found, return empty anomalies array with summary showing zeros and "none" risk level.`;

    const { content, error } = await callAIWithFallback(systemPrompt, userPrompt, primaryKey);

    if (!content) {
      console.warn("All AI options exhausted — returning empty result:", error);
      return new Response(JSON.stringify(EMPTY_RESULT), {
        status: 200,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    let anomalyData;
    try {
      const jsonMatch = content.match(/\{[\s\S]*\}/);
      anomalyData = JSON.parse(jsonMatch ? jsonMatch[0] : content);
    } catch (parseError) {
      console.error("Failed to parse AI response:", parseError);
      anomalyData = EMPTY_RESULT;
    }

    return new Response(JSON.stringify(anomalyData), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });

  } catch (error) {
    console.error("Anomaly detection error:", error);
    const errorMessage = error instanceof Error ? error.message : "Unknown error";
    return new Response(JSON.stringify({ error: errorMessage }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
