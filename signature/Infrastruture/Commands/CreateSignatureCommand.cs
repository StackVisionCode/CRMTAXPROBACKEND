using Application.DTOs;
using MediatR;

namespace Infraestructure.Commands;
public record CreateSignatureCommand(CreateSignatureDto CreateSignDto) : IRequest<Guid>;