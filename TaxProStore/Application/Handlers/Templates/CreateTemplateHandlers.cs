using Application.Common;
using Application.Domain.Entity.Templates;
using Application.Dtos;
using AutoMapper;
using Infrastructure.Command.Templates;
using Infrastructure.Context;
using MediatR;

namespace Application.Handlers.Templates;


public sealed class CreateTemplateHandlers : IRequestHandler<CreateTemplateCommads, ApiResponse<TemplateDto>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly ILogger<CreateTemplateHandlers> _log;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public CreateTemplateHandlers(
        TaxProStoreDbContext db,
         ILogger<CreateTemplateHandlers> log,
          IMapper mapper,
            IConfiguration configuration

    )
    {
        _db = db;
        _log = log;
        _mapper = mapper;
        _configuration = configuration;
    }

    public async Task<ApiResponse<TemplateDto>> Handle(CreateTemplateCommads request, CancellationToken cancellationToken)
    {

        // Aquí se implementaría la lógica para crear una plantilla
        // Por ejemplo, podrías mapear el DTO a una entidad y guardarla en la
        // base de datos usando el contexto _db.
        _log.LogInformation("Handling CreateTemplateCommads for TemplateDto: {TemplateDto}", request.TemplateDto);
        if (request.TemplateDto == null)
        {
            _log.LogError("TemplateDto is null.");
            return new ApiResponse<TemplateDto>(false, "Template data is required.");
        }
        var templateEntity = _mapper.Map<Template>(request.TemplateDto);
        templateEntity.HtmlContent.Replace("\n", "").Replace("\r", "");
        templateEntity.Id = Guid.NewGuid();
        templateEntity.CreatedAt = DateTime.UtcNow;
        // Generar la URL del preview
         var backendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
         templateEntity.PreviewUrl = $"{backendBaseUrl}/template/preview/{templateEntity.Id}";
        await _db.Templates.AddAsync(templateEntity);
        await _db.SaveChangesAsync(cancellationToken);

        if (templateEntity.Id == Guid.Empty)
        {
            _log.LogError("Failed to create template.");
            return new ApiResponse<TemplateDto>(false, "Failed to create template.");
        }
        var valor = new TemplateDto
        {
            Id = templateEntity.Id,
            Name = templateEntity.Name,
            HtmlContent = templateEntity.HtmlContent,
            PreviewUrl = templateEntity.PreviewUrl,
            OwnerUserId = templateEntity.OwnerUserId
        };
        _log.LogInformation("Template created successfully with ID: {TemplateId}", templateEntity.Id);
        return new ApiResponse<TemplateDto>(true, "Template created successfully.", valor);
    }
}