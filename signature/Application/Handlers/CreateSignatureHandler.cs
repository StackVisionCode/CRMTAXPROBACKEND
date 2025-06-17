using Application.Interfaces;
using AutoMapper;
using Domain;
using Domain.Entities;
using Infraestructure.Commands;
using MediatR;

namespace Application.Handlers;

public class CreateSignatureHandler : IRequestHandler<CreateSignatureCommand, Guid>
{

    private readonly ISignatureRepository _repo;
    private readonly ICertificateService _certService;
    private readonly IMapper _mapper;

    public CreateSignatureHandler(ISignatureRepository repo, ICertificateService certService, IMapper mapper)
    {
        _repo = repo;
        _certService = certService;
        _mapper = mapper;
    }

    private string SaveBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var path = Path.Combine("signatures", $"{Guid.NewGuid()}.png");

        Directory.CreateDirectory("signatures"); // asegúrate que la carpeta exista
        File.WriteAllBytes(path, bytes);

        return path;
    }
    public async  Task<Guid> Handle(CreateSignatureCommand request, CancellationToken cancellationToken)
    {
            try
        {
            var dto = request.CreateSignDto;

            // Convertir base64 en archivo temporal (solo para firmar)
            var tempPath = SaveBase64Temporarily(dto.Base64Image!);

            // Firmar el archivo (usa archivo físico para aplicar certificado)
            var thumbprint = _certService.SignFile(tempPath);
                File.Delete(tempPath);
            // Mapear el DTO a la entidad y completar campos
            var entity = _mapper.Map<Signature>(dto);
            entity.Id = Guid.NewGuid();
            entity.Status = SignatureStatus.Signed;
            entity.CreatedAt = DateTime.UtcNow;
            entity.SignedAt = DateTime.UtcNow;
            entity.CertificateThumbprint = thumbprint;
            entity.FilePath = tempPath; // opcional: puedes eliminar si no vas a guardar el archivo
            entity.Base64Image = dto.Base64Image!; // almacenar en DB

            await _repo.AddAsync(entity, cancellationToken);

            return entity.Id;
        }
        catch (Exception ex)
        {
            throw new Exception("Error al crear la firma", ex);
        }
    }

    /// <summary>
    /// Guarda temporalmente el base64 en un archivo para firmarlo con certificado.
    /// </summary>
    private string SaveBase64Temporarily(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var path = Path.Combine("temp-signatures", $"{Guid.NewGuid()}.png");

        Directory.CreateDirectory("temp-signatures");
        File.WriteAllBytes(path, bytes);

        return path;
    }
}
