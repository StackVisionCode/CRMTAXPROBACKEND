namespace Application.Validation;

using Application.Common.DTO;

public interface IEmailConfigValidator
{
    void Validate(EmailConfigDTO cfg);
    void Validate(CreateEmailConfigDTO cfg);
    void Validate(UpdateEmailConfigDTO cfg);
}
