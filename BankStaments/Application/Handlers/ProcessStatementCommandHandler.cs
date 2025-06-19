using Application.Interfaes;
using BankStaments.Infrastructure.Commands;
using Infrastructure.Context;
using MediatR;

namespace Application.Handlers;

public class ProcessStatementCommandHandler : IRequestHandler<ProcessStatementCommand, Guid>
{
      private readonly BankStamentContext _context;
        private readonly IFileParser _fileParser;

    public ProcessStatementCommandHandler(BankStamentContext context, IFileParser fileParser)
    {
        _context = context;
        _fileParser = fileParser;
    }
    public async Task<Guid> Handle(ProcessStatementCommand request, CancellationToken cancellationToken)
    {
            var statement = await _fileParser.ParseFileAsync(request.FileStream, request.FileName);
            
            await _context.BankStatements.AddAsync(statement, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            return statement.Id;
    }
}
