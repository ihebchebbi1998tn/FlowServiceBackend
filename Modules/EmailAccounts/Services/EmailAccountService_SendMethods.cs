// ────────────────────────────────────────────────
// Send Email — ADD these methods INSIDE your existing EmailAccountService class
// ────────────────────────────────────────────────
// Also add to IEmailAccountService:
//   Task<SendEmailResultDto> SendEmailAsync(Guid accountId, int userId, SendEmailDto dto);
// ────────────────────────────────────────────────

using System.Text;
using System.Text.Json;

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

    // Build RFC 2822 MIME message
    var mime = new StringBuilder();
    mime.AppendLine($"From: {account.Handle}");
    mime.AppendLine($"To: {string.Join(", ", dto.To)}");
    if (dto.Cc.Count > 0) mime.AppendLine($"Cc: {string.Join(", ", dto.Cc)}");
    if (dto.Bcc.Count > 0) mime.AppendLine($"Bcc: {string.Join(", ", dto.Bcc)}");
    mime.AppendLine($"Subject: {dto.Subject}");
    mime.AppendLine("Content-Type: text/plain; charset=UTF-8");
    mime.AppendLine();
    mime.AppendLine(dto.Body);

    var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mime.ToString()))
        .Replace('+', '-')
        .Replace('/', '_')
        .Replace("=", "");

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

    var emailPayload = new
    {
        message = new
        {
            subject = dto.Subject,
            body = new
            {
                contentType = "Text",
                content = dto.Body
            },
            toRecipients,
            ccRecipients,
            bccRecipients
        },
        saveToSentItems = true
    };

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
