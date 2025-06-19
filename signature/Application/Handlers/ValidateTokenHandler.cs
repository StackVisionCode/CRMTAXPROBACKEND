using Application.Helpers;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using signature.Application.DTOs;
using signature.Infrastruture.Queries;

namespace signature.Application.Handlers;

public class ValidateTokenHandler
    : IRequestHandler<ValidateTokenQuery, ApiResponse<ValidateTokenResultDto>>
{
    private readonly SignatureDbContext _db;
    private readonly ISignatureValidToken _tokenSvc;
    private readonly ILogger<ValidateTokenHandler> _log;

    public ValidateTokenHandler(
        SignatureDbContext db,
        ISignatureValidToken tokenSvc,
        ILogger<ValidateTokenHandler> log
    )
    {
        _db = db;
        _tokenSvc = tokenSvc;
        _log = log;
    }

    public async Task<ApiResponse<ValidateTokenResultDto>> Handle(
        ValidateTokenQuery request,
        CancellationToken cancellationToken
    )
    {
        /* 1 ▸ Valida JWT */
        (bool ok, Guid signerId, Guid reqId) = _tokenSvc.Validate(request.Token, "sign");
        if (!ok)
            return new(false, "Token inválido o expirado");

        /* 2 ▸ Busca solicitud + firmante */
        var req = await _db
            .SignatureRequests.Include(r => r.Signers)
            .FirstOrDefaultAsync(r => r.Id == reqId, cancellationToken);

        if (req is null)
            return new(false, "Solicitud no encontrada");

        var signer = req.Signers.FirstOrDefault(s => s.Id == signerId);
        if (signer is null)
            return new(false, "Firmante no encontrado para este token");

        /* 3 ▸ Construye DTO */
        var dto = new ValidateTokenResultDto
        {
            SignatureRequestId = req.Id,
            SignerId = signer.Id,
            DocumentId = req.DocumentId,
            SignerEmail = signer.Email,
            SignerStatus = signer.Status,
            RequestStatus = req.Status,
        };

        return new(true, "Token válido", dto);
    }
}
