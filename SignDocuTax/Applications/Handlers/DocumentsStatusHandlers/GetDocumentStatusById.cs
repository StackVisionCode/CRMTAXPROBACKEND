using AutoMapper;
using Common;
using DTOs.Documents;
using DTOs.DocumentsStatus;
using DTOs.DocumentsType;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Documents;
using Queries.DocumentStatus;


namespace Handlers.DocumentsStatusHandlers;

public class GetDocumentStatusById : IRequestHandler<GetDocumentsStatusByIdQuery, ApiResponse<List<ReadDocumentsDtosStatus>>>
{

      private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDocumentStatusById> _logger;
    public GetDocumentStatusById(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetDocumentStatusById> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }
   
    public async Task<ApiResponse<List<ReadDocumentsDtosStatus>>> Handle(GetDocumentsStatusByIdQuery request, CancellationToken cancellationToken)
    {
       try
        {
            var documentType = await _dbContext.DocumentTypes.FindAsync(request.DocumentsStatus.Id, cancellationToken);
             _logger.LogInformation("document created successfully: {document}", documentType);
            if (documentType == null)
            {
                return new ApiResponse<List<ReadDocumentsDtosStatus>>(false, "No document Type found", null!);
            }
            var documentesDTOs = _mapper.Map<List<ReadDocumentsDtosStatus>>(documentType);
            return new ApiResponse<List<ReadDocumentsDtosStatus>>(true, "document types retrieved successfully",documentesDTOs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user tax: {Message}", ex.Message);
            return new ApiResponse<List<ReadDocumentsDtosStatus>>(false, ex.Message, null!);
        }
    }
}
