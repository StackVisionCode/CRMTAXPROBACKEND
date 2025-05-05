using Commands.Firms;
using Common;
using MediatR;
using AutoMapper;
using Domains.Firms;
using Infraestructure.Context;

namespace Handlers.Firms
{
    public class CreateFirmHandler : IRequestHandler<CreateFirmCommand, ApiResponse<bool>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateFirmHandler> _logger;
        private readonly IWebHostEnvironment _env;

        public CreateFirmHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateFirmHandler> logger, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _env = env;
        }

        public async Task<ApiResponse<bool>> Handle(CreateFirmCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentYear = DateTime.UtcNow.Year;
                var doc = request.Firm;

                if (doc.File == null || doc.File.Length == 0)
                {
                    return new ApiResponse<bool>(false, "No file uploaded", false);
                }

                // Validar extensión de archivo permitida
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(doc.File.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    return new ApiResponse<bool>(false, "Invalid file type", false);
                }

                // Crear nombre único de archivo
                var fileName = $"{Guid.NewGuid()}{extension}";

                // Ruta base para guardar el archivo
                var basePath = Path.Combine(
                    _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
                    "Firms",
                    doc.TaxUserId.ToString(),
                    currentYear.ToString()
                );

                // Crear el directorio si no existe
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }

                var filePath = Path.Combine(basePath, fileName);

                // Guardar el archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await doc.File.CopyToAsync(stream, cancellationToken);
                }

                var dbPath = $"Firms/{doc.TaxUserId}/{currentYear}/{fileName}";

                // Mapear y completar la entidad
                var firm = _mapper.Map<Firm>(doc);
                firm.Path = dbPath;
                firm.CreatedAt = DateTime.UtcNow;
                firm.UpdatedAt = DateTime.UtcNow;

                await _dbContext.Firms.AddAsync(firm, cancellationToken);
                var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

                return new ApiResponse<bool>(result, result ? "Firm created successfully" : "Failed to create firm", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating firm: {Message}", ex.Message);
                return new ApiResponse<bool>(false, ex.Message, false);
            }
        }
    }
}
