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
      return new Response(JSON.stringify({ anomalies: [], summary: null }), {
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

    const response = await fetch(OPENROUTER_URL, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${OPENROUTER_API_KEY}`,
        "Content-Type": "application/json",
        "HTTP-Referer": "https://lovable.dev",
        "X-Title": "Stock Anomaly Detection",
      },
      body: JSON.stringify({
        model: "google/gemini-2.5-flash-preview-05-20",
        messages: [
          { role: "system", content: systemPrompt },
          { role: "user", content: userPrompt },
        ],
        temperature: 0.2,
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

    console.log("AI anomaly detection response received");

    let anomalyData;
    try {
      const jsonMatch = content.match(/\{[\s\S]*\}/);
      if (jsonMatch) {
        anomalyData = JSON.parse(jsonMatch[0]);
      } else {
        anomalyData = JSON.parse(content);
      }
    } catch (parseError) {
      console.error("Failed to parse AI response:", parseError);
      console.error("Raw content:", content);
      anomalyData = { 
        anomalies: [], 
        summary: { 
          totalAnomalies: 0, 
          criticalCount: 0, 
          highCount: 0, 
          mediumCount: 0, 
          lowCount: 0,
          mostAffectedArticle: null,
          overallRiskLevel: "none",
          summaryText: "Analysis completed but no anomalies detected."
        } 
      };
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
