using Application.Common.DTO;

namespace Infrastructure.Services;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(
        Guid templateId,
        Guid companyId,
        Dictionary<string, object> variables
    );
    Task<EmailTemplateDTO> CreateTemplateAsync(
        EmailTemplateDTO template,
        Guid companyId,
        Guid createdByTaxUserId
    );
    Task<EmailTemplateDTO> UpdateTemplateAsync(
        Guid id,
        EmailTemplateDTO template,
        Guid companyId,
        Guid lastModifiedByTaxUserId
    );
    Task DeleteTemplateAsync(Guid id, Guid companyId, Guid deletedByTaxUserId);
    Task<IEnumerable<EmailTemplateDTO>> GetTemplatesAsync(Guid companyId, Guid? taxUserId = null);
}
