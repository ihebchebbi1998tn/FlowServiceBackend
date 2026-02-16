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
    const OPENROUTER_API_KEY = Deno.env.get('OPENROUTER_API_KEY');
    const FALLBACK_API_KEY = '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';
    
    if (!OPENROUTER_API_KEY && !FALLBACK_API_KEY) {
      throw new Error('No API keys configured');
    }

    const { form_fields, entity_type, entity_fields, language }: MappingRequest = await req.json();

    if (!form_fields || !entity_type || !entity_fields) {
      return new Response(JSON.stringify({ error: 'Missing required fields' }), {
        status: 400,
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
      });
    }

    console.log(`AI Field Mapping: ${form_fields.length} form fields -> ${entity_type} (${entity_fields.length} entity fields)`);

    // Build the prompt for AI
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

    // Try primary key first, then fallback
    const apiKeys = OPENROUTER_API_KEY ? [OPENROUTER_API_KEY, FALLBACK_API_KEY] : [FALLBACK_API_KEY];
    let lastError: Error | null = null;
    let aiResponse: any = null;

    for (const apiKey of apiKeys) {
      try {
        console.log(`Trying API key: ${apiKey.substring(0, 15)}...`);
        
        const response = await fetch('https://openrouter.ai/api/v1/chat/completions', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${apiKey}`,
            'Content-Type': 'application/json',
            'HTTP-Referer': 'https://lovable.dev',
            'X-Title': 'Lovable Dynamic Forms',
          },
          body: JSON.stringify({
            model: 'google/gemini-2.5-flash-preview-05-20',
            messages: [
              { role: 'system', content: systemPrompt },
              { role: 'user', content: userPrompt },
            ],
            temperature: 0.3,
            max_tokens: 2000,
          }),
        });

        if (!response.ok) {
          const errorText = await response.text();
          console.error(`API key failed (${response.status}):`, errorText);
          lastError = new Error(`OpenRouter API error: ${response.status}`);
          continue; // Try next key
        }

        aiResponse = await response.json();
        console.log('API call succeeded');
        break; // Success, exit loop
        
      } catch (err) {
        console.error('API call error:', err);
        lastError = err instanceof Error ? err : new Error('Unknown API error');
        continue; // Try next key
      }
    }

    if (!aiResponse) {
      throw lastError || new Error('All API keys failed');
    }

    const content = aiResponse.choices?.[0]?.message?.content;

    if (!content) {
      throw new Error('No content in AI response');
    }

    console.log('AI response received:', content.substring(0, 200));

    // Parse the AI response - extract JSON from potential markdown
    let mappings: FieldMapping[] = [];
    try {
      // Try to extract JSON from the response
      const jsonMatch = content.match(/\[[\s\S]*\]/);
      if (jsonMatch) {
        mappings = JSON.parse(jsonMatch[0]);
      } else {
        // Try parsing the whole content as JSON
        mappings = JSON.parse(content);
      }
    } catch (parseError) {
      console.error('Failed to parse AI response:', parseError);
      console.error('Raw content:', content);
      // Return empty mappings if parsing fails
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
