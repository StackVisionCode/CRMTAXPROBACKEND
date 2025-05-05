using AutoMapper;
using Commands.DocumentsType;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;

namespace Handlers.DocumentsTypeHandlers;

public class UpdateDocumentsStatus : IRequestHandler<UpdateDocumentStatusCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateDocumentsType> _logger;
    public UpdateDocumentsStatus(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateDocumentsType> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateDocumentStatusCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var documentType = await _dbContext.DocumentStatus.FindAsync(new object[] { request.DocumentsStatus.Id }, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("DocumentType with ID {Id} not found.", request.DocumentsStatus.Id);
                return new ApiResponse<bool>(false, "Document type not found", false);
            }

            documentType.Name = request.DocumentsStatus.Name;
            documentType.Description = request.DocumentsStatus.Description ?? documentType.Description; // Keep existing description if not provided
            documentType.UpdatedAt = DateTime.UtcNow; // Update the timestamp

            // Map the updated fields from request to entity

            var documentTypeUpdate = _mapper.Map<DocumentStatus>(documentType);
            _dbContext.DocumentStatus.Update(documentTypeUpdate);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DocumentType with ID {Id} updated successfully.", request.DocumentsStatus.Id);
            return new ApiResponse<bool>(true, "Document type updated successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DocumentType: {Message}", ex.Message);
            return new ApiResponse<bool>(false, "An error occurred while updating the document type.", false);
        }
    }

}

