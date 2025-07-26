using Application.Common.DTO;

namespace Infrastructure.Services;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateAsync(Guid templateId, Dictionary<string, object> variables);
    Task<EmailTemplateDTO> CreateTemplateAsync(EmailTemplateDTO template);
    Task<EmailTemplateDTO> UpdateTemplateAsync(Guid id, EmailTemplateDTO template);
    Task DeleteTemplateAsync(Guid id);
    Task<IEnumerable<EmailTemplateDTO>> GetTemplatesAsync(Guid userId);
}
