using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailConfigByIdQueryHandler : IRequestHandler<GetEmailConfigByIdQuery, EmailConfigDTO?>
{
  private readonly EmailContext _ctx;
  private readonly IMapper _map;

  public GetEmailConfigByIdQueryHandler(EmailContext ctx, IMapper map)
  {
    _ctx = ctx;
    _map = map;
  }

  public async Task<EmailConfigDTO?> Handle(GetEmailConfigByIdQuery q,
                                            CancellationToken ct)
  {
    var entity = await _ctx.EmailConfigs
                          .FirstOrDefaultAsync(c => c.Id == q.Id, ct);
    return entity is null ? null : _map.Map<EmailConfigDTO>(entity);
  }
}
