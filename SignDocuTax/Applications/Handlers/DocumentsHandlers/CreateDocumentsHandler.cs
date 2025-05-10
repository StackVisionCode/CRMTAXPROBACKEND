using System.Security.Cryptography;
using AutoMapper;
using Commands.Documents;
using Common;
using Domain.Documents;
using Infraestructure.Context;
using MediatR;

namespace Handlers.DocumentsHandlers
{
    public class CreateDocumentsHandler : IRequestHandler<CreateDocumentCommands, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateDocumentsHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public CreateDocumentsHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateDocumentsHandler> logger, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _env = env;
        }

        public async Task<ApiResponse<bool>> Handle(CreateDocumentCommands request, CancellationToken cancellationToken)
        {
            try
            {
                var currentYear = DateTime.UtcNow.Year;
                var doc = request.Documents;

                if (doc.File == null || doc.File.Length == 0)
                    return new ApiResponse<bool>(false, "No file uploaded", false);

                // Control de tamaño máximo (opcional pero recomendado)
                const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
                if (doc.File.Length > MaxFileSize)
                    return new ApiResponse<bool>(false, "File size exceeds the limit (10MB)", false);

                // Validar extensiones permitidas (opcional pero recomendado)
                var allowedExtensions = new[] { ".pdf", ".docx", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(doc.File.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return new ApiResponse<bool>(false, $"File type {extension} not allowed", false);

                var originalFileName = Path.GetFileName(doc.File.FileName);

                // Ruta base para guardar el archivo
                var basePath = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    "Documents",
                    doc.TaxUserId.ToString(),
                    currentYear.ToString()
                );

                // Crear el directorio si no existe
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                // Lógica de nombre del archivo
                var fileName = string.IsNullOrWhiteSpace(doc.Name)
                    ? $"{doc.TaxUserId}_{currentYear}_{originalFileName}"
                    : $"{doc.Name}_{originalFileName}";

                var filePath = Path.Combine(basePath, fileName);

                // Copiar el archivo a memoria para guardar y calcular hash solo una vez
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await doc.File.CopyToAsync(memoryStream, cancellationToken);
                    fileBytes = memoryStream.ToArray();
                }

                // Guardar el archivo físico
                await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

                // Calcular el hash
                string originalHash = GenerateSHA256(fileBytes);

                var dbPath = $"Documents/{doc.TaxUserId}/{currentYear}/{fileName}";

                // Crear el documento en la base de datos
                var document = new Document
                {
                    CompanyId = doc.CompanyId,
                    TaxUserId = doc.TaxUserId,
                    Name = doc.Name,
                    DocumentStatusId = doc.DocumentStatusId,
                    DocumentTypeId = doc.DocumentTypeId,
                    Path = dbPath,
                    OriginalHash = originalHash,
                    SignedHash = null,
                    SignedDocumentPath = null,
                    IsSigned = false,
                    FileName = fileName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DeleteAt = null
                };

                await _dbContext.Documents.AddAsync(document, cancellationToken);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                _logger.LogInformation("Document created successfully: {FileName}", fileName);

                return new ApiResponse<bool>(result, result ? "Document created successfully" : "Failed to create document", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }

        // Método para generar SHA256 del archivo
        private string GenerateSHA256(byte[] fileBytes)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(fileBytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
