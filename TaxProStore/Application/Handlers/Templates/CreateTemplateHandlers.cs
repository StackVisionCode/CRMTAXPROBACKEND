using Application.Common;
using Application.Domain.Entity.Templates;
using AutoMapper;
using Infrastructure.Command.Templates;
using Infrastructure.Context;
using MediatR;

namespace Application.Handlers.Templates;


public sealed class CreateTemplateHandlers : IRequestHandler<CreateTemplateCommads, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly ILogger<CreateTemplateHandlers> _log;
    private readonly IMapper _mapper;


    public CreateTemplateHandlers(
        TaxProStoreDbContext db,
         ILogger<CreateTemplateHandlers> log,
          IMapper mapper

    )
    {
        _db = db;
        _log = log;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(CreateTemplateCommads request, CancellationToken cancellationToken)
    {

        // Aquí se implementaría la lógica para crear una plantilla
        // Por ejemplo, podrías mapear el DTO a una entidad y guardarla en la
        // base de datos usando el contexto _db.
        _log.LogInformation("Handling CreateTemplateCommads for TemplateDto: {TemplateDto}", request.TemplateDto);
        if (request.TemplateDto == null)
        {
            _log.LogError("TemplateDto is null.");
            return new ApiResponse<bool>(false, "Template data is required.");
        }
        var templateEntity = _mapper.Map<Template>(request.TemplateDto);
        templateEntity.HtmlContent.Replace("\n", "").Replace("\r", "");
        templateEntity.Id = Guid.NewGuid();
        templateEntity.CreatedAt = DateTime.UtcNow;
        // Generar la URL del preview
        templateEntity.PreviewUrl = $"http://localhost:5172/templates/preview/{templateEntity.Id}";
        await _db.Templates.AddAsync(templateEntity);
        await _db.SaveChangesAsync(cancellationToken);

        if (templateEntity.Id == Guid.Empty)
        {
            _log.LogError("Failed to create template.");
            return new ApiResponse<bool>(false, "Failed to create template.");
        }
        _log.LogInformation("Template created successfully with ID: {TemplateId}", templateEntity.Id);
        return new ApiResponse<bool>(true, "Template created successfully.");
    }
}