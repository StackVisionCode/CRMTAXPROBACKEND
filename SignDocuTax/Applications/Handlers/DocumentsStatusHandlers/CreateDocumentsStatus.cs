using AutoMapper;
using Commands.DocumentsStatus;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;

namespace Handlers.DocumentsTypeHandlers;

public class CreateDocumentsStatus : IRequestHandler<CreateDocumentStatusCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateDocumentsStatus> _logger;
    public CreateDocumentsStatus(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateDocumentsStatus> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(CreateDocumentStatusCommands request, CancellationToken cancellationToken)
    {

        try
        {
            var documentType = _mapper.Map<DocumentStatus>(request.DocumentsStatus);
            await _dbContext.DocumentStatus.AddAsync(documentType, cancellationToken);
            documentType.CreatedAt = DateTime.UtcNow;
            documentType.UpdatedAt = DateTime.UtcNow;
            documentType.DeleteAt = null; // Set to null for new records
            // Save changes to the database
            
            
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;

            _logger.LogInformation("Document Type created successfully: {Document Type}", documentType);
            return new ApiResponse<bool>(result, result ? "Document Type created successfully" : "Failed to create Document Type", result);
        }
        catch (Exception ex)
        {
            var fullError = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Error creating Document Type: {Message}", fullError);
            return new ApiResponse<bool>(false, ex.Message, false);

        }

    }


}

