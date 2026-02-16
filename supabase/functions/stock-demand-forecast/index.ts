import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type, x-supabase-client-platform, x-supabase-client-platform-version, x-supabase-client-runtime, x-supabase-client-runtime-version",
};

const OPENROUTER_URL = "https://openrouter.ai/api/v1/chat/completions";

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

interface ForecastResult {
  articleId: number;
  articleName: string;
  articleNumber: string;
  currentStock: number;
  minStock: number;
  predictions: {
    period: string;
    predictedDemand: number;
    confidence: number;
    trend: "increasing" | "decreasing" | "stable";
  };
  recommendations: {
    reorderPoint: number;
    suggestedOrderQuantity: number;
    urgency: "critical" | "high" | "medium" | "low";
    reasoning: string;
  };
  insights: string[];
}

serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const OPENROUTER_API_KEY = Deno.env.get("OPENROUTER_API_KEY");
    if (!OPENROUTER_API_KEY) {
      throw new Error("OPENROUTER_API_KEY is not configured");
    }

    const { articles, language = "en" }: { articles: TransactionData[]; language?: string } = await req.json();

    if (!articles || articles.length === 0) {
      return new Response(JSON.stringify({ error: "No articles provided" }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    console.log(`Processing demand forecast for ${articles.length} articles`);

    // Build AI prompt with transaction history
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

    const response = await fetch(OPENROUTER_URL, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${OPENROUTER_API_KEY}`,
        "Content-Type": "application/json",
        "HTTP-Referer": "https://lovable.dev",
        "X-Title": "Stock Demand Forecast",
      },
      body: JSON.stringify({
        model: "google/gemini-2.5-flash-preview-05-20",
        messages: [
          { role: "system", content: systemPrompt },
          { role: "user", content: userPrompt },
        ],
        temperature: 0.3,
        max_tokens: 4000,
      }),
    });

    if (!response.ok) {
      if (response.status === 429) {
        return new Response(JSON.stringify({ error: "Rate limit exceeded. Please try again later." }), {
          status: 429,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        });
      }
      if (response.status === 402) {
        return new Response(JSON.stringify({ error: "Credits exhausted. Please add funds to continue." }), {
          status: 402,
          headers: { ...corsHeaders, "Content-Type": "application/json" },
        });
      }
      const errorText = await response.text();
      console.error("AI gateway error:", response.status, errorText);
      throw new Error(`AI gateway error: ${response.status}`);
    }

    const aiResponse = await response.json();
    const content = aiResponse.choices?.[0]?.message?.content;

    if (!content) {
      throw new Error("No content in AI response");
    }

    console.log("AI forecast response received");

    // Parse JSON from response
    let forecastData;
    try {
      const jsonMatch = content.match(/\{[\s\S]*\}/);
      if (jsonMatch) {
        forecastData = JSON.parse(jsonMatch[0]);
      } else {
        forecastData = JSON.parse(content);
      }
    } catch (parseError) {
      console.error("Failed to parse AI response:", parseError);
      console.error("Raw content:", content);
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
