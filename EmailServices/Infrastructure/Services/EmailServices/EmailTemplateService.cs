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
        Guid companyId,
        Dictionary<string, object> variables
    )
    {
        // Buscar template con CompanyId para seguridad
        var template = await _context
            .EmailTemplates.Where(t => t.Id == templateId && t.CompanyId == companyId && t.IsActive)
            .FirstOrDefaultAsync();

        if (template is null)
            throw new KeyNotFoundException("Template not found or access denied");

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

        _logger.LogInformation(
            $"Template {templateId} rendered successfully for company {companyId}"
        );

        return rendered;
    }

    public async Task<EmailTemplateDTO> CreateTemplateAsync(
        EmailTemplateDTO templateDto,
        Guid companyId,
        Guid createdByTaxUserId
    )
    {
        // Verificar que no existe otro template con el mismo nombre en la compañía
        var nameExists = await _context
            .EmailTemplates.Where(t =>
                t.CompanyId == companyId && t.Name == templateDto.Name && t.IsActive
            )
            .AnyAsync();

        if (nameExists)
            throw new InvalidOperationException(
                "A template with this name already exists for this company"
            );

        var template = _mapper.Map<EmailTemplate>(templateDto);

        // Establecer campos obligatorios
        template.Id = Guid.NewGuid();
        template.CompanyId = companyId;
        template.CreatedByTaxUserId = createdByTaxUserId;
        template.CreatedOn = DateTime.UtcNow;
        template.IsActive = true;

        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Email template {template.Id} created by user {createdByTaxUserId} for company {companyId}"
        );

        return _mapper.Map<EmailTemplateDTO>(template);
    }

    public async Task<EmailTemplateDTO> UpdateTemplateAsync(
        Guid id,
        EmailTemplateDTO templateDto,
        Guid companyId,
        Guid lastModifiedByTaxUserId
    )
    {
        // Buscar template con CompanyId para seguridad
        var template = await _context
            .EmailTemplates.Where(t => t.Id == id && t.CompanyId == companyId && t.IsActive)
            .FirstOrDefaultAsync();

        if (template is null)
            throw new KeyNotFoundException("Template not found or access denied");

        // Verificar que no existe otro template con el mismo nombre en la compañía
        var nameExists = await _context
            .EmailTemplates.Where(t =>
                t.CompanyId == companyId && t.Name == templateDto.Name && t.Id != id && t.IsActive
            )
            .AnyAsync();

        if (nameExists)
            throw new InvalidOperationException(
                "A template with this name already exists for this company"
            );

        // Preservar campos importantes
        var originalId = template.Id;
        var originalCompanyId = template.CompanyId;
        var originalCreatedByTaxUserId = template.CreatedByTaxUserId;
        var originalCreatedOn = template.CreatedOn;

        _mapper.Map(templateDto, template);

        // Restaurar campos que no deben cambiar y actualizar auditoría
        template.Id = originalId;
        template.CompanyId = originalCompanyId;
        template.CreatedByTaxUserId = originalCreatedByTaxUserId;
        template.CreatedOn = originalCreatedOn;
        template.LastModifiedByTaxUserId = lastModifiedByTaxUserId;
        template.UpdatedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Email template {template.Id} updated by user {lastModifiedByTaxUserId} for company {companyId}"
        );

        return _mapper.Map<EmailTemplateDTO>(template);
    }

    public async Task DeleteTemplateAsync(Guid id, Guid companyId, Guid deletedByTaxUserId)
    {
        // Buscar template con CompanyId para seguridad
        var template = await _context
            .EmailTemplates.Where(t => t.Id == id && t.CompanyId == companyId && t.IsActive)
            .FirstOrDefaultAsync();

        if (template is null)
            throw new KeyNotFoundException("Template not found or access denied");

        // Verificar si hay emails usando este template
        // Nota: Si implementas referencia a templates en emails, agregar esta validación

        // Soft delete
        template.IsActive = false;
        template.LastModifiedByTaxUserId = deletedByTaxUserId;
        template.UpdatedOn = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Email template {id} deleted by user {deletedByTaxUserId} for company {companyId}"
        );
    }

    public async Task<IEnumerable<EmailTemplateDTO>> GetTemplatesAsync(
        Guid companyId,
        Guid? taxUserId = null
    )
    {
        // Filtrar por CompanyId y opcionalmente por TaxUserId
        var query = _context.EmailTemplates.Where(t => t.CompanyId == companyId && t.IsActive);

        if (taxUserId.HasValue)
            query = query.Where(t => t.CreatedByTaxUserId == taxUserId);

        var templates = await query
            .OrderBy(t => t.Name)
            .ThenByDescending(t => t.CreatedOn)
            .ToListAsync();

        _logger.LogInformation($"Retrieved {templates.Count} templates for company {companyId}");

        return _mapper.Map<IEnumerable<EmailTemplateDTO>>(templates);
    }
}
