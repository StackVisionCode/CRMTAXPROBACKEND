using Application.DTOS;
using MediatR;

namespace Infrastructure.Commands;

public record class DeleteTaxCommand(TaxDto taxDto):IRequest<Guid>;