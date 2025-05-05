using AutoMapper;
using Common;
using DTOs.DocumentsStatus;
using DTOs.DocumentsType;
using Infraestructure.Context;
using MediatR;
using Queries.DocumentStatus;


namespace Handlers.DocumentsStatusHandlers;

public class GetDocumentsStatusAll : IRequestHandler<GetAllDocumentsStatusQuery, ApiResponse<List<ReadDocumentsDtosStatus>>>
{

      private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDocumentsStatusAll> _logger;
    public GetDocumentsStatusAll(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetDocumentsStatusAll> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }
   
   
    Task<ApiResponse<List<ReadDocumentsDtosStatus>>> IRequestHandler<GetAllDocumentsStatusQuery, ApiResponse<List<ReadDocumentsDtosStatus>>>.Handle(GetAllDocumentsStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var documentsStatus = _dbContext.DocumentStatus.ToList();
            var documentsStatusDtos = _mapper.Map<List<ReadDocumentsDtosStatus>>(documentsStatus);
            return Task.FromResult(new ApiResponse<List<ReadDocumentsDtosStatus>>(true, "Document status retrieved successfully", documentsStatusDtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document status: {Message}", ex.Message);
            return Task.FromResult(new ApiResponse<List<ReadDocumentsDtosStatus>>(false, "An error occurred while retrieving the document status.", []));
        }
    }
}
