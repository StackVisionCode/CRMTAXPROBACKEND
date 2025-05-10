using AutoMapper;
using Commands.DocumentsType;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.DocumentsTypeHandlers;

public class DeleteDocumentsStatus : IRequestHandler<DeleteDocumenttStatusCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<DeleteDocumentsStatus> _logger;
    public DeleteDocumentsStatus(ApplicationDbContext dbContext, IMapper mapper, ILogger<DeleteDocumentsStatus> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteDocumenttStatusCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var documentType = await _dbContext.DocumentStatus.FirstOrDefaultAsync(d => d.Id == request.DocumentsStatus.Id, cancellationToken);

            if (documentType == null)
            {
                return new ApiResponse<bool>(false, "Document Type not found", false);
            }

            _dbContext.DocumentStatus.Remove(documentType);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document Type deleted successfully: {Id}", request.DocumentsStatus.Id);

            return new ApiResponse<bool>(true, "Document Type deleted successfully", true);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DocumentType: {Message}", ex.Message);
            return new ApiResponse<bool>(false, "An error occurred while updating the document type.", false);
        }
    }
}

