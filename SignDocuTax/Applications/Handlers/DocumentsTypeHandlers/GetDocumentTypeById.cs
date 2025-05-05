using AutoMapper;
using Common;
using DTOs.Documents;
using DTOs.DocumentsStatus;
using DTOs.DocumentsType;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Documents;


namespace Handlers.DocumentsTypeHandlers;

public class GetDocumentTypeById : IRequestHandler<GetDocumentsTypeByIdQuery, ApiResponse<ReadDocumentsType>>
{

    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetDocumentTypeById> _logger;
    public GetDocumentTypeById(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetDocumentTypeById> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ReadDocumentsType>> Handle(GetDocumentsTypeByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var documentType = await _dbContext.DocumentTypes.FindAsync(new object[] { request.DocumentsType.Id }, cancellationToken);
            if (documentType == null)
            {
                return new ApiResponse<ReadDocumentsType>(false, "No document type found", null!);
            }
            
            var documentDto = _mapper.Map<ReadDocumentsType>(documentType);

            return new ApiResponse<ReadDocumentsType>(true, "Document type retrieved successfully", documentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document type: {Message}", ex.Message);
            return new ApiResponse<ReadDocumentsType>(false, ex.Message, null!);
        }
    }
}
