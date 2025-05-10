using AutoMapper;
using Commands.Documents;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.DocumentsHandlers
{
    public class UpdateDocumentsHandler : IRequestHandler<UpdateDocumentCommands, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateDocumentsHandler> _logger;

        public UpdateDocumentsHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateDocumentsHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<bool>> Handle(UpdateDocumentCommands request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == request.Documents.Id, cancellationToken);
                if (document == null)
                    return new ApiResponse<bool>(false, "Document not found", false);

                // Validar si el documento está firmado
                if (document.IsSigned)
                    return new ApiResponse<bool>(false, "Cannot update a signed document", false);

                _mapper.Map(request.Documents, document);
                document.UpdatedAt = DateTime.UtcNow;

                _dbContext.Documents.Update(document);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                _logger.LogInformation("Document updated successfully: {@Document}", document);
                return new ApiResponse<bool>(result, result ? "Document updated successfully" : "Failed to update document", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
