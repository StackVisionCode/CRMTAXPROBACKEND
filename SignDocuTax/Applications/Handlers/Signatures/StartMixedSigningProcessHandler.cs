using AutoMapper;
using Commands.Signatures;
using Common;
using Domain.Signatures;
using Domains.Requirements;
using Domains.Signers;
using DTOs.Signatures;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Services.Contracts;

namespace Handlers.Signatures;

public class StartMixedSigningProcessHandler 
    : IRequestHandler<StartMixedSigningProcessCommand, ApiResponse<SigningProcessResultDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
   

    public StartMixedSigningProcessHandler(
        ApplicationDbContext context,
        IMapper mapper
      )
    {
        _context = context;
        _mapper = mapper;
  
    }

    public async Task<ApiResponse<SigningProcessResultDto>> Handle(
        StartMixedSigningProcessCommand request, 
        CancellationToken cancellationToken)
    {
        // Validar documento
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.SigningProcessDto.DocumentId, cancellationToken);
            
        if (document == null)
            return new ApiResponse<SigningProcessResultDto>( false, "Documento no encontrado");

        
        // Validar firmantes externos
        var invalidExternalSigners = await ValidateExternalSigners(
            request.SigningProcessDto.ExternalSigners, cancellationToken);
            
        if (invalidExternalSigners.Any())
            return new ApiResponse<SigningProcessResultDto>(false, $"Contactos externos no válidos: {string.Join(", ", invalidExternalSigners)}");

        // Crear proceso de firma
        var requirement = new RequirementSignature
        {
            DocumentId = document.Id,
            StatusSignatureId = 1,
            Quantity = request.SigningProcessDto.InternalSigners.Count + 
                      request.SigningProcessDto.ExternalSigners.Count,
            ExpiryDate = request.SigningProcessDto.ExpiryDate ?? DateTime.UtcNow.AddDays(15),
            ConsentText = "Al firmar, acepta los términos y condiciones..."
        };

        // Agregar firmantes internos
        foreach (var userId in request.SigningProcessDto.InternalSigners)
        {
            requirement.EventSignatures.Add(new EventSignature
            {
                TaxUserId = userId,
                SignatureEventTypeId = 1,
                DocumentId = document.Id
            });
        }

        // Agregar firmantes externos
        foreach (var contactId in request.SigningProcessDto.ExternalSigners)
        {
            requirement.ExternalSigners.Add(new ExternalSigner
            {
                ContactId = contactId,
                SignatureStatusId = 1,
                InvitationSentDate = DateTime.UtcNow,
                DocumentId = document.Id,
                SigningToken = GenerateSigningToken()
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        /*Enviar notificaciones
        await _notificationService.NotifySignersAsync(
            requirement.Id, 
            request.SigningProcessDto.CustomMessage);*/ 
          var dtos=    _mapper.Map<SigningProcessResultDto>(requirement);

        return new ApiResponse<SigningProcessResultDto>(true,"Proceso de firma iniciado correctamente",dtos);
    }

    private async Task<List<int>> ValidateInternalSigners(List<int> userIds, CancellationToken ct)
    {
        var existingUsers = await _context.Documents
            .Where(u => userIds.Contains(u.TaxUserId))
            .Select(u => u.Id)
            .ToListAsync(ct);
            
        return userIds.Except(existingUsers).ToList();
    }

    private async Task<List<int>> ValidateExternalSigners(List<int> contactIds, CancellationToken ct)
    {
        var existingContacts = await _context.Contacts
            .Where(c => contactIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
            
        return contactIds.Except(existingContacts).ToList();
    }

    private string GenerateSigningToken()
    {
        return $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks.ToString("x")}";
    }
}