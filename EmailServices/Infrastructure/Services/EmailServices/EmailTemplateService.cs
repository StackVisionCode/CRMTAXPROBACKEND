using System.Text.RegularExpressions;
using Application.Common.DTO;
using AutoMapper;
using Domain;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly EmailContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        EmailContext context,
        IMapper mapper,
        ILogger<EmailTemplateService> logger
    )
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<string> RenderTemplateAsync(
        Guid templateId,
        Dictionary<string, object> variables
    )
    {
        var template = await _context.EmailTemplates.FindAsync(templateId);
        if (template is null)
            throw new KeyNotFoundException("Template not found");

        if (!template.IsActive)
            throw new InvalidOperationException("Template is not active");

        var rendered = template.BodyTemplate;

        // Simple template variable replacement using {{variableName}} syntax
        foreach (var variable in variables)
        {
            var pattern = $@"\{{\{{\s*{Regex.Escape(variable.Key)}\s*\}}\}}";
            rendered = Regex.Replace(
                rendered,
                pattern,
                variable.Value?.ToString() ?? "",
                RegexOptions.IgnoreCase
            );
        }

        return rendered;
    }

    public async Task<EmailTemplateDTO> CreateTemplateAsync(EmailTemplateDTO templateDto)
    {
        var template = _mapper.Map<EmailTemplate>(templateDto);
        template.CreatedOn = DateTime.UtcNow;
        template.IsActive = true;

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        return _mapper.Map<EmailTemplateDTO>(template);
    }

    public async Task<EmailTemplateDTO> UpdateTemplateAsync(Guid id, EmailTemplateDTO templateDto)
    {
        var template = await _context.EmailTemplates.FindAsync(id);
        if (template is null)
            throw new KeyNotFoundException("Template not found");

        _mapper.Map(templateDto, template);
        template.UpdatedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return _mapper.Map<EmailTemplateDTO>(template);
    }

    public async Task DeleteTemplateAsync(Guid id)
    {
        var template = await _context.EmailTemplates.FindAsync(id);
        if (template is null)
            throw new KeyNotFoundException("Template not found");

        _context.EmailTemplates.Remove(template);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<EmailTemplateDTO>> GetTemplatesAsync(Guid userId)
    {
        var templates = await _context
            .EmailTemplates.Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EmailTemplateDTO>>(templates);
    }
}
