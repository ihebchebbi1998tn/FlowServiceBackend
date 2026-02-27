import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, x-supabase-client-platform, x-supabase-client-platform-version, x-supabase-client-runtime, x-supabase-client-runtime-version",
};

const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

// ─── Multi-key fallback pool ──────────────────────────────────────────────────
// Keys tried in order; blacklisted on spend-limit, cooled on rate-limit.
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
  transactions: {
    transactionType: string;
    quantity: number;
    createdAt: string;
    referenceType?: string;
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

      console.log(`Forecast: trying ${model.split("/").pop()} with key ${key.slice(0, 18)}…`);
      try {
        const resp = await fetch(OPENROUTER_URL, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${key}`,
            "Content-Type": "application/json",
            "HTTP-Referer": "https://lovable.dev",
            "X-Title": "Stock Demand Forecast",
          },
          body: JSON.stringify({
            model,
            messages: [
              { role: "system", content: systemPrompt },
              { role: "user", content: userPrompt },
            ],
            temperature: 0.3,
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
          console.warn(`HTTP ${resp.status} from ${model}: ${errorText.slice(0, 100)}`);
          continue;
        }

        const data = await resp.json();
        const content = data.choices?.[0]?.message?.content;
        if (!content) {
          console.warn(`Empty content from ${model}`);
          continue;
        }

        console.log(`✅ Forecast success — ${model.split("/").pop()}`);
        return { content };
      } catch (err) {
        const msg = err instanceof Error ? err.message : "Network error";
        console.warn(`Fetch error: ${msg}`);
        coolingUntil.set(key, Date.now() + 5000);
        continue;
      }
    }
  }

  return { content: "", error: "All AI models and keys exhausted for demand forecast." };
}

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const primaryKey = Deno.env.get("OPENROUTER_API_KEY");

    const { articles, language = "en" }: { articles: TransactionData[]; language?: string } = await req.json();

    if (!articles || articles.length === 0) {
      return new Response(JSON.stringify({ error: "No articles provided" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    console.log(`Processing demand forecast for ${articles.length} articles`);

    const systemPrompt = `You are an inventory demand forecasting expert. Analyze stock transaction history to predict future demand and provide actionable recommendations.

You will analyze transaction patterns including:
- Sale deductions (sale_deduction)
- Manual removals (remove)
- Additions (add)
- Returns (return)
- Adjustments (adjustment)

Focus on CONSUMPTION patterns (sales, removals) to predict demand.

Return a JSON object with forecasts for each article. Consider:
1. Seasonal patterns
2. Trend direction (increasing/decreasing/stable demand)
3. Average consumption rate
4. Stock criticality vs minimum stock level
5. Lead time assumptions (assume 7 days for reorder)

Language for insights: ${language === 'fr' ? 'French' : 'English'}`;

    const articlesData = articles.map(a => ({
      id: a.articleId,
      name: a.articleName,
      sku: a.articleNumber,
      currentStock: a.currentStock,
      minStock: a.minStock,
      transactions: a.transactions.slice(0, 50).map(t => ({
        type: t.transactionType,
        qty: t.quantity,
        date: t.createdAt,
        ref: t.referenceType,
      })),
    }));

    const userPrompt = `Analyze demand for these articles and return forecasts:

${JSON.stringify(articlesData, null, 2)}

Return ONLY a valid JSON object with this structure:
{
  "forecasts": [
    {
      "articleId": number,
      "articleName": "string",
      "articleNumber": "string",
      "currentStock": number,
      "minStock": number,
      "predictions": {
        "period": "next_30_days",
        "predictedDemand": number,
        "confidence": 0.0-1.0,
        "trend": "increasing" | "decreasing" | "stable"
      },
      "recommendations": {
        "reorderPoint": number,
        "suggestedOrderQuantity": number,
        "urgency": "critical" | "high" | "medium" | "low",
        "reasoning": "brief explanation"
      },
      "insights": ["key insight 1", "key insight 2"]
    }
  ],
  "summary": {
    "criticalItems": number,
    "totalPredictedDemand": number,
    "highestDemandItem": "article name",
    "overallTrend": "brief market summary"
  }
}`;

    const { content, error } = await callAIWithFallback(systemPrompt, userPrompt, primaryKey);

    if (!content) {
      return new Response(JSON.stringify({ error: error || "AI unavailable" }), {
        status: 503,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    let forecastData;
    try {
      const jsonMatch = content.match(/\{[\s\S]*\}/);
      forecastData = JSON.parse(jsonMatch ? jsonMatch[0] : content);
    } catch (parseError) {
      console.error("Failed to parse AI response:", parseError);
      throw new Error("Failed to parse AI forecast response");
    }

    return new Response(JSON.stringify(forecastData), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });

  } catch (error) {
    console.error("Demand forecast error:", error);
    const errorMessage = error instanceof Error ? error.message : "Unknown error";
    return new Response(JSON.stringify({ error: errorMessage }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
