using AutoMapper;
using Common;
using DTOs.Documents;
using DTOs.DocumentsType;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Documents;


namespace Handlers.DocumentsTypeHandlers;

public class GetAllDocumentsType : IRequestHandler<GetAllDocumentsTypeQuery, ApiResponse<List<ReadDocumentsType>>>
{

      private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllDocumentsType> _logger;
    public GetAllDocumentsType(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllDocumentsType> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }
   
    public async Task<ApiResponse<List<ReadDocumentsType>>> Handle(GetAllDocumentsTypeQuery request, CancellationToken cancellationToken)
    {
       try
        {
            var documentType = await _dbContext.DocumentTypes.ToListAsync(cancellationToken);
             _logger.LogInformation("document created successfully: {document}", documentType);
            if (documentType == null || !documentType.Any())
            {
                return new ApiResponse<List<ReadDocumentsType>>(false, "No document Type found", null!);
            }
            var documentesDTOs = _mapper.Map<List<ReadDocumentsType>>(documentType);
            return new ApiResponse<List<ReadDocumentsType>>(true, "document types retrieved successfully",documentesDTOs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user tax: {Message}", ex.Message);
            return new ApiResponse<List<ReadDocumentsType>>(false, ex.Message, null!);
        }
    }
}
