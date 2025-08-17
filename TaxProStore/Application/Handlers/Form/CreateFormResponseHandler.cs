using System.Text.Json;
using Application.Common;
using Application.Dtos.Form;
using AutoMapper;
using Domain.Entity.Form;
using Infrastructure.Command.Form;
using Infrastructure.Context;
using Infrastructure.Services;
using MediatR;

public class CreateFormResponseHandler : IRequestHandler<CreateFormResponseCommand, ApiResponse<bool>>
{
    private readonly TaxProStoreDbContext _context;
    private readonly IMapper _mapper;
    private readonly IFileService _fileService;

    private readonly ILogger<CreateFormResponseHandler> _logger;

    public CreateFormResponseHandler(TaxProStoreDbContext context, IMapper mapper, IFileService fileService, ILogger<CreateFormResponseHandler> logger)
    {
        _context = context;
        _mapper = mapper;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(CreateFormResponseCommand request, CancellationToken cancellationToken)
    {


        // Process files if any
        var fileProcessingResult = await _fileService.ProcessFormFilesAsync(
               request.Response.Data,
               request.Response.FormInstanceId.ToString());


        if (fileProcessingResult == null)
        {
            return new ApiResponse<bool>(false,"File processing failed.");
        }

        // Analizar los datos para obtener estadísticas
        // Analizar los datos para obtener estadísticas
        var dataAnalysis = AnalyzeFormData(request.Response.Data);

        if (dataAnalysis == null)
        {
            return new ApiResponse<bool>(false, "Invalid form data format.");
        }

        var entity = _mapper.Map<FormResponse>(request.Response);
        if (entity == null)
        {
            return new ApiResponse<bool>(false, "Invalid form response data.");
        }
        // Check if the form response already exists
        entity.Id = Guid.NewGuid(); // Ensure a new ID is set for the new response



        await _context.FormResponses.AddAsync(entity, cancellationToken);
        // Save changes to the databaser

        await _context.SaveChangesAsync(cancellationToken);


        // Preparar resultado con información detallada
        var result = new FormResponseResult
        {
            ResponseId = entity.Id,
            FormInstanceId = request.Response.FormInstanceId,
            FieldsCount = dataAnalysis.FieldsCount,
            FilesCount = dataAnalysis.FilesCount,
            ProcessedFiles = fileProcessingResult.Data ?? new List<string>(),
            SubmittedAt = entity.SubmittedAt,
            Success = true
        };
        _logger.LogInformation("Form response created successfully with ID: {ResponseId}", result);
        _logger.LogInformation("Form response data analysis: {FieldsCount} fields, {FilesCount} files", 
            dataAnalysis.FieldsCount, dataAnalysis.FilesCount);


        return new ApiResponse<bool>(true, "Form response created successfully.");
    }

    private FormDataAnalysis AnalyzeFormData(string data)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(data);
            var root = jsonDoc.RootElement;

            int fieldsCount = 0;
            int filesCount = 0;

            foreach (var field in root.EnumerateObject())
            {
                if (field.Name == "_metadata") continue;

                fieldsCount++;

                var fieldValue = field.Value;

                // Contar archivos
                if (fieldValue.ValueKind == JsonValueKind.Object && HasFileProperties(fieldValue))
                {
                    filesCount++;
                }
                else if (fieldValue.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in fieldValue.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object && HasFileProperties(item))
                        {
                            filesCount++;
                        }
                    }
                }
            }

            return new FormDataAnalysis
            {
                FieldsCount = fieldsCount,
                FilesCount = filesCount
            };
        }
        catch
        {
            return new FormDataAnalysis { FieldsCount = 0, FilesCount = 0 };
        }
    }

    private static bool HasFileProperties(JsonElement element)
    {
        return element.TryGetProperty("name", out _) &&
               element.TryGetProperty("size", out _) &&
               element.TryGetProperty("type", out _);
    }
}
