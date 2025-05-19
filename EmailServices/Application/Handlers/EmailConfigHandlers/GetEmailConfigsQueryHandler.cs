using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailConfigsQueryHandler : IRequestHandler<GetEmailConfigsQuery, IEnumerable<EmailConfigDTO>>
{
  private readonly EmailContext _ctx;
  private readonly IMapper _map;

  public GetEmailConfigsQueryHandler(EmailContext ctx, IMapper map)
  {
    _ctx = ctx;
    _map = map;
  }

  public async Task<IEnumerable<EmailConfigDTO>> Handle(GetEmailConfigsQuery q, CancellationToken ct)
  {
    var query = _ctx.EmailConfigs.AsQueryable();

    if (q.CompanyId.HasValue)
      query = query.Where(c => c.CompanyId == q.CompanyId);

    if (q.UserId.HasValue)
      query = query.Where(c => c.UserId == q.UserId);

    var list = await query.OrderBy(c => c.Name).ToListAsync(ct);
    return _map.Map<IEnumerable<EmailConfigDTO>>(list);
  }
}
