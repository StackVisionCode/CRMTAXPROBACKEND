using Application.DTOS;
using MediatR;

namespace Infrastructure.Commands;

public record class CreateTaxCommand(TaxDto taxDto):IRequest<Guid>;