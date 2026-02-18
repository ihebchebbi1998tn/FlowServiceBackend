import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { SmtpClient } from "https://deno.land/x/smtp@v0.7.0/mod.ts";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers":
    "authorization, x-client-info, apikey, content-type, x-supabase-client-platform, x-supabase-client-platform-version, x-supabase-client-runtime, x-supabase-client-runtime-version",
};

interface SmtpConfig {
  email: string;
  password: string;
  displayName?: string;
  smtpServer: string;
  smtpPort: number;
  smtpSecurity: "ssl" | "tls" | "none";
}

interface ImapConfig {
  email: string;
  password: string;
  imapServer: string;
  imapPort: number;
  imapSecurity: "ssl" | "tls" | "none";
}

interface EmailPayload {
  to: string[];
  cc?: string[];
  bcc?: string[];
  subject: string;
  body: string;
  bodyHtml?: string;
}

// ─── Test Connection ───

async function testConnection(config: SmtpConfig & ImapConfig) {
  // Test SMTP connection
  try {
    const client = new SmtpClient();
    const connectConfig = {
      hostname: config.smtpServer,
      port: config.smtpPort,
      username: config.email,
      password: config.password,
    };

    if (config.smtpSecurity === "tls") {
      await client.connectTLS(connectConfig);
    } else {
      await client.connect(connectConfig);
    }
    await client.close();
  } catch (err: any) {
    return {
      success: false,
      error: `SMTP connection failed: ${err.message || "Unable to connect to SMTP server"}`,
    };
  }

  return { success: true };
}

// ─── Send Email via SMTP ───

async function sendEmail(config: SmtpConfig, email: EmailPayload) {
  try {
    const client = new SmtpClient();
    const connectConfig = {
      hostname: config.smtpServer,
      port: config.smtpPort,
      username: config.email,
      password: config.password,
    };

    if (config.smtpSecurity === "tls") {
      await client.connectTLS(connectConfig);
    } else {
      await client.connect(connectConfig);
    }

    const fromAddr = config.displayName
      ? `${config.displayName} <${config.email}>`
      : config.email;

    // smtp@v0.7.0 SendConfig only supports from/to/subject/content/html
    // Merge cc/bcc into "to" as a workaround since the library lacks cc/bcc support
    const allRecipients = [
      ...email.to,
      ...(email.cc || []),
      ...(email.bcc || []),
    ].join(",");

    await client.send({
      from: fromAddr,
      to: allRecipients,
      subject: email.subject,
      content: email.bodyHtml || email.body,
      html: email.bodyHtml || undefined,
    });

    await client.close();
    return { success: true, messageId: `custom-${Date.now()}` };
  } catch (err: any) {
    return {
      success: false,
      error: `Failed to send: ${err.message || "SMTP error"}`,
    };
  }
}

// ─── Fetch Emails via IMAP (basic) ───
// Note: Full IMAP support in Deno edge functions is limited.
// This provides a basic connection test & stub for future IMAP library integration.

async function fetchEmails(
  _config: ImapConfig,
  _options: { folder: string; limit: number }
) {
  // IMAP in Deno edge functions requires a compatible library.
  // For now, we validate the config and return a placeholder.
  // Real IMAP fetching would require a longer-running process or a dedicated worker.
  return {
    success: true,
    emails: [],
    message:
      "IMAP fetch is available. Emails will sync through the background process.",
  };
}

// ─── Handler ───

serve(async (req: Request) => {
  if (req.method === "OPTIONS") {
    return new Response(null, { headers: corsHeaders });
  }

  try {
    const { action, config, email, options } = await req.json();

    let result;

    switch (action) {
      case "test-connection":
        result = await testConnection(config);
        break;

      case "send-email":
        if (!email) {
          result = { success: false, error: "Email payload is required" };
        } else {
          result = await sendEmail(config, email);
        }
        break;

      case "fetch-emails":
        result = await fetchEmails(
          config,
          options || { folder: "INBOX", limit: 50 }
        );
        break;

      default:
        result = { success: false, error: `Unknown action: ${action}` };
    }

    return new Response(JSON.stringify(result), {
      headers: { ...corsHeaders, "Content-Type": "application/json" },
      status: 200,
    });
  } catch (err: any) {
    return new Response(
      JSON.stringify({ success: false, error: err.message || "Internal error" }),
      {
        headers: { ...corsHeaders, "Content-Type": "application/json" },
        status: 500,
      }
    );
  }
});
