using Application.Common;
using AutoMapper;
using Domain.Entity.Form;
using Infrastructure.Command.Form;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.Form;

public class DeleteFormResponseHandler : IRequestHandler<DeleteFormIntanceCommads, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<DeleteFormResponseHandler> _logger;


    public DeleteFormResponseHandler(TaxProStoreDbContext context, IMapper mapper, ILogger<DeleteFormResponseHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteFormIntanceCommads request, CancellationToken cancellationToken)
    {
      try
        {
            // Buscar la FormResponse por ID
            var formResponse = await _context.FormResponses
                .FirstOrDefaultAsync(x => x.FormInstanceId  == request.Id, cancellationToken);

            if (formResponse == null)
            {
                _logger.LogWarning("FormResponse with ID {Id} not found", request.Id);
                return new ApiResponse<bool>(false, "FormResponse not found");
            }

            // Eliminar la FormResponse
            _context.FormResponses.Remove(formResponse);

            // Guardar cambios
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("FormResponse with ID {Id} deleted successfully", request.Id);
                return new ApiResponse<bool>(true, "FormResponse deleted successfully");
            }
            else
            {
                _logger.LogError("Failed to delete FormResponse with ID {Id}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete FormResponse");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting FormResponse with ID {Id}", request.Id);
            return new ApiResponse<bool>(false, "Error deleting FormResponse");
        }

    }
}