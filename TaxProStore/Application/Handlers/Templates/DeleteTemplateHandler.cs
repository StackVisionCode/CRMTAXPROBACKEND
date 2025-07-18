using Application.Common;
using Infrastructure.Command.Templates;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Templates;

public class DeleteTemplateHandler : IRequestHandler<DeleteTemplateCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _db;
    private readonly ILogger<DeleteTemplateHandler> _log;

    public DeleteTemplateHandler(
        TaxProStoreDbContext db,
        ILogger<DeleteTemplateHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _db.Templates
                .FirstOrDefaultAsync(t => t.Id == request.IdTemplate, cancellationToken);

            if (template == null)
                return new ApiResponse<bool>(false,"Template no encontrado");
                template.DeleteAt = DateTime.UtcNow;
                template.IsPublished = false;
            _db.Templates.Update(template);
            await _db.SaveChangesAsync(cancellationToken);

            return new ApiResponse<bool>(true, "Template eliminado correctamente");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error eliminando el template con ID {TemplateId}", request.IdTemplate);
            return new ApiResponse<bool>(false, "Error eliminando el template");
        }
    }
}
