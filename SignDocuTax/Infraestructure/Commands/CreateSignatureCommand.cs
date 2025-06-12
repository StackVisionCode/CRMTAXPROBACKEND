using Applications.DTOs;
using MediatR;

namespace Infraestructure.Commands;
public record CreateSignatureCommand(CreateSignatureDto Dto) : IRequest<Guid>;