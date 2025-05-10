using AutoMapper;
using Common;
using DTOs.Documents;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Documents;

namespace Handlers.DocumentsHandlers
{
    public class GetDocumentByIdHandler : IRequestHandler<GetDocumentsByIdQuery, ApiResponse<ReadDocumentsDto>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GetDocumentByIdHandler> _logger;

        public GetDocumentByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetDocumentByIdHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<ReadDocumentsDto>> Handle(GetDocumentsByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _dbContext.Documents.Include(d => d.DocumentStatus)
                    .Include(d => d.DocumentTypes)
                    .FirstOrDefaultAsync(x => x.Id == request.documents.Id, cancellationToken);

                if (document == null)
                {
                    _logger.LogWarning("Document with ID {Id} not found.", request.documents.Id);
                    return new ApiResponse<ReadDocumentsDto>(false, "Document not found", null!);
                }


                var dtos = _mapper.Map<ReadDocumentsDto>(document);



                return new ApiResponse<ReadDocumentsDto>(true, "Document retrieved successfully", dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document: {Message}", ex.Message);
                return new ApiResponse<ReadDocumentsDto>(false, ex.Message, null!);
            }
        }
    }
}
