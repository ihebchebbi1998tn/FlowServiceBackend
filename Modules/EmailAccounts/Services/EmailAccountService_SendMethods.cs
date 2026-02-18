using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyApi.Modules.EmailAccounts.DTOs;
using MyApi.Modules.EmailAccounts.Models;

namespace MyApi.Modules.EmailAccounts.Services
{
    public partial class EmailAccountService
    {

public async Task<SendEmailResultDto> SendEmailAsync(Guid accountId, int userId, SendEmailDto dto)
{
    var account = await _context.ConnectedEmailAccounts
        .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

    if (account == null)
        return new SendEmailResultDto { Success = false, Error = "Account not found" };

    try
    {
        var result = account.Provider switch
        {
            "google" => await SendGmailEmailAsync(account, dto),
            "microsoft" => await SendOutlookEmailAsync(account, dto),
            _ => new SendEmailResultDto { Success = false, Error = $"Unsupported provider: {account.Provider}" }
        };

        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send email via {Provider} for account {Handle}", account.Provider, account.Handle);
        return new SendEmailResultDto { Success = false, Error = ex.Message };
    }
}

private async Task<SendEmailResultDto> SendGmailEmailAsync(ConnectedEmailAccount account, SendEmailDto dto)
{
    var client = _httpClientFactory.CreateClient();
    var accessToken = await EnsureValidGoogleTokenAsync(account);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var hasAttachments = dto.Attachments != null && dto.Attachments.Count > 0;
    string rawMessage;

    if (hasAttachments)
    {
        // Build multipart MIME with attachments
        var boundary = $"boundary_{Guid.NewGuid():N}";
        var mime = new StringBuilder();
        mime.AppendLine($"From: {account.Handle}");
        mime.AppendLine($"To: {string.Join(", ", dto.To)}");
        if (dto.Cc.Count > 0) mime.AppendLine($"Cc: {string.Join(", ", dto.Cc)}");
        if (dto.Bcc.Count > 0) mime.AppendLine($"Bcc: {string.Join(", ", dto.Bcc)}");
        mime.AppendLine($"Subject: {dto.Subject}");
        mime.AppendLine("MIME-Version: 1.0");
        mime.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        mime.AppendLine();

        // Body part
        mime.AppendLine($"--{boundary}");
        if (!string.IsNullOrEmpty(dto.BodyHtml))
        {
            mime.AppendLine("Content-Type: text/html; charset=UTF-8");
            mime.AppendLine();
            mime.AppendLine(dto.BodyHtml);
        }
        else
        {
            mime.AppendLine("Content-Type: text/plain; charset=UTF-8");
            mime.AppendLine();
            mime.AppendLine(dto.Body);
        }

        // Attachment parts
        foreach (var att in dto.Attachments!)
        {
            mime.AppendLine($"--{boundary}");
            mime.AppendLine($"Content-Type: {att.ContentType}; name=\"{att.FileName}\"");
            mime.AppendLine("Content-Transfer-Encoding: base64");
            mime.AppendLine($"Content-Disposition: attachment; filename=\"{att.FileName}\"");
            mime.AppendLine();
            // Insert base64 content in 76-char lines
            var b64 = att.ContentBase64;
            for (int i = 0; i < b64.Length; i += 76)
            {
                mime.AppendLine(b64.Substring(i, Math.Min(76, b64.Length - i)));
            }
        }
        mime.AppendLine($"--{boundary}--");

        rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime.ToString()))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }
    else
    {
        // Simple plain-text email (original logic)
        var mime = new StringBuilder();
        mime.AppendLine($"From: {account.Handle}");
        mime.AppendLine($"To: {string.Join(", ", dto.To)}");
        if (dto.Cc.Count > 0) mime.AppendLine($"Cc: {string.Join(", ", dto.Cc)}");
        if (dto.Bcc.Count > 0) mime.AppendLine($"Bcc: {string.Join(", ", dto.Bcc)}");
        mime.AppendLine($"Subject: {dto.Subject}");
        if (!string.IsNullOrEmpty(dto.BodyHtml))
        {
            mime.AppendLine("Content-Type: text/html; charset=UTF-8");
            mime.AppendLine();
            mime.AppendLine(dto.BodyHtml);
        }
        else
        {
            mime.AppendLine("Content-Type: text/plain; charset=UTF-8");
            mime.AppendLine();
            mime.AppendLine(dto.Body);
        }

        rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime.ToString()))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    var jsonBody = JsonSerializer.Serialize(new { raw = rawMessage });
    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

    var response = await client.PostAsync("https://gmail.googleapis.com/gmail/v1/users/me/messages/send", content);

    if (!response.IsSuccessStatusCode)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        _logger.LogError("Gmail send failed: {Status} - {Body}", response.StatusCode, errorBody);
        return new SendEmailResultDto { Success = false, Error = $"Gmail send failed: {response.StatusCode}" };
    }

    var responseJson = await response.Content.ReadAsStringAsync();
    var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);
    var messageId = responseData.TryGetProperty("id", out var id) ? id.GetString() : null;

    return new SendEmailResultDto { Success = true, MessageId = messageId };
}

private async Task<SendEmailResultDto> SendOutlookEmailAsync(ConnectedEmailAccount account, SendEmailDto dto)
{
    var client = _httpClientFactory.CreateClient();
    var accessToken = await EnsureValidMicrosoftTokenAsync(account);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var toRecipients = dto.To.Select(e => new { emailAddress = new { address = e } }).ToList();
    var ccRecipients = dto.Cc.Select(e => new { emailAddress = new { address = e } }).ToList();
    var bccRecipients = dto.Bcc.Select(e => new { emailAddress = new { address = e } }).ToList();

    var bodyContentType = !string.IsNullOrEmpty(dto.BodyHtml) ? "HTML" : "Text";
    var bodyContent = !string.IsNullOrEmpty(dto.BodyHtml) ? dto.BodyHtml : dto.Body;

    var attachments = dto.Attachments?.Select(a => new
    {
        @odata_type = "#microsoft.graph.fileAttachment",
        name = a.FileName,
        contentType = a.ContentType,
        contentBytes = a.ContentBase64
    }).ToList();

    object emailPayload;
    if (attachments != null && attachments.Count > 0)
    {
        emailPayload = new
        {
            message = new
            {
                subject = dto.Subject,
                body = new
                {
                    contentType = bodyContentType,
                    content = bodyContent
                },
                toRecipients,
                ccRecipients,
                bccRecipients,
                attachments = attachments.Select(a => new Dictionary<string, object>
                {
                    { "@odata.type", "#microsoft.graph.fileAttachment" },
                    { "name", a.name },
                    { "contentType", a.contentType },
                    { "contentBytes", a.contentBytes }
                }).ToList()
            },
            saveToSentItems = true
        };
    }
    else
    {
        emailPayload = new
        {
            message = new
            {
                subject = dto.Subject,
                body = new
                {
                    contentType = bodyContentType,
                    content = bodyContent
                },
                toRecipients,
                ccRecipients,
                bccRecipients
            },
            saveToSentItems = true
        };
    }

    var jsonBody = JsonSerializer.Serialize(emailPayload);
    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

    var response = await client.PostAsync("https://graph.microsoft.com/v1.0/me/sendMail", content);

    if (!response.IsSuccessStatusCode)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        _logger.LogError("Outlook send failed: {Status} - {Body}", response.StatusCode, errorBody);
        return new SendEmailResultDto { Success = false, Error = $"Outlook send failed: {response.StatusCode}" };
    }

    // Microsoft sendMail returns 202 Accepted with no body
    return new SendEmailResultDto { Success = true };
    }
}
}
