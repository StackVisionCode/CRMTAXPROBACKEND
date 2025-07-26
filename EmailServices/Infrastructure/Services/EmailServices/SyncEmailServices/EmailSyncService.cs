using System.Text;
using System.Text.Json;
using Domain;
using Infrastructure.Context;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Infrastructure.Services;

public class EmailSyncService : IEmailSyncService
{
    private readonly EmailContext _context;
    private readonly ILogger<EmailSyncService> _logger;

    public EmailSyncService(EmailContext context, ILogger<EmailSyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailSyncResult> SyncEmailsAsync(Guid configId, DateTime? since = null)
    {
        var result = new EmailSyncResult { ConfigId = configId };

        try
        {
            var config = await _context.EmailConfigs.FindAsync(configId);
            if (config == null)
            {
                result.Message = "Configuration not found";
                return result;
            }

            result.ConfigName = config.Name;

            if (!since.HasValue)
            {
                since = await _context
                    .IncomingEmails.Where(e => e.ConfigId == configId)
                    .OrderByDescending(e => e.ReceivedOn)
                    .Select(e => e.ReceivedOn)
                    .FirstOrDefaultAsync();

                if (since.HasValue)
                {
                    since = since.Value.AddHours(-1);
                }
                else
                {
                    since = DateTime.UtcNow.AddDays(-7);
                }
            }

            _logger.LogInformation(
                "📧 Syncing emails for {ConfigName} since {Since}",
                config.Name,
                since
            );

            var fetchedEmails = await CheckAllEmailsAsync(config, 100, since);
            result.TotalFetched = fetchedEmails.Count();

            var savedEmails = await SaveUniqueEmailsAsync(fetchedEmails.ToList(), config);
            result.NewEmails = savedEmails.Count;
            result.ExistingEmails = result.TotalFetched - result.NewEmails; // ⭐ CORRECCIÓN: ExistingEmails en lugar de ExistingCount

            result.Success = true;
            result.Message =
                $"Synced successfully: {result.NewEmails} new emails, {result.ExistingEmails} already existed";

            _logger.LogInformation(
                "✅ Sync completed for {ConfigName}: {NewCount} new, {ExistingCount} existing",
                config.Name,
                result.NewEmails,
                result.ExistingEmails
            ); // ⭐ CORRECCIÓN: ExistingEmails
        }
        catch (Exception ex)
        {
            result.Message = $"Sync failed: {ex.Message}";
            _logger.LogError(
                ex,
                "❌ Sync failed for config {ConfigId}: {Error}",
                configId,
                ex.Message
            );
        }

        return result;
    }

    private async Task<IEnumerable<IncomingEmail>> CheckAllEmailsAsync(
        EmailConfig config,
        int maxMessages,
        DateTime? since
    )
    {
        _logger.LogInformation(
            "🔍 Checking ALL emails for config {ConfigName} (not just unread)",
            config.Name
        );

        return config.ProviderType?.ToLower() switch
        {
            "gmail" => await CheckAllGmailEmailsAsync(config, maxMessages, since),
            "smtp" => await CheckAllImapEmailsAsync(config, maxMessages, since),
            _ => throw new NotSupportedException(
                $"Provider type '{config.ProviderType}' not supported"
            ),
        };
    }

    private async Task<List<IncomingEmail>> CheckAllGmailEmailsAsync(
        EmailConfig config,
        int maxMessages,
        DateTime? since
    )
    {
        var emails = new List<IncomingEmail>();

        try
        {
            await RefreshGmailTokenIfNeeded(config);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    config.GmailAccessToken
                );

            var query = "in:inbox";
            if (since.HasValue)
            {
                var sinceStr = since.Value.ToString("yyyy/MM/dd");
                query += $" after:{sinceStr}";
            }

            var listUrl =
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults={maxMessages}&q={Uri.EscapeDataString(query)}";

            _logger.LogDebug("🔍 Gmail API query: {Query}", query);

            var listResponse = await client.GetAsync(listUrl);

            if (!listResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Failed to get Gmail message list: {listResponse.StatusCode}"
                );
            }

            var listJson = await listResponse.Content.ReadAsStringAsync();
            using var listDoc = JsonDocument.Parse(listJson);

            if (!listDoc.RootElement.TryGetProperty("messages", out var messagesElement))
            {
                _logger.LogInformation(
                    "📭 No messages found in Gmail inbox for {ConfigName}",
                    config.Name
                );
                return emails;
            }

            var messageCount = messagesElement.GetArrayLength();
            _logger.LogInformation(
                "📫 Found {Count} messages in Gmail inbox for {ConfigName}",
                messageCount,
                config.Name
            );

            foreach (var messageElement in messagesElement.EnumerateArray())
            {
                if (messageElement.TryGetProperty("id", out var idElement))
                {
                    var messageId = idElement.GetString();
                    // ⭐ CORRECCIÓN: Verificar null antes de pasar a método
                    if (!string.IsNullOrEmpty(messageId))
                    {
                        try
                        {
                            var email = await GetGmailMessageDetailsAsync(
                                client,
                                messageId,
                                config
                            );
                            if (email != null)
                            {
                                emails.Add(email);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "⚠️ Failed to process Gmail message {MessageId}",
                                messageId
                            );
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Error checking Gmail emails for config {ConfigName}",
                config.Name
            );
            throw;
        }

        return emails;
    }

    private async Task<List<IncomingEmail>> CheckAllImapEmailsAsync(
        EmailConfig config,
        int maxMessages,
        DateTime? since
    )
    {
        var emails = new List<IncomingEmail>();

        // ⭐ CORRECCIÓN: Verificar que SmtpServer no sea null
        if (string.IsNullOrEmpty(config.SmtpServer))
        {
            _logger.LogWarning("⚠️ SMTP server not configured for {ConfigName}", config.Name);
            return emails;
        }

        using var client = new ImapClient();
        var imapServer = GetImapServerFromSmtp(config.SmtpServer);
        var imapPort = GetImapPortFromSmtp(config.SmtpServer);

        await client.ConnectAsync(imapServer, imapPort, true);
        await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);

        SearchQuery searchQuery;
        if (since.HasValue)
        {
            searchQuery = SearchQuery.DeliveredAfter(since.Value.Date);
            _logger.LogDebug("🔍 IMAP searching emails since: {Since}", since.Value);
        }
        else
        {
            searchQuery = SearchQuery.All;
            _logger.LogDebug("🔍 IMAP searching ALL emails");
        }

        var uids = await inbox.SearchAsync(searchQuery);
        var recentUids = uids.OrderByDescending(u => u.Id).Take(maxMessages).ToList();

        _logger.LogInformation(
            "📫 Found {Total} total emails, processing {Recent} most recent for {ConfigName}",
            uids.Count,
            recentUids.Count,
            config.Name
        );

        foreach (var uid in recentUids)
        {
            try
            {
                var message = await inbox.GetMessageAsync(uid);
                var incomingEmail = ConvertMimeMessageToIncomingEmail(message);
                emails.Add(incomingEmail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error processing IMAP message {Uid}", uid);
            }
        }

        await client.DisconnectAsync(true);
        return emails;
    }

    private async Task RefreshGmailTokenIfNeeded(EmailConfig config)
    {
        bool tokenExpired =
            string.IsNullOrEmpty(config.GmailAccessToken)
            || !config.GmailTokenExpiry.HasValue
            || DateTime.UtcNow >= config.GmailTokenExpiry.Value;

        if (!tokenExpired)
            return;

        _logger.LogInformation("🔄 Refreshing Gmail token for config {ConfigName}", config.Name);

        using var tokenClient = new HttpClient();
        var content = new FormUrlEncodedContent(
            new[]
            {
                new KeyValuePair<string, string>("client_id", config.GmailClientId ?? ""),
                new KeyValuePair<string, string>("client_secret", config.GmailClientSecret ?? ""),
                new KeyValuePair<string, string>("refresh_token", config.GmailRefreshToken ?? ""),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            }
        );

        var tokenResponse = await tokenClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            content
        );

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var error = await tokenResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gmail token refresh failed: {error}");
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokenJson);

        var newAccessToken = doc.RootElement.GetProperty("access_token").GetString();
        int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

        config.GmailAccessToken = newAccessToken;
        config.GmailTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "✅ Gmail token refreshed successfully for {ConfigName}",
            config.Name
        );
    }

    private async Task<IncomingEmail?> GetGmailMessageDetailsAsync(
        HttpClient client,
        string messageId, // ⭐ CORRECCIÓN: messageId ya está verificado como no-null antes de llamar este método
        EmailConfig config
    )
    {
        try
        {
            var messageUrl = $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{messageId}";
            var messageResponse = await client.GetAsync(messageUrl);

            if (!messageResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "⚠️ Failed to get Gmail message details for {MessageId}: {StatusCode}",
                    messageId,
                    messageResponse.StatusCode
                );
                return null;
            }

            var messageJson = await messageResponse.Content.ReadAsStringAsync();
            var incomingEmail = ConvertGmailMessageToIncomingEmail(messageJson);

            incomingEmail.ConfigId = config.Id;
            incomingEmail.UserId = config.UserId;

            return incomingEmail;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "⚠️ Error processing Gmail message {MessageId}: {Error}",
                messageId,
                ex.Message
            );
            return null;
        }
    }

    private IncomingEmail ConvertGmailMessageToIncomingEmail(string messageJson)
    {
        using var doc = JsonDocument.Parse(messageJson);
        var payload = doc.RootElement.GetProperty("payload");
        var headers = payload.GetProperty("headers");

        var email = new IncomingEmail
        {
            Id = Guid.NewGuid(),
            IsRead = false,
            ReceivedOn = DateTime.UtcNow,
            Attachments = new List<EmailAttachment>(),
        };

        // Extract headers
        foreach (var header in headers.EnumerateArray())
        {
            var name = header.GetProperty("name").GetString();
            var value = header.GetProperty("value").GetString();

            switch (name?.ToLower())
            {
                case "from":
                    email.FromAddress = value ?? "";
                    break;
                case "to":
                    email.ToAddress = value ?? "";
                    break;
                case "cc":
                    email.CcAddresses = value;
                    break;
                case "subject":
                    email.Subject = value ?? "";
                    break;
                case "message-id":
                    email.MessageId = value;
                    break;
                case "in-reply-to":
                    email.InReplyTo = value;
                    break;
                case "references":
                    email.References = value;
                    break;
                case "date":
                    if (value != null && DateTime.TryParse(value, out var date))
                        email.ReceivedOn = date;
                    break;
            }
        }

        // Extract body
        email.Body = ExtractGmailMessageBody(payload);

        // Extract attachments
        email.Attachments = ExtractGmailAttachments(payload, email.Id);

        return email;
    }

    private IncomingEmail ConvertMimeMessageToIncomingEmail(MimeMessage message)
    {
        var email = new IncomingEmail
        {
            Id = Guid.NewGuid(),
            MessageId = message.MessageId,
            FromAddress = message.From.FirstOrDefault()?.ToString() ?? "",
            ToAddress = message.To.FirstOrDefault()?.ToString() ?? "",
            CcAddresses = string.Join("; ", message.Cc.Select(a => a.ToString())),
            Subject = message.Subject ?? "",
            Body = message.TextBody ?? message.HtmlBody ?? "",
            ReceivedOn = message.Date.DateTime,
            IsRead = false,
            InReplyTo = message.InReplyTo,
            References = string.Join(" ", message.References),
            Attachments = new List<EmailAttachment>(),
        };

        // Process attachments
        foreach (var attachment in message.Attachments.OfType<MimePart>())
        {
            using var memory = new MemoryStream();
            attachment.Content.DecodeTo(memory);

            email.Attachments.Add(
                new EmailAttachment
                {
                    Id = Guid.NewGuid(),
                    EmailId = email.Id,
                    FileName = attachment.FileName ?? "attachment",
                    ContentType = attachment.ContentType.MimeType,
                    Size = memory.Length,
                    Content = memory.ToArray(),
                    CreatedOn = DateTime.UtcNow,
                }
            );
        }

        return email;
    }

    private string ExtractGmailMessageBody(JsonElement payload)
    {
        try
        {
            // Try to get plain text body first
            if (
                payload.TryGetProperty("body", out var body)
                && body.TryGetProperty("data", out var data)
            )
            {
                var bodyData = data.GetString();
                if (!string.IsNullOrEmpty(bodyData))
                {
                    return DecodeBase64Url(bodyData);
                }
            }

            // Check for multipart content
            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("mimeType", out var mimeType))
                    {
                        var mimeTypeStr = mimeType.GetString();

                        // Prefer plain text, but accept HTML if no plain text
                        if (mimeTypeStr == "text/plain" || mimeTypeStr == "text/html")
                        {
                            if (
                                part.TryGetProperty("body", out var partBody)
                                && partBody.TryGetProperty("data", out var partData)
                            )
                            {
                                var bodyData = partData.GetString();
                                if (!string.IsNullOrEmpty(bodyData))
                                {
                                    return DecodeBase64Url(bodyData);
                                }
                            }
                        }
                    }
                }
            }

            return "No readable content found";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error extracting Gmail message body");
            return "Error extracting message body";
        }
    }

    private List<EmailAttachment> ExtractGmailAttachments(JsonElement payload, Guid emailId)
    {
        var attachments = new List<EmailAttachment>();

        try
        {
            if (payload.TryGetProperty("parts", out var parts))
            {
                foreach (var part in parts.EnumerateArray())
                {
                    if (
                        part.TryGetProperty("filename", out var filename)
                        && !string.IsNullOrEmpty(filename.GetString())
                        && part.TryGetProperty("body", out var body)
                    )
                    {
                        var attachment = new EmailAttachment
                        {
                            Id = Guid.NewGuid(),
                            EmailId = emailId,
                            FileName = filename.GetString() ?? "attachment",
                            ContentType = part.TryGetProperty("mimeType", out var mimeType)
                                ? mimeType.GetString() ?? "application/octet-stream"
                                : "application/octet-stream",
                            CreatedOn = DateTime.UtcNow,
                        };

                        // Get attachment size
                        if (body.TryGetProperty("size", out var size))
                        {
                            attachment.Size = size.GetInt64();
                        }

                        // Note: For Gmail API, actual attachment content needs to be fetched separately
                        // using attachmentId. For now, we just store metadata.
                        if (body.TryGetProperty("attachmentId", out var attachmentId))
                        {
                            // Store attachment ID for later retrieval if needed
                            attachment.FilePath = $"gmail_attachment:{attachmentId.GetString()}";
                        }

                        attachments.Add(attachment);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Error extracting Gmail attachments");
        }

        return attachments;
    }

    private static string DecodeBase64Url(string base64Url)
    {
        try
        {
            // Convert base64url to base64
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');

            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception)
        {
            return base64Url; // Return as-is if decoding fails
        }
    }

    private async Task<List<IncomingEmail>> SaveUniqueEmailsAsync(
        List<IncomingEmail> emails,
        EmailConfig config
    )
    {
        var savedEmails = new List<IncomingEmail>();

        if (!emails.Any())
        {
            return savedEmails;
        }

        var messageIds = emails
            .Where(e => !string.IsNullOrEmpty(e.MessageId))
            .Select(e => e.MessageId)
            .ToList();

        var existingMessageIds = await _context
            .IncomingEmails.Where(e => e.ConfigId == config.Id && messageIds.Contains(e.MessageId))
            .Select(e => e.MessageId)
            .ToListAsync();

        var newEmails = emails
            .Where(e =>
                string.IsNullOrEmpty(e.MessageId) || !existingMessageIds.Contains(e.MessageId)
            )
            .ToList();

        if (!newEmails.Any())
        {
            _logger.LogDebug("📭 All fetched emails already exist for {ConfigName}", config.Name);
            return savedEmails;
        }

        foreach (var email in newEmails)
        {
            email.ConfigId = config.Id;
            email.UserId = config.UserId;
            email.ReceivedOn = email.ReceivedOn == default ? DateTime.UtcNow : email.ReceivedOn;
        }

        try
        {
            _context.IncomingEmails.AddRange(newEmails);
            await _context.SaveChangesAsync();
            savedEmails.AddRange(newEmails);

            _logger.LogInformation(
                "💾 Saved {Count} new emails for {ConfigName}",
                newEmails.Count,
                config.Name
            );
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message?.Contains(
                    "IX_IncomingEmails_MessageId_ConfigId_Unique"
                ) == true
            )
        {
            _logger.LogWarning(
                "⚠️ Duplicate key conflict, saving emails individually for {ConfigName}",
                config.Name
            );

            _context.IncomingEmails.RemoveRange(newEmails);

            foreach (var email in newEmails)
            {
                try
                {
                    var exists = await _context.IncomingEmails.AnyAsync(e =>
                        e.MessageId == email.MessageId && e.ConfigId == config.Id
                    );

                    if (!exists)
                    {
                        _context.IncomingEmails.Add(email);
                        await _context.SaveChangesAsync();
                        savedEmails.Add(email);
                    }
                }
                catch (Exception individualEx)
                {
                    _logger.LogWarning(
                        individualEx,
                        "⚠️ Failed to save individual email: {Subject}",
                        email.Subject
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Failed to save emails for {ConfigName}: {Error}",
                config.Name,
                ex.Message
            );
            throw;
        }

        return savedEmails;
    }

    // ⭐ CORRECCIÓN: Parámetro ya verificado como no-null antes de llamar este método
    private string GetImapServerFromSmtp(string smtpServer)
    {
        return smtpServer.ToLower() switch
        {
            "smtp.gmail.com" => "imap.gmail.com",
            "smtp.outlook.com" => "imap-mail.outlook.com",
            "smtp.yahoo.com" => "imap.mail.yahoo.com",
            _ => smtpServer.Replace("smtp", "imap", StringComparison.OrdinalIgnoreCase),
        };
    }

    // ⭐ CORRECCIÓN: Parámetro ya verificado como no-null antes de llamar este método
    private int GetImapPortFromSmtp(string smtpServer)
    {
        return smtpServer.ToLower() switch
        {
            "smtp.gmail.com" => 993,
            "smtp.outlook.com" => 993,
            "smtp.yahoo.com" => 993,
            _ => 993,
        };
    }
}
