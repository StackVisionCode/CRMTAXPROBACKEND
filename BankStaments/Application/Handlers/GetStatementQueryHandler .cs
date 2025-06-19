using Application.DTOS;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetStatementQueryHandler : IRequestHandler<GetStatementQuery, StatementDto>
{
       private readonly BankStamentContext _context;
        private readonly IMapper _mapper;
    public GetStatementQueryHandler(BankStamentContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<StatementDto> Handle(GetStatementQuery request, CancellationToken cancellationToken)
    {
       var statement = await _context.BankStatements
                .Include(bs => bs.Transactions)
                .FirstOrDefaultAsync(bs => bs.Id == request.StatementId, cancellationToken);

            return _mapper.Map<StatementDto>(statement);
    }
}
  