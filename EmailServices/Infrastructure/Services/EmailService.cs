using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Domain;
using EmailServices.Domain;
using Infrastructure.Context;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

public class EmailService : IEmailService
{
    private readonly EmailContext _context; // contexto de base de datos (inyectado)
    private readonly ILogger<EmailService> _logger;

    // Allowed domains for recipients (could also come from config or database)
    private readonly string[] _allowedDomains = new string[]
    {
        "gmail.com",
        "outlook.com",
        "empresa.com",
    };

    public EmailService(EmailContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendEmailAsync(Email email, EmailConfig config)
    {
        // Validation: daily sending limit
        DateTime today = DateTime.UtcNow.Date;
        int sentCountToday = await _context.Emails.CountAsync(e =>
            e.ConfigId == config.Id
            && e.SentOn != null
            && e.SentOn.Value.Date == today
            && e.Status == EmailStatus.Sent
        );
        if (sentCountToday >= config.DailyLimit)
        {
            throw new InvalidOperationException("Daily email limit reached for this configuration");
        }

        // Validation: email addresses format and domain
        // Combine To, Cc, Bcc into one list for validation
        var allRecipients = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrWhiteSpace(email.ToAddresses))
            allRecipients.AddRange(
                email.ToAddresses.Split(
                    new char[] { ';', ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        if (!string.IsNullOrWhiteSpace(email.CcAddresses))
            allRecipients.AddRange(
                email.CcAddresses.Split(
                    new char[] { ';', ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        if (!string.IsNullOrWhiteSpace(email.BccAddresses))
            allRecipients.AddRange(
                email.BccAddresses.Split(
                    new char[] { ';', ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries
                )
            );
        foreach (string addr in allRecipients)
        {
            try
            {
                var mailAddr = new MailAddress(addr);
                string domain = mailAddr.Host;
                if (!_allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Email domain not allowed: {domain}");
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException($"Invalid email address format: {addr}");
            }
        }

        // Ensure From address is set (use config's email if not provided)
        if (string.IsNullOrEmpty(email.FromAddress))
        {
            email.FromAddress =
                (config.ProviderType == "Gmail") ? config.GmailEmailAddress : config.SmtpUsername;
        }

        // Attempt sending based on provider type
        if (config.ProviderType.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            // SMTP sending
            using (var smtpClient = new SmtpClient(config.SmtpServer, config.SmtpPort ?? 25))
            using (var message = new MailMessage())
            {
                smtpClient.EnableSsl = config.EnableSsl ?? true;
                smtpClient.Credentials = new NetworkCredential(
                    config.SmtpUsername,
                    config.SmtpPassword
                );
                // Prepare MailMessage
                message.From = new MailAddress(email.FromAddress);
                // Add recipients
                foreach (
                    string to in email.ToAddresses.Split(
                        new char[] { ';', ',', ' ' },
                        StringSplitOptions.RemoveEmptyEntries
                    )
                )
                {
                    message.To.Add(to.Trim());
                }
                if (!string.IsNullOrWhiteSpace(email.CcAddresses))
                {
                    foreach (
                        string cc in email.CcAddresses.Split(
                            new char[] { ';', ',', ' ' },
                            StringSplitOptions.RemoveEmptyEntries
                        )
                    )
                    {
                        message.CC.Add(cc.Trim());
                    }
                }
                if (!string.IsNullOrWhiteSpace(email.BccAddresses))
                {
                    foreach (
                        string bcc in email.BccAddresses.Split(
                            new char[] { ';', ',', ' ' },
                            StringSplitOptions.RemoveEmptyEntries
                        )
                    )
                    {
                        message.Bcc.Add(bcc.Trim());
                    }
                }
                message.Subject = email.Subject;
                message.Body = email.Body;
                message.IsBodyHtml = false; // assume plain text body for now

                int attempt = 0;
                bool sent = false;
                Exception lastError = null;
                while (attempt < 3 && !sent)
                {
                    attempt++;
                    try
                    {
                        _logger.LogInformation(
                            $"Sending Email Id={email.Id} via SMTP (attempt {attempt})..."
                        );
                        await smtpClient.SendMailAsync(message);
                        sent = true;
                    }
                    catch (SmtpException ex)
                    {
                        lastError = ex;
                        _logger.LogWarning(
                            $"SMTP send attempt {attempt} failed: {ex.StatusCode} - {ex.Message}"
                        );
                        // Only retry on certain transient errors
                        if (
                            ex.StatusCode == SmtpStatusCode.MailboxBusy
                            || ex.StatusCode == SmtpStatusCode.MailboxUnavailable
                            || ex.StatusCode == SmtpStatusCode.TransactionFailed
                            || ex.StatusCode == SmtpStatusCode.ServiceNotAvailable
                        )
                        {
                            // Wait a bit and retry
                            await Task.Delay(2000);
                            continue;
                        }
                        else
                        {
                            // For other errors (authentication failed, etc.), do not retry
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        _logger.LogError($"Unexpected error during SMTP send: {ex.Message}");
                        break;
                    }
                }
                if (!sent)
                {
                    // Mark as failed and throw
                    email.Status = EmailStatus.Failed;
                    email.ErrorMessage = lastError?.Message;
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException(
                        $"Failed to send email via SMTP: {lastError?.Message}"
                    );
                }
            }
        }
        else if (config.ProviderType.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
        {
            /* ---------- Gmail API ---------- */
            string accessToken = config.GmailAccessToken;

            // ⇣⇣ NUEVO: calcula si el token falta o expiró
            bool tokenExpired =
                string.IsNullOrEmpty(accessToken)
                || !config.GmailTokenExpiry.HasValue
                || DateTime.UtcNow >= config.GmailTokenExpiry.Value;
            // ⇡⇡ ----------------------------------

            if (tokenExpired)
            {
                _logger.LogInformation("Refreshing Gmail API access token...");
                using var tokenClient = new HttpClient();

                var content = new FormUrlEncodedContent(
                    new[]
                    {
                        new KeyValuePair<string, string>("client_id", config.GmailClientId!),
                        new KeyValuePair<string, string>(
                            "client_secret",
                            config.GmailClientSecret!
                        ),
                        new KeyValuePair<string, string>(
                            "refresh_token",
                            config.GmailRefreshToken!
                        ),
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    }
                );

                var tokenResponse = await tokenClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    content
                );
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError(
                        $"Failed to refresh Gmail token: {tokenResponse.StatusCode}, Details: {errorContent}"
                    );
                    throw new InvalidOperationException(
                        $"Could not refresh Gmail access token: {errorContent}"
                    );
                }
                var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(tokenJson);
                accessToken = doc.RootElement.GetProperty("access_token").GetString();
                int expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

                // guarda nuevo token y expiración
                config.GmailAccessToken = accessToken;
                config.GmailTokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
                await _context.SaveChangesAsync();
            }

            // Build raw MIME email content
            StringBuilder mimeBuilder = new StringBuilder();
            mimeBuilder.AppendLine($"From: {email.FromAddress}");
            mimeBuilder.AppendLine($"To: {email.ToAddresses}");
            if (!string.IsNullOrWhiteSpace(email.CcAddresses))
                mimeBuilder.AppendLine($"Cc: {email.CcAddresses}");
            mimeBuilder.AppendLine($"Subject: {email.Subject}");
            mimeBuilder.AppendLine("Content-Type: text/plain; charset=utf-8");
            mimeBuilder.AppendLine();
            mimeBuilder.AppendLine(email.Body);
            string rawMessage = mimeBuilder.ToString();
            // Base64URL encode the MIME message
            string base64Raw;
            {
                byte[] rawBytes = Encoding.UTF8.GetBytes(rawMessage);
                string base64 = Convert.ToBase64String(rawBytes);
                base64Raw = base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
            // Send via Gmail API
            using (var gmailClient = new HttpClient())
            {
                gmailClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var gmailContent = new StringContent(
                    $"{{\"raw\":\"{base64Raw}\"}}",
                    Encoding.UTF8,
                    "application/json"
                );
                _logger.LogInformation($"Sending Email Id={email.Id} via Gmail API...");
                var gmailResponse = await gmailClient.PostAsync(
                    "https://gmail.googleapis.com/gmail/v1/users/me/messages/send",
                    gmailContent
                );
                if (!gmailResponse.IsSuccessStatusCode)
                {
                    if (gmailResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning(
                            "Gmail API returned 401 Unauthorized. Will refresh token and retry once."
                        );
                        // Invalidate stored token and retry once
                        config.GmailAccessToken = null;
                        config.GmailTokenExpiry = null;
                        await _context.SaveChangesAsync();
                        await SendEmailAsync(email, config);
                        return;
                    }
                    string errorDetail = await gmailResponse.Content.ReadAsStringAsync();
                    _logger.LogError(
                        $"Gmail API send failed: {gmailResponse.StatusCode}, Details: {errorDetail}"
                    );
                    email.Status = EmailStatus.Failed;
                    email.ErrorMessage = $"Gmail send error: {gmailResponse.StatusCode}";
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException(
                        $"Failed to send email via Gmail API: {gmailResponse.StatusCode}"
                    );
                }
            }
        }

        // Mark email as sent in database
        email.Status = EmailStatus.Sent;
        email.SentOn = DateTime.UtcNow;
        email.ErrorMessage = null;
        await _context.SaveChangesAsync();
        _logger.LogInformation($"Email Id={email.Id} sent successfully via {config.ProviderType}");
    }
}
