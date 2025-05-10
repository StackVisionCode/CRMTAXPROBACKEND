using AutoMapper;
using Common;
using Domain.Documents;
using DTOs.Documents;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Documents;

namespace Handlers.DocumentsHandlers
{
    public class GetAllDocumentsHandler : IRequestHandler<GetAllDocumentsQuery, ApiResponse<List<ReadDocumentsDto>>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllDocumentsHandler> _logger;

        public GetAllDocumentsHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllDocumentsHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<ReadDocumentsDto>>> Handle(GetAllDocumentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var documents = await _dbContext.Documents
                .Include(d => d.DocumentStatus)
                .Include(d => d.DocumentTypes)
                .ToListAsync(cancellationToken);
                var dtos = _mapper.Map<List<ReadDocumentsDto>>(documents);

              
                return new ApiResponse<List<ReadDocumentsDto>>(true, "Documents retrieved successfully", dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents: {Message}", ex.Message);
                return new ApiResponse<List<ReadDocumentsDto>>(false, ex.Message, null!);
            }
        }
    }
}
