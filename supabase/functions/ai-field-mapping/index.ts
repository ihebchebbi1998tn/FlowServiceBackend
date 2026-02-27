import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type, x-supabase-client-platform, x-supabase-client-platform-version, x-supabase-client-runtime, x-supabase-client-runtime-version',
};

interface FormField {
  id: string;
  type: string;
  label_en: string;
  label_fr: string;
  description_en?: string;
  description_fr?: string;
}

interface EntityField {
  field: string;
  label_en: string;
  label_fr: string;
  required: boolean;
  type: string;
}

interface MappingRequest {
  form_fields: FormField[];
  entity_type: string;
  entity_fields: EntityField[];
  language: 'en' | 'fr';
}

interface FieldMapping {
  form_field_id: string;
  entity_field: string;
  confidence: number;
  reason: string;
}

serve(async (req) => {
  // Handle CORS preflight requests
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders });
  }

  try {
    const primaryKey = Deno.env.get('OPENROUTER_API_KEY');

    // ─── Multi-key fallback pool ──────────────────────────────────────────────
    const FALLBACK_KEYS: string[] = [
      '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',
    ];
    const MODEL_ROTATION: string[] = [
      'arcee-ai/trinity-large-preview:free',
      'sourceful/riverflow-v2-pro',
      'google/gemini-2.5-flash-preview-05-20',
      'meta-llama/llama-3.3-70b-instruct',
    ];

    const keyPool = primaryKey
      ? [primaryKey, ...FALLBACK_KEYS.filter(k => k !== primaryKey)]
      : FALLBACK_KEYS;

    const { form_fields, entity_type, entity_fields, language }: MappingRequest = await req.json();

    if (!form_fields || !entity_type || !entity_fields) {
      return new Response(JSON.stringify({ error: 'Missing required fields' }), {
        status: 400,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    console.log(`AI Field Mapping: ${form_fields.length} form fields -> ${entity_type} (${entity_fields.length} entity fields)`);

    const systemPrompt = `You are an intelligent data mapping assistant. Your task is to analyze form field definitions and map them to entity fields for data export.

You must understand the SEMANTIC meaning of fields, not just keyword matching. Consider:
- Field names and labels in both English and French
- Field descriptions and context
- Data types and validation rules
- Business logic and common patterns

Entity type being exported to: ${entity_type}

Available entity fields:
${entity_fields.map(f => `- ${f.field} (${f.label_en} / ${f.label_fr}) [${f.type}]${f.required ? ' REQUIRED' : ''}`).join('\n')}

Return a JSON array of mappings. Only map fields that have a clear semantic match. For each mapping include:
- form_field_id: the ID of the form field
- entity_field: the matched entity field name
- confidence: 0.0 to 1.0 (how confident you are in this mapping)
- reason: brief explanation of why this mapping makes sense

IMPORTANT:
- Required entity fields should be prioritized
- Don't force mappings - only map when there's genuine semantic similarity
- Consider field types (don't map a signature to a number field)
- Name/Email/Phone are common patterns to look for
- For contacts: look for name, email, phone, company, address patterns
- For articles: look for name, description, price, SKU, stock patterns
- For installations: look for name, address, model, serial number patterns`;

    const userPrompt = `Analyze these form fields and suggest mappings to ${entity_type} fields:

Form Fields:
${form_fields.map(f => {
  const label = language === 'en' ? f.label_en : f.label_fr;
  const desc = language === 'en' ? f.description_en : f.description_fr;
  return `- ID: "${f.id}", Type: ${f.type}, Label: "${label}"${desc ? `, Description: "${desc}"` : ''}`;
}).join('\n')}

Return ONLY valid JSON array, no markdown or explanation outside the JSON.`;

    // ─── Try every model × key combination ───────────────────────────────────
    const blacklisted = new Set<string>();
    const coolingUntil = new Map<string, number>();
    let aiContent: string | null = null;

    outer:
    for (const model of MODEL_ROTATION) {
      for (const key of keyPool) {
        if (blacklisted.has(key)) continue;
        const cool = coolingUntil.get(key) ?? 0;
        if (cool > Date.now()) continue;

        console.log(`Field mapping: trying ${model.split('/').pop()} with key ${key.slice(0, 18)}…`);
        try {
          const resp = await fetch('https://openrouter.ai/api/v1/chat/completions', {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${key}`,
              'Content-Type': 'application/json',
              'HTTP-Referer': 'https://lovable.dev',
              'X-Title': 'Lovable Dynamic Forms',
            },
            body: JSON.stringify({
              model,
              messages: [
                { role: 'system', content: systemPrompt },
                { role: 'user', content: userPrompt },
              ],
              temperature: 0.3,
              max_tokens: 2000,
            }),
          });

          if (!resp.ok) {
            const errorText = await resp.text();
            if (resp.status === 402 || errorText.toLowerCase().includes('spend limit')) {
              blacklisted.add(key);
              continue;
            }
            if (resp.status === 429) {
              const ra = parseInt(resp.headers.get('retry-after') || '60', 10);
              coolingUntil.set(key, Date.now() + ra * 1000);
              continue;
            }
            if (resp.status === 503) break; // try next model
            continue;
          }

          const data = await resp.json();
          const content = data.choices?.[0]?.message?.content;
          if (content) {
            aiContent = content;
            console.log(`✅ Field mapping success — ${model.split('/').pop()}`);
            break outer;
          }
        } catch (err) {
          console.warn('Fetch error:', err);
          coolingUntil.set(key, Date.now() + 5000);
          continue;
        }
      }
    }

    if (!aiContent) {
      console.warn('All AI options exhausted for field mapping — returning empty mappings');
      return new Response(JSON.stringify({ mappings: [], total_form_fields: form_fields.length, mapped_count: 0 }), {
        status: 200,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    console.log('AI response received:', aiContent.substring(0, 200));

    // Parse the AI response - extract JSON from potential markdown
    let mappings: FieldMapping[] = [];
    try {
      const jsonMatch = aiContent.match(/\[[\s\S]*\]/);
      if (jsonMatch) {
        mappings = JSON.parse(jsonMatch[0]);
      } else {
        mappings = JSON.parse(aiContent);
      }
    } catch (parseError) {
      console.error('Failed to parse AI response:', parseError);
      mappings = [];
    }

    // Validate and filter mappings
    const validMappings = mappings.filter((m: any) => {
      return (
        m.form_field_id &&
        m.entity_field &&
        form_fields.some(f => f.id === m.form_field_id) &&
        entity_fields.some(f => f.field === m.entity_field)
      );
    });

    console.log(`AI suggested ${validMappings.length} valid mappings`);

    return new Response(JSON.stringify({ 
      mappings: validMappings,
      total_form_fields: form_fields.length,
      mapped_count: validMappings.length,
    }), {
      status: 200,
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
    });

  } catch (error: unknown) {
    console.error('AI Field Mapping error:', error);
    const errorMessage = error instanceof Error ? error.message : 'Unknown error';
    return new Response(JSON.stringify({ error: errorMessage, mappings: [] }), {
      status: 500,
      headers: { ...corsHeaders, 'Content-Type': 'application/json' },
    });
  }
});
