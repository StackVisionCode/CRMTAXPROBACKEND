using AutoMapper;
using Common;
using Domain.Documents;
using DTOs.Documents;
using Infraestructure.Context;
using MediatR;

public class CreateDocumentWithSignersHandler 
    : IRequestHandler<CreateDocumentWithSignersCommand, ApiResponse<DocumentResponse>>
{
    private readonly ApplicationDbContext _context;
            private readonly IMapper _mapper;
   

    public CreateDocumentWithSignersHandler(
        ApplicationDbContext context, IMapper mapper)
       
    {
        _context = context;
        _mapper=mapper;
       
    }

    public async Task<ApiResponse<DocumentResponse>> Handle(
        CreateDocumentWithSignersCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validar archivo
            if (command.Request.File == null || command.Request.File.Length == 0)
                return new ApiResponse<DocumentResponse>(false,"El archivo es requerido");

            // 2. Guardar documento físico
            var filePath = await SaveDocumentFile(command.Request.File);
            
            // 3. Crear entidad Document
            var document = new Document
            {
                Name = command.Request.Name,
                FileName = command.Request.File.FileName,
                Path = filePath,
                DocumentTypeId = command.Request.DocumentTypeId,
                CompanyId = command.Request.CompanyId,
                TaxUserId = command.Request.TaxUserId,
                DocumentStatusId = command.Request.DocumentTypeId
            };

            await _context.Documents.AddAsync(document, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // 4. Procesar firmantes
            await ProcessSigners(document.Id, command.Request);


            var dtos = _mapper.Map<Document>(document);

            // 5. Retornar respuesta
            return new ApiResponse<DocumentResponse>(true,"Documento creado exitosamente");
        }
        catch (Exception ex)
        {
            return new ApiResponse<DocumentResponse>(false,$"Error: {ex.Message}");
        }
    }

    private async Task<string> SaveDocumentFile(IFormFile file)
    {
        // Implementación para guardar archivo físico
        // Retornar ruta relativa
        return "";
    }

    private async Task ProcessSigners(int documentId, CreateDocumentWithSignersRequest request)
    {
        // Procesar firmantes registrados y externos
        // Enviar emails de invitación
    }

}