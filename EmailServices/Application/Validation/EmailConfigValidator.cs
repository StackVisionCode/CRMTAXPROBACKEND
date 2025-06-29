namespace Application.Validation;

using Application.Common.DTO;

public sealed class EmailConfigValidator : IEmailConfigValidator
{
    public void Validate(EmailConfigDTO cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.ProviderType))
            throw new ArgumentException("ProviderType is required");

        if (cfg.ProviderType.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (
                string.IsNullOrWhiteSpace(cfg.SmtpServer)
                || !cfg.SmtpPort.HasValue
                || string.IsNullOrWhiteSpace(cfg.SmtpUsername)
                || string.IsNullOrWhiteSpace(cfg.SmtpPassword)
            )
                throw new ArgumentException("SMTP fields are mandatory");
        }
        else if (cfg.ProviderType.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
        {
            if (
                string.IsNullOrWhiteSpace(cfg.GmailClientId)
                || string.IsNullOrWhiteSpace(cfg.GmailClientSecret)
                || string.IsNullOrWhiteSpace(cfg.GmailRefreshToken)
                || string.IsNullOrWhiteSpace(cfg.GmailEmailAddress)
            )
                throw new ArgumentException("Gmail OAuth2 fields are mandatory");
        }
        else
        {
            throw new ArgumentException("Unknown ProviderType");
        }
    }
}
