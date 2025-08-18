namespace Application.Validation;

using Application.Common.DTO;

public sealed class EmailConfigValidator : IEmailConfigValidator
{
    public void Validate(EmailConfigDTO cfg)
    {
        ValidateCommon(
            cfg.ProviderType,
            cfg.SmtpServer,
            cfg.SmtpPort,
            cfg.SmtpUsername,
            cfg.SmtpPassword,
            cfg.GmailClientId,
            cfg.GmailClientSecret,
            cfg.GmailRefreshToken,
            cfg.GmailEmailAddress
        );
    }

    public void Validate(CreateEmailConfigDTO cfg)
    {
        ValidateCommon(
            cfg.ProviderType,
            cfg.SmtpServer,
            cfg.SmtpPort,
            cfg.SmtpUsername,
            cfg.SmtpPassword,
            cfg.GmailClientId,
            cfg.GmailClientSecret,
            cfg.GmailRefreshToken,
            cfg.GmailEmailAddress
        );
    }

    public void Validate(UpdateEmailConfigDTO cfg)
    {
        ValidateCommon(
            cfg.ProviderType,
            cfg.SmtpServer,
            cfg.SmtpPort,
            cfg.SmtpUsername,
            cfg.SmtpPassword,
            cfg.GmailClientId,
            cfg.GmailClientSecret,
            cfg.GmailRefreshToken,
            cfg.GmailEmailAddress
        );
    }

    private void ValidateCommon(
        string? providerType,
        string? smtpServer,
        int? smtpPort,
        string? smtpUsername,
        string? smtpPassword,
        string? gmailClientId,
        string? gmailClientSecret,
        string? gmailRefreshToken,
        string? gmailEmailAddress
    )
    {
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("ProviderType is required");

        if (providerType.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (
                string.IsNullOrWhiteSpace(smtpServer)
                || !smtpPort.HasValue
                || string.IsNullOrWhiteSpace(smtpUsername)
                || string.IsNullOrWhiteSpace(smtpPassword)
            )
                throw new ArgumentException("SMTP fields are mandatory");
        }
        else if (providerType.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
        {
            if (
                string.IsNullOrWhiteSpace(gmailClientId)
                || string.IsNullOrWhiteSpace(gmailClientSecret)
                || string.IsNullOrWhiteSpace(gmailRefreshToken)
                || string.IsNullOrWhiteSpace(gmailEmailAddress)
            )
                throw new ArgumentException("Gmail OAuth2 fields are mandatory");
        }
        else
        {
            throw new ArgumentException("Unknown ProviderType");
        }
    }
}
