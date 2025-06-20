using MediatR;

namespace BankStaments.Infrastructure.Commands;

public record class ProcessStatementCommand(Stream FileStream, string FileName) : IRequest<Guid>;
