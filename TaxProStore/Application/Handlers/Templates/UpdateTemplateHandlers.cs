using Application.Common;
using AutoMapper;
using Infrastructure.Command.Templates;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Templates;


public class UpdateTemplateHandlers : IRequestHandler<UpdateTemplateCommands, ApiResponse<bool>>
{

    private readonly TaxProStoreDbContext _db;
    private readonly ILogger<CreateTemplateHandlers> _log;
    private readonly IMapper _mapper;


    public UpdateTemplateHandlers(
            TaxProStoreDbContext db,
            ILogger<CreateTemplateHandlers> log,
            IMapper mapper)
    {
        _db = db;
        _log = log;
        _mapper = mapper;

    }



    public async Task<ApiResponse<bool>> Handle(UpdateTemplateCommands request, CancellationToken cancellationToken)
    {
      try
        {
            var existingTemplate = await _db.Templates
                .FirstOrDefaultAsync(t => t.Id == request.IdTemplade, cancellationToken);

            if (existingTemplate == null)
            {
                return new  ApiResponse<bool>(false,"Template no encontrado");
            }

            // Mapea las propiedades desde el DTO al entity existente
          var map=  _mapper.Map(request.TemplateDto, existingTemplate);
                map.UpdatedAt= DateTime.UtcNow;
            _db.Templates.Update(existingTemplate);
        
            await _db.SaveChangesAsync(cancellationToken);

              return new ApiResponse<bool>(true, "Template update successfully.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error actualizando template con ID: {TemplateId}", request.IdTemplade);
            return new ApiResponse<bool>(false,"Error actualizando el template");
        }
    }
}