using AutoMapper;
using Commands.Documents;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.DocumentsHandlers
{
    public class DeleteDocumentsHandler : IRequestHandler<DeleteDocumentCommands, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<DeleteDocumentsHandler> _logger;
          private readonly IWebHostEnvironment _env;

        public DeleteDocumentsHandler(ApplicationDbContext dbContext, ILogger<DeleteDocumentsHandler> logger,IWebHostEnvironment env)
        {
        
            _dbContext = dbContext;
            _logger = logger;_env = env;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteDocumentCommands request, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _dbContext.Documents.FirstOrDefaultAsync(x => x.Id == request.Documents.Id, cancellationToken);
                if (document == null)
                    return new ApiResponse<bool>(false, "Document not found", false);
     // Llamamos al método privado para eliminar el archivo físico
                var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), document.Path);
                if (File.Exists(filePath))
                    DeleteFile(filePath); // Eliminar el archivo físico
                   
                _dbContext.Documents.Remove(document);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                _logger.LogInformation("Document deleted successfully: {@Document}", document);
                return new ApiResponse<bool>(result, result ? "Document deleted successfully" : "Failed to delete document", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }


        // Método privado para eliminar el archivo físico
        private void DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath); // Elimina el archivo físico
                    _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                }
                else
                {
                    _logger.LogWarning("File not found: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Message}", ex.Message);
            }
        }
    }

    
}
