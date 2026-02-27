// Supabase client for edge function calls
// This project uses Lovable Cloud for edge functions

export const supabaseUrl = import.meta.env.VITE_SUPABASE_URL;
export const supabaseKey = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY;

// Helper to call edge functions
export async function invokeEdgeFunction<T = unknown>(
  functionName: string,
  body: unknown
): Promise<T> {
  if (!supabaseUrl) {
    throw new Error("Supabase URL is not configured. Please ensure Lovable Cloud is enabled.");
  }

  const response = await fetch(`${supabaseUrl}/functions/v1/${functionName}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(supabaseKey && { Authorization: `Bearer ${supabaseKey}` }),
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: `HTTP ${response.status}` }));
    throw new Error(error.error || `Request failed with status ${response.status}`);
  }

  return response.json();
}
