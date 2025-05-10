using AutoMapper;
using Commands.DocumentsType;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;

namespace Handlers.DocumentsTypeHandlers;

public class UpdateDocumentsType : IRequestHandler<UpdateDocumentTypeCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateDocumentsType> _logger;
    public UpdateDocumentsType(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateDocumentsType> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateDocumentTypeCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var documentType = await _dbContext.DocumentTypes.FindAsync(new object[] { request.DocumentsType.Id }, cancellationToken);

            if (documentType == null)
            {
                _logger.LogWarning("DocumentType with ID {Id} not found.", request.DocumentsType.Id);
                return new ApiResponse<bool>(false, "Document type not found", false);
            }

            documentType.Name = request.DocumentsType.Name;
            documentType.Description = request.DocumentsType.Description ?? documentType.Description; // Keep existing description if not provided
            documentType.UpdatedAt = DateTime.UtcNow; // Update the timestamp

            // Map the updated fields from request to entity

            var documentTypeUpdate = _mapper.Map<DocumentType>(documentType);
            _dbContext.DocumentTypes.Update(documentTypeUpdate);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("DocumentType with ID {Id} updated successfully.", request.DocumentsType.Id);
            return new ApiResponse<bool>(true, "Document type updated successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating DocumentType: {Message}", ex.Message);
            return new ApiResponse<bool>(false, "An error occurred while updating the document type.", false);
        }
    }

}

