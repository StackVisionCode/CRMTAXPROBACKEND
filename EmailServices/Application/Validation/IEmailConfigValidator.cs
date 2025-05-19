namespace Application.Validation;

using Application.Common.DTO;

public interface IEmailConfigValidator
{
    void Validate(EmailConfigDTO cfg);
}