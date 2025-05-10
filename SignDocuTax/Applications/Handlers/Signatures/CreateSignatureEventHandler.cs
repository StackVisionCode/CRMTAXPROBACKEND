using AutoMapper;
using Commands.Signatures;
using Common;
using Domain.Documents;
using Domain.Signatures;
using DTOs.Signatures;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.Signatures;

public class CreateSignatureEventHandler
    : IRequestHandler<CreateSignatureEventCommand, ApiResponse<SignatureEventResultDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateSignatureEventHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<SignatureEventResultDto>> Handle(
        CreateSignatureEventCommand request,
        CancellationToken cancellationToken)
    {
        // Validar documento
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.SignatureEventDto.DocumentId, cancellationToken);

        if (document == null)
            return new ApiResponse<SignatureEventResultDto>(false, "Documento no encontrado");

        if (request.SignatureEventDto.ExternalSignerId.HasValue)
        {
            var signer = await _context.ExternalSigners
                .Include(s => s.Contact)
                .FirstOrDefaultAsync(s => s.Id == request.SignatureEventDto.ExternalSignerId, cancellationToken);

            if (signer == null)
                return new ApiResponse<SignatureEventResultDto>(
                   false, "Firmante externo no válido");

            if (signer.SignedDate.HasValue)
                return new ApiResponse<SignatureEventResultDto>(
                    false, "El firmante externo ya ha firmado");
        }
        else
        {
            return new ApiResponse<SignatureEventResultDto>(
                 false, "Debe especificar un firmante");
        }

        // Crear evento de firma
        var signatureEvent = _mapper.Map<EventSignature>(request.SignatureEventDto);
        signatureEvent.SignatureDate = DateTime.UtcNow;
        signatureEvent.SignatureEventTypeId = 1;

        _context.EventSignatures.Add(signatureEvent);

        // Actualizar firmante externo si aplica
        if (request.SignatureEventDto.ExternalSignerId.HasValue)
        {
            var externalSigner = await _context.ExternalSigners
                .FirstAsync(s => s.Id == request.SignatureEventDto.ExternalSignerId, cancellationToken);

            externalSigner.SignedDate = DateTime.UtcNow;
            externalSigner.SignatureStatusId = 1;
        }

        // Verificar si el documento está completamente firmado
        var allSignaturesComplete = await CheckAllSignaturesComplete(document.Id, cancellationToken);
        if (allSignaturesComplete)
        {
            document.IsSigned = true;
            document.SignedHash = ComputeDocumentHash(document);
        }

        var result = await _context.SaveChangesAsync(cancellationToken) > 0;
        var dtos =  _mapper.Map<SignatureEventResultDto>(signatureEvent);
        return new ApiResponse<SignatureEventResultDto>(result, result ? "Firma registrada correctamente":"Error al registrar", dtos);
    }

    private async Task<bool> CheckAllSignaturesComplete(int documentId, CancellationToken ct)
    {
        var requirement = await _context.RequirementSignatures
            .Include(r => r.EventSignatures)
            .Include(r => r.ExternalSigners)
            .FirstOrDefaultAsync(r => r.DocumentId == documentId, ct);

        if (requirement == null) return false;

        return requirement.EventSignatures.All(e => e.SignatureDate != null) &&
               requirement.ExternalSigners.All(e => e.SignedDate != null);
    }

    private string ComputeDocumentHash(Document document)
    {
        // Implementar lógica de hash del documento
        return Guid.NewGuid().ToString(); // Ejemplo simplificado
    }
}