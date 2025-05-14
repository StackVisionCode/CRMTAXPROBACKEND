using AutoMapper;
using Common;
using DTOs.Contacts;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.Contacts;

public class GetContactsByCompanyIdQueryHandler : IRequestHandler<GetContactsByCompanyIdQuery, ApiResponse<List<ContactDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetContactsByCompanyIdQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ContactDto>>> Handle(GetContactsByCompanyIdQuery request, CancellationToken cancellationToken)
    {
        var contacts = await _context.Contacts
            .Where(x => x.CompanyId == request.CompanyId)
            .ToListAsync(cancellationToken);

        return new ApiResponse <List<ContactDto>>(true,"Contact obteneid",_mapper.Map<List<ContactDto>>(contacts));
    }
}
