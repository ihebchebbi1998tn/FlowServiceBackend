// ────────────────────────────────────────────────
// Email Sync & Fetch — ADD these methods to EmailAccountService.cs
// ────────────────────────────────────────────────
// 
// IMPORTANT: Add these methods INSIDE your existing EmailAccountService class.
// Also add `using System.Text.Json;` at the top of the file.
// Also register SyncedEmails in your ApplicationDbContext:
//   public DbSet<SyncedEmail> SyncedEmails { get; set; }
//
// ────────────────────────────────────────────────

/*

// Add this using at the top:
using System.Text.Json;

// Add these methods inside EmailAccountService class:

public async Task<SyncResultDto> SyncEmailsAsync(Guid accountId, int userId, int maxResults = 50)
{
    var account = await _context.ConnectedEmailAccounts
        .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

    if (account == null)
        throw new InvalidOperationException("Account not found");

    if (!account.IsEmailSyncEnabled)
        throw new InvalidOperationException("Email sync is disabled for this account");

    _logger.LogInformation("Starting email sync for account {Handle} ({Provider})", account.Handle, account.Provider);

    var result = account.Provider switch
    {
        "google" => await SyncGmailEmailsAsync(account, maxResults),
        "microsoft" => await SyncOutlookEmailsAsync(account, maxResults),
        _ => throw new InvalidOperationException($"Unsupported provider: {account.Provider}")
    };

    // Update last synced timestamp
    account.LastSyncedAt = DateTime.UtcNow;
    account.SyncStatus = "active";
    account.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return result;
}

private async Task<SyncResultDto> SyncGmailEmailsAsync(ConnectedEmailAccount account, int maxResults)
{
    var client = _httpClientFactory.CreateClient();
    var accessToken = await EnsureValidGoogleTokenAsync(account);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    // 1. List message IDs
    var listUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults={maxResults}&labelIds=INBOX";
    var listResponse = await client.GetAsync(listUrl);

    if (!listResponse.IsSuccessStatusCode)
    {
        _logger.LogError("Gmail list messages failed: {Status}", listResponse.StatusCode);
        account.SyncStatus = "failed";
        await _context.SaveChangesAsync();
        throw new InvalidOperationException("Failed to fetch emails from Gmail");
    }

    var listJson = await listResponse.Content.ReadAsStringAsync();
    var listData = JsonSerializer.Deserialize<JsonElement>(listJson);

    int newCount = 0, updatedCount = 0;

    if (listData.TryGetProperty("messages", out var messages))
    {
        foreach (var msg in messages.EnumerateArray())
        {
            var messageId = msg.GetProperty("id").GetString()!;
            var threadId = msg.TryGetProperty("threadId", out var tid) ? tid.GetString() : null;

            // Check if already synced
            var existing = await _context.Set<SyncedEmail>()
                .FirstOrDefaultAsync(e => e.ConnectedEmailAccountId == account.Id && e.ExternalId == messageId);

            if (existing != null)
            {
                updatedCount++;
                continue;
            }

            // 2. Get full message details
            var detailUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}?format=metadata&metadataHeaders=Subject&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Cc&metadataHeaders=Date";
            var detailResponse = await client.GetAsync(detailUrl);

            if (!detailResponse.IsSuccessStatusCode) continue;

            var detailJson = await detailResponse.Content.ReadAsStringAsync();
            var detail = JsonSerializer.Deserialize<JsonElement>(detailJson);

            var headers = detail.GetProperty("payload").GetProperty("headers");
            var subject = "";
            var from = "";
            var to = "";
            var cc = "";
            var dateStr = "";

            foreach (var header in headers.EnumerateArray())
            {
                var name = header.GetProperty("name").GetString();
                var value = header.GetProperty("value").GetString() ?? "";
                switch (name?.ToLower())
                {
                    case "subject": subject = value; break;
                    case "from": from = value; break;
                    case "to": to = value; break;
                    case "cc": cc = value; break;
                    case "date": dateStr = value; break;
                }
            }

            var snippet = detail.TryGetProperty("snippet", out var snip) ? snip.GetString() : "";
            var labelIds = detail.TryGetProperty("labelIds", out var labels)
                ? JsonSerializer.Serialize(labels.EnumerateArray().Select(l => l.GetString()).ToList())
                : "[]";

            // Parse from field: "Name <email>" or just "email"
            var (fromName, fromEmail) = ParseEmailAddress(from);

            // Parse received date
            DateTime receivedAt;
            if (!DateTime.TryParse(dateStr, out receivedAt))
                receivedAt = DateTime.UtcNow;

            var isRead = !labelIds.Contains("UNREAD");

            var syncedEmail = new SyncedEmail
            {
                ConnectedEmailAccountId = account.Id,
                ExternalId = messageId,
                ThreadId = threadId,
                Subject = subject,
                Snippet = snippet,
                FromEmail = fromEmail,
                FromName = fromName,
                ToEmails = string.IsNullOrEmpty(to) ? null : JsonSerializer.Serialize(to.Split(',').Select(e => e.Trim()).ToList()),
                CcEmails = string.IsNullOrEmpty(cc) ? null : JsonSerializer.Serialize(cc.Split(',').Select(e => e.Trim()).ToList()),
                BodyPreview = snippet?.Length > 200 ? snippet.Substring(0, 200) : snippet,
                IsRead = isRead,
                HasAttachments = labelIds.Contains("ATTACHMENT") || (detail.TryGetProperty("payload", out var payload) && payload.TryGetProperty("parts", out _)),
                Labels = labelIds,
                ReceivedAt = receivedAt,
            };

            _context.Set<SyncedEmail>().Add(syncedEmail);
            newCount++;
        }

        await _context.SaveChangesAsync();
    }

    return new SyncResultDto { NewEmails = newCount, UpdatedEmails = updatedCount, SyncedAt = DateTime.UtcNow };
}

private async Task<SyncResultDto> SyncOutlookEmailsAsync(ConnectedEmailAccount account, int maxResults)
{
    var client = _httpClientFactory.CreateClient();
    var accessToken = await EnsureValidMicrosoftTokenAsync(account);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var url = $"https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages?$top={maxResults}&$orderby=receivedDateTime desc&$select=id,conversationId,subject,bodyPreview,from,toRecipients,ccRecipients,isRead,hasAttachments,receivedDateTime,flag";
    var response = await client.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError("Outlook fetch messages failed: {Status}", response.StatusCode);
        account.SyncStatus = "failed";
        await _context.SaveChangesAsync();
        throw new InvalidOperationException("Failed to fetch emails from Outlook");
    }

    var json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<JsonElement>(json);

    int newCount = 0, updatedCount = 0;

    if (data.TryGetProperty("value", out var messages))
    {
        foreach (var msg in messages.EnumerateArray())
        {
            var messageId = msg.GetProperty("id").GetString()!;
            var threadId = msg.TryGetProperty("conversationId", out var cid) ? cid.GetString() : null;

            var existing = await _context.Set<SyncedEmail>()
                .FirstOrDefaultAsync(e => e.ConnectedEmailAccountId == account.Id && e.ExternalId == messageId);

            if (existing != null) { updatedCount++; continue; }

            var subject = msg.TryGetProperty("subject", out var sub) ? sub.GetString() ?? "" : "";
            var bodyPreview = msg.TryGetProperty("bodyPreview", out var bp) ? bp.GetString() : "";
            var isRead = msg.TryGetProperty("isRead", out var ir) && ir.GetBoolean();
            var hasAttachments = msg.TryGetProperty("hasAttachments", out var ha) && ha.GetBoolean();

            var fromEmail = "";
            var fromName = "";
            if (msg.TryGetProperty("from", out var fromObj) && fromObj.TryGetProperty("emailAddress", out var ea))
            {
                fromEmail = ea.TryGetProperty("address", out var addr) ? addr.GetString() ?? "" : "";
                fromName = ea.TryGetProperty("name", out var fn) ? fn.GetString() : null;
            }

            var toList = new List<string>();
            if (msg.TryGetProperty("toRecipients", out var tos))
            {
                foreach (var r in tos.EnumerateArray())
                {
                    if (r.TryGetProperty("emailAddress", out var tea) && tea.TryGetProperty("address", out var ta))
                        toList.Add(ta.GetString() ?? "");
                }
            }

            var ccList = new List<string>();
            if (msg.TryGetProperty("ccRecipients", out var ccs))
            {
                foreach (var r in ccs.EnumerateArray())
                {
                    if (r.TryGetProperty("emailAddress", out var cea) && cea.TryGetProperty("address", out var ca))
                        ccList.Add(ca.GetString() ?? "");
                }
            }

            var receivedStr = msg.TryGetProperty("receivedDateTime", out var rd) ? rd.GetString() : null;
            DateTime receivedAt = DateTime.TryParse(receivedStr, out var dt) ? dt : DateTime.UtcNow;

            var syncedEmail = new SyncedEmail
            {
                ConnectedEmailAccountId = account.Id,
                ExternalId = messageId,
                ThreadId = threadId,
                Subject = subject,
                Snippet = bodyPreview?.Length > 100 ? bodyPreview.Substring(0, 100) : bodyPreview,
                FromEmail = fromEmail,
                FromName = fromName,
                ToEmails = toList.Count > 0 ? JsonSerializer.Serialize(toList) : null,
                CcEmails = ccList.Count > 0 ? JsonSerializer.Serialize(ccList) : null,
                BodyPreview = bodyPreview?.Length > 200 ? bodyPreview.Substring(0, 200) : bodyPreview,
                IsRead = isRead,
                HasAttachments = hasAttachments,
                ReceivedAt = receivedAt,
            };

            _context.Set<SyncedEmail>().Add(syncedEmail);
            newCount++;
        }

        await _context.SaveChangesAsync();
    }

    return new SyncResultDto { NewEmails = newCount, UpdatedEmails = updatedCount, SyncedAt = DateTime.UtcNow };
}

public async Task<SyncedEmailsPageDto> GetSyncedEmailsAsync(Guid accountId, int userId, int page = 1, int pageSize = 25, string? search = null)
{
    var account = await _context.ConnectedEmailAccounts
        .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId);

    if (account == null)
        return new SyncedEmailsPageDto();

    var query = _context.Set<SyncedEmail>()
        .Where(e => e.ConnectedEmailAccountId == accountId);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.ToLower();
        query = query.Where(e =>
            e.Subject.ToLower().Contains(term) ||
            e.FromEmail.ToLower().Contains(term) ||
            (e.FromName != null && e.FromName.ToLower().Contains(term)) ||
            (e.Snippet != null && e.Snippet.ToLower().Contains(term))
        );
    }

    var totalCount = await query.CountAsync();

    var emails = await query
        .OrderByDescending(e => e.ReceivedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new SyncedEmailDto
        {
            Id = e.Id,
            ExternalId = e.ExternalId,
            ThreadId = e.ThreadId,
            Subject = e.Subject,
            Snippet = e.Snippet,
            FromEmail = e.FromEmail,
            FromName = e.FromName,
            ToEmails = e.ToEmails,
            CcEmails = e.CcEmails,
            BodyPreview = e.BodyPreview,
            IsRead = e.IsRead,
            IsStarred = e.IsStarred,
            HasAttachments = e.HasAttachments,
            Labels = e.Labels,
            ReceivedAt = e.ReceivedAt,
        })
        .ToListAsync();

    return new SyncedEmailsPageDto
    {
        Emails = emails,
        TotalCount = totalCount,
    };
}

// ────────────────────────────────────────────────
// Token refresh helpers
// ────────────────────────────────────────────────

private async Task<string> EnsureValidGoogleTokenAsync(ConnectedEmailAccount account)
{
    // Try the existing token first — if it fails, refresh
    var client = _httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.AccessToken);

    var testResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + account.AccessToken);

    if (testResponse.IsSuccessStatusCode)
        return account.AccessToken;

    // Token expired — refresh it
    _logger.LogInformation("Refreshing Google token for {Handle}", account.Handle);

    var refreshClient = _httpClientFactory.CreateClient();
    var body = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = _configuration["OAuth:Google:ClientId"] ?? "",
        ["client_secret"] = _configuration["OAuth:Google:ClientSecret"] ?? "",
        ["refresh_token"] = account.RefreshToken,
        ["grant_type"] = "refresh_token"
    });

    var response = await refreshClient.PostAsync("https://oauth2.googleapis.com/token", body);
    if (!response.IsSuccessStatusCode)
    {
        account.AuthFailedAt = DateTime.UtcNow;
        account.SyncStatus = "failed";
        await _context.SaveChangesAsync();
        throw new InvalidOperationException("Failed to refresh Google access token");
    }

    var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    var newToken = json?.GetValueOrDefault("access_token")?.ToString() ?? "";

    account.AccessToken = newToken;
    account.AuthFailedAt = null;
    account.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return newToken;
}

private async Task<string> EnsureValidMicrosoftTokenAsync(ConnectedEmailAccount account)
{
    var client = _httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", account.AccessToken);

    var testResponse = await client.GetAsync("https://graph.microsoft.com/v1.0/me");

    if (testResponse.IsSuccessStatusCode)
        return account.AccessToken;

    // Token expired — refresh it
    _logger.LogInformation("Refreshing Microsoft token for {Handle}", account.Handle);

    var refreshClient = _httpClientFactory.CreateClient();
    var body = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = _configuration["OAuth:Microsoft:ClientId"] ?? "",
        ["client_secret"] = _configuration["OAuth:Microsoft:ClientSecret"] ?? "",
        ["refresh_token"] = account.RefreshToken,
        ["grant_type"] = "refresh_token",
        ["scope"] = "openid email profile offline_access Mail.ReadWrite Mail.Send Calendars.Read User.Read"
    });

    var response = await refreshClient.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", body);
    if (!response.IsSuccessStatusCode)
    {
        account.AuthFailedAt = DateTime.UtcNow;
        account.SyncStatus = "failed";
        await _context.SaveChangesAsync();
        throw new InvalidOperationException("Failed to refresh Microsoft access token");
    }

    var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    var newToken = json?.GetValueOrDefault("access_token")?.ToString() ?? "";
    var newRefresh = json?.GetValueOrDefault("refresh_token")?.ToString();

    account.AccessToken = newToken;
    if (!string.IsNullOrEmpty(newRefresh)) account.RefreshToken = newRefresh;
    account.AuthFailedAt = null;
    account.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return newToken;
}

// ────────────────────────────────────────────────
// Helper: parse "Display Name <email@example.com>" format
// ────────────────────────────────────────────────

private static (string? name, string email) ParseEmailAddress(string raw)
{
    if (string.IsNullOrEmpty(raw)) return (null, "");

    var match = System.Text.RegularExpressions.Regex.Match(raw, @"^(.+?)\s*<(.+?)>$");
    if (match.Success)
    {
        return (match.Groups[1].Value.Trim().Trim('"'), match.Groups[2].Value.Trim());
    }

    return (null, raw.Trim());
}

*/
